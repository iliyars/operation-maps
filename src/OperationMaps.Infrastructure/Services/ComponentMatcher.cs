using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Word;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Infrastructure.Services;

public sealed class ComponentMatcher : IComponentMatcher
{
  private readonly IDbContextFactory<CatalogDbContext> _dbFactory;
  private readonly IComponentNameParser _nameParser;

  public ComponentMatcher(IDbContextFactory<CatalogDbContext> dbFactory, IComponentNameParser nameParser)
  {
    _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    _nameParser = nameParser ?? throw new ArgumentNullException(nameof(nameParser));
  }

  public async Task<MatchResult> MatchAsync(
    ImportedComponent imported,
    CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(imported);
    var result = await MatchAllAsync([imported], ct);
    return result.Matched.Concat(result.Unresolved).Single().MatchResult;
  }

  // ── Batch match — 3 queries total regardless of input size ────────────────
  public async Task<ProjectMatchResult> MatchAllAsync(
    IReadOnlyList<ImportedComponent> components,
    CancellationToken ct = default)
  {
    await using var context = await _dbFactory.CreateDbContextAsync(ct);
    ArgumentNullException.ThrowIfNull(components);

    if (components.Count == 0)
      return new ProjectMatchResult
      {
        Matched = [],
        Unresolved = [],
        Warnings = [],
      };

    // ── Step 1: parse all names up-front (CPU-only, no DB) ──────────────────
    var parsed = components.ToDictionary(c => c, c => _nameParser.Parse($"{c.DetectedCategory} {c.RawName}"));

    // ── Step 2: load all needed data in 3 batch queries ─────────────────────
    var typeNames = components
      .Select(c => c.DetectedCategory)
      .Where(n => !string.IsNullOrEmpty(n))
      .Distinct()
      .ToList();

    // Query 1 — all referenced ComponentTypes
    var typesByName = await context.ComponentTypes
      .Where(t => typeNames.Contains(t.Name))
      .ToDictionaryAsync(t => t.Name, ct);

    // Include full-name families too (non-RLC components). Load by type so
    // formatting variations of a family name can be compared in memory.
    var familiesByTypeAndName = await context.Families
      .Include(f => f.FamilyForms)
        .ThenInclude(ff => ff.Form)
      .Where(f => typeNames.Contains(f.ComponentType.Name))
      .ToListAsync(ct);

    // Build a lookup keyed by (ComponentTypeId, FamilyName) for O(1) access
    var familyLookup = familiesByTypeAndName
        .ToDictionary(f => (f.ComponentTypeId, f.Name));

    // SQL cannot apply our Unicode comparison rules. Load catalog designations
    // once, index exact and normalized keys, and retain ambiguity explicitly.
    var catalogComponents = await context.Components
        .Include(c => c.OwnForm)
        .Include(c => c.Family).ThenInclude(f => f.ComponentType)
        .Include(c => c.Family).ThenInclude(f => f.FamilyForms).ThenInclude(ff => ff.Form)
        .AsNoTracking()
        .ToListAsync(ct);
    var componentsByName = catalogComponents.ToDictionary(c => c.FullName);
    var normalizedComponents = catalogComponents.ToLookup(c => ComponentNameKey.Create(c.FullName));
    var normalizedFamilies = familiesByTypeAndName.ToLookup(f => (f.ComponentTypeId, ComponentNameKey.Create(f.Name)));

    // ── Step 3: match in-memory, zero additional DB calls ───────────────────
    var matched = new List<ComponentMatchEntry>(components.Count);
    var unresolved = new List<ComponentMatchEntry>();
    var warnings = new List<string>();

    foreach (var imported in components)
    {
      ct.ThrowIfCancellationRequested();

      if (Form3DataBuilder.IsBoard(imported))
      {
        matched.Add(new ComponentMatchEntry { Imported = imported, MatchResult = new MatchResult { IsMatched = true } });
        continue;
      }

      var p = parsed[imported];
      var typeName = imported.DetectedCategory;

      if (componentsByName.TryGetValue(imported.RawName, out var exact))
      {
        matched.Add(new ComponentMatchEntry
        {
          Imported = imported,
          MatchResult = BuildResult(p, exact.Family.ComponentType, exact.Family, exact)
        });
        continue;
      }

      var candidates = normalizedComponents[ComponentNameKey.Create(imported.RawName)].ToArray();
      if (candidates.Length == 1)
      {
        var candidate = candidates[0];
        matched.Add(new ComponentMatchEntry
        {
          Imported = imported,
          MatchResult = BuildResult(p, candidate.Family.ComponentType, candidate.Family, candidate)
        });
        continue;
      }
      if (candidates.Length > 1)
      {
        var warning = $"Неоднозначное соответствие для «{imported.RawName}»: {string.Join("; ", candidates.Select(c => c.FullName))}. Уточните запись каталога.";
        unresolved.Add(new ComponentMatchEntry { Imported = imported, MatchResult = Unmatched(warning) });
        warnings.Add(warning);
        continue;
      }

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
      if (!string.IsNullOrEmpty(p.Family))
        familyLookup.TryGetValue((type.Id, p.Family), out family);

      if (family is null && !string.IsNullOrEmpty(p.Family))
      {
        var candidateFamilies = normalizedFamilies[(type.Id, ComponentNameKey.Create(p.Family))].ToArray();
        if (candidateFamilies.Length == 1) family = candidateFamilies[0];
      }
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
