using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Domain.Entities.Catalog;
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

  public async Task<MatchResult> MatchAsync(ImportedComponent imported, CancellationToken ct = default)
  {
    var typeName = imported.DetectedCategory;

    if (string.IsNullOrEmpty(typeName))
      return Unmatched($"Не удалось распарсить тип компонента: {imported.RawName}");

    // 1. Ищем тип компонента в БД
    var type = await _context.ComponentTypes
        .FirstOrDefaultAsync(t => t.Name == typeName, ct);

    if (type is null)
      return Unmatched($"Неизвестный тип компонента: {typeName}");

    // 2. Парсим семейство — передаём полное имя с типом чтобы парсер работал корректно
    var parsed = _nameParser.Parse($"{typeName} {imported.RawName}");


    Family? family = null;
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
    .FirstOrDefaultAsync(c => c.FullName == imported.RawName, ct);

    // 4. Определяем требуемые формы
    var requiredForms = family?.FamilyForms.Select(ff => ff.Form).ToList()
                     ?? [];


    return new MatchResult
    {
      IsMatched = component is not null,
      MatchedType = type,
      MatchedFamily = family,
      MatchedComponent = component,
      RequiredForm = requiredForms,
      Warning = component is null ? $"Компонент не найден: {parsed.Name}" : null
    };
  }

  public async Task<ProjectMatchResult> MatchAllAsync(
    IReadOnlyList<ImportedComponent> components,
    CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(components);

    var matched = new List<ComponentMatchEntry>(components.Count);
    var unresolved = new List<ComponentMatchEntry>();
    var warnings = new List<string>();

    foreach (var imported in components)
    {
      ct.ThrowIfCancellationRequested();

      var result = await MatchAsync(imported, ct);

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

  private static MatchResult Unmatched(string warning) => new()
  {
    IsMatched = false,
    MatchedType = null,
    MatchedFamily = null,
    MatchedComponent = null,
    Warning = warning,
  };
}
