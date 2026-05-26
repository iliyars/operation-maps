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
    _context = context;
    _nameParser = nameParser;
  }

  public async Task<MatchResult> MatchAsync(ImportedComponent imported, CancellationToken ct = default)
  {
    // 1. Парсим имя компонента
    var parsed = _nameParser.Parse(imported.RawName);

    if (string.IsNullOrEmpty(parsed.Type))
    {
      return new MatchResult
      {
        IsMatched = false,
        MatchedType = null,
        MatchedFamily = null,
        MatchedComponent = null,
        Warning = $"Не удалось распарсить тип компонента: {imported.RawName}"
      };
    }

    // 2. Ищем тип компонента в БД
    var type = await _context.ComponentTypes
        .FirstOrDefaultAsync(t => t.Name == parsed.Type, ct);

    if (type is null)
    {
      return new MatchResult
      {
        IsMatched = false,
        MatchedType = null,
        MatchedFamily = null,
        MatchedComponent = null,
        Warning = $"Неизвестный тип компонента: {parsed.Type}"
      };
    }

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
}
