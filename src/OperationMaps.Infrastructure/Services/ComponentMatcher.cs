using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Infrastructure.Services;

public sealed class ComponentMatcher : IComponentMatcher
{
  private readonly OperationMapsDbContext _context;
  private readonly IComponentNameParser _nameParser;

  public ComponentMatcher(OperationMapsDbContext context, IComponentNameParser nameParser)
  {
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _nameParser = nameParser ?? throw new ArgumentNullException(nameof(nameParser));
  }

  public async Task<MatchResult> MatchAsync(ImportedComponent imported, CancellationToken ct = default)
  {
    var parsed = _nameParser.Parse(imported.RawName);

    if (string.IsNullOrEmpty(parsed.Type))
      return Unmatched($"Не удалось распарсить тип компонента: {imported.RawName}");

    // 2. Ищем тип компонента в БД
    var type = await _context.ComponentTypes
        .FirstOrDefaultAsync(t => t.Name == parsed.Type, ct);

    if (type is null)
      return Unmatched($"Неизвестный тип компонента: {parsed.Type}");

    // 3. Ищем семейство (только для RLC типов)
    Family? family = null;
    if (!string.IsNullOrEmpty(parsed.Family) && parsed.Family != parsed.Name)
    {
      family = await _context.Families
          .FirstOrDefaultAsync(f => f.ComponentTypeId == type.Id && f.Name == parsed.Family, ct);
    }

    // 4. Ищем компонент по полному имени (очищенному от ТУ)
    var component = await _context.Components
        .FirstOrDefaultAsync(c => c.FullName == parsed.Name, ct);

    return new MatchResult
    {
      IsMatched = component is not null,
      MatchedType = type,
      MatchedFamily = family,
      MatchedComponent = component,
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
