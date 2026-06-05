using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Infrastructure.Services;

public sealed class ComponentMatcher : IComponentMatcher
{
  private readonly CatalogDbContext _context;
  private readonly IComponentNameParser _nameParser;

  public ComponentMatcher(CatalogDbContext context, IComponentNameParser nameParser)
  {
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _nameParser = nameParser ?? throw new ArgumentNullException(nameof(nameParser));
  }

  public async Task<MatchResult> MatchAsync(
    ImportedComponent imported,
    CancellationToken ct = default)
  {
    var typeName = imported.DetectedCategory;

    if (string.IsNullOrEmpty(typeName))
      return Unmatched($"Не удалось распарсить тип компонента: {imported.RawName}");

    // 1. Ищем тип компонента в БД
    var type = await _context.ComponentTypes
        .FirstOrDefaultAsync(t => t.Name == typeName, ct);

    if (type is null)
      return Unmatched($"Неизвестный тип компонента: {typeName}");

    Family? family = null;
    var parsed = _nameParser.Parse($"{typeName} {imported.RawName}");

    if (!string.IsNullOrEmpty(parsed.Family) && parsed.Family != parsed.Name)
    {
      family = await _context.Families
          .Include(f => f.FamilyForms)
              .ThenInclude(ff => ff.Form)
          .FirstOrDefaultAsync(
              f => f.ComponentTypeId == type.Id
                && f.Name == parsed.Family, ct);
    }


    // 4. Ищем компонент по полному имени
    var component = await _context.Components
    .Include(c => c.OwnForm)
    .FirstOrDefaultAsync(c => c.FullName == imported.RawName, ct);

    // 4. Определяем требуемые формы
    var requiredForms = new List<Form>();

    if (family is not null)
      requiredForms.AddRange(family.FamilyForms.Select(ff => ff.Form));

    if (component?.OwnForm is not null
        && requiredForms.All(f => f.Id != component.OwnForm.Id))
      requiredForms.Add(component.OwnForm);

    System.Diagnostics.Debug.WriteLine(
  $"Looking for component: '{imported.RawName}'");
    System.Diagnostics.Debug.WriteLine(
        $"Found component: {component?.FullName ?? "NULL"}");
    System.Diagnostics.Debug.WriteLine(
        $"OwnForm: {component?.OwnForm?.Number ?? "NULL"}");


    return new MatchResult
    {
      IsMatched = component is not null,
      MatchedType = type,
      MatchedFamily = family,
      MatchedComponent = component,
      RequiredForms = requiredForms,
      Warning = component is null ? $"Компонент не найден: {parsed.Name}" : null
    };
  }

  // ── Batch match — 3 queries total regardless of input size ────────────────
  public async Task<ProjectMatchResult> MatchAllAsync(
    IReadOnlyList<ImportedComponent> components,
    CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(components);

    if (components.Count == 0)
      return new ProjectMatchResult
      {
        Matched = [],
        Unresolved = [],
        Warnings = [],
      };

    // ── Step 1: parse all names up-front (CPU-only, no DB) ──────────────────
    // Key: imported component  →  its parsed representation
    var parsed = components.ToDictionary(c => c, c => _nameParser.Parse($"{c.DetectedCategory} {c.RawName}"));

    // ── Step 2: load all needed data in 3 batch queries ─────────────────────
    var typeNames = components
      .Select(c => c.DetectedCategory)
      .Where(n => !string.IsNullOrEmpty(n))
      .Distinct()
      .ToList();

    // Query 1 — all referenced ComponentTypes
    var typesByName = await _context.ComponentTypes
      .Where(t => typeNames.Contains(t.Name))
      .ToDictionaryAsync(t => t.Name, ct);

    var familyNames = parsed.Values
      .Where(p => !string.IsNullOrEmpty(p.Family) && p.Family != p.Name)
      .Select(p => p.Family)
      .Distinct()
      .ToList();

    // Query 2 — all referenced Families with their form associations
    var familiesByTypeAndName = await _context.Families
      .Include(f => f.FamilyForms)
        .ThenInclude(ff => ff.Form)
      .Where(f => familyNames.Contains(f.Name))
      .ToListAsync(ct);

    // Build a lookup keyed by (ComponentTypeId, FamilyName) for O(1) access
    var familyLookup = familiesByTypeAndName
        .ToDictionary(f => (f.ComponentTypeId, f.Name));

    var rawNames = components
    .Select(c => c.RawName)
    .Distinct()
    .ToList();

    // Query 3 — all referenced Components
    var componentsByName = await _context.Components
        .Include(c => c.OwnForm)
        .Where(c => rawNames.Contains(c.FullName))
        .ToDictionaryAsync(c => c.FullName, ct);

    // ── Step 3: match in-memory, zero additional DB calls ───────────────────
    var matched = new List<ComponentMatchEntry>(components.Count);
    var unresolved = new List<ComponentMatchEntry>();
    var warnings = new List<string>();

    foreach (var imported in components)
    {
      ct.ThrowIfCancellationRequested();

      var p = parsed[imported];
      var typeName = imported.DetectedCategory;

      if (string.IsNullOrEmpty(typeName))
      {
        var warning = $"Не удалось распарсить тип компонента: {imported.RawName}";
        unresolved.Add(new ComponentMatchEntry
        {
          Imported = imported,
          MatchResult = Unmatched(warning),
        });
        warnings.Add(warning);
        continue;
      }

      if (!typesByName.TryGetValue(typeName, out var type))
      {
        var warning = $"Неизвестный тип компонента: {typeName}";
        unresolved.Add(new ComponentMatchEntry
        {
          Imported = imported,
          MatchResult = Unmatched(warning),
        });
        warnings.Add(warning);
        continue;
      }

      Family? family = null;
      if (!string.IsNullOrEmpty(p.Family) && p.Family != p.Name)
        familyLookup.TryGetValue((type.Id, p.Family), out family);

      componentsByName.TryGetValue(imported.RawName, out var component);

      var result = BuildResult(p, type, family, component);

      var entry = new ComponentMatchEntry
      {
        Imported = imported,
        MatchResult = result,
      };

      if (result.IsMatched)
        matched.Add(entry);
      else
        unresolved.Add(entry);

      if (result.Warning is not null)
        warnings.Add(result.Warning);
    }

    return new ProjectMatchResult
    {
      Matched = matched,
      Unresolved = unresolved,
      Warnings = warnings,
    };
  }

  private static MatchResult BuildResult(
      ParsedComponentName parsed,
      ComponentType type,
      Family? family,
      Component? component)
  {
    var requiredForms = new List<Form>();

    if (family is not null)
      requiredForms.AddRange(family.FamilyForms.Select(ff => ff.Form));

    if (component?.OwnForm is not null
        && requiredForms.All(f => f.Id != component.OwnForm.Id))
      requiredForms.Add(component.OwnForm);

    return new MatchResult
    {
      IsMatched = component is not null,
      MatchedType = type,
      MatchedFamily = family,
      MatchedComponent = component,
      RequiredForms = requiredForms,
      Warning = component is null ? $"Компонент не найден: {parsed.Name}" : null,
    };
  }

  private static MatchResult Unmatched(string warning) => new()
  {
    IsMatched = false,
    MatchedType = null,
    MatchedFamily = null,
    MatchedComponent = null,
    Warning = warning,
  };
}
