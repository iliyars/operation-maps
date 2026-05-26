using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Infrastructure.Services;

public sealed class ComponentMatcher : IComponentMatcher
{
  private readonly OperationMapsDbContext _context;

  public ComponentMatcher(OperationMapsDbContext context)
  {
    _context = context;
  }

  public async Task<MatchResult> MatchAsync(ImportedComponent imported, CancellationToken ct = default)
  {
    // 1. Ищем тип компонента по DetectedCategory
    var type = await _context.ComponentTypes
        .FirstOrDefaultAsync(t => t.Name == imported.DetectedCategory, ct);

    if (type is null)
    {
      return new MatchResult
      {
        IsMatched = false,
        MatchedType = null,
        MatchedFamily = null,
        MatchedComponent = null,
        Warning = $"Неизвестный тип компонента: {imported.DetectedCategory}"
      };
    }

    // 2. Для резисторов и конденсаторов пытаемся определить семейство
    Family? family = null;
    if (type.Name is "Резистор" or "Конденсатор")
    {
      family = await ParseFamilyAsync(imported.RawName, type.Id, ct);
    }

    // 3. Ищем компонент по очищенному имени
    Component? component = null;

    if (family is not null)
    {
      component = await _context.Components
          .FirstOrDefaultAsync(c => c.FamilyId == family.Id && c.FullName == imported.RawName, ct);
    }
    else if (type.Name is not ("Резистор" or "Конденсатор"))
    {
      component = await _context.Components
          .FirstOrDefaultAsync(c => c.FullName == imported.RawName, ct);
    }

    return new MatchResult
    {
      IsMatched = component is not null,
      MatchedType = type,
      MatchedFamily = family,
      MatchedComponent = component,
      Warning = component is null ? $"Компонент не найден в справочнике: {imported.RawName}" : null
    };
  }

  private async Task<Family?> ParseFamilyAsync(string componentName, int typeId, CancellationToken ct)
  {
    var rules = await _context.FamilyParsingRules
        .Where(r => r.ComponentTypeId == typeId)
        .OrderBy(r => r.Priority)
        .ToListAsync(ct);

    foreach (var rule in rules)
    {
      var match = Regex.Match(componentName, rule.Pattern, RegexOptions.IgnoreCase);
      if (match.Success)
      {
        var familyName = match.Groups["family"].Value;
        if (!string.IsNullOrEmpty(familyName))
        {
          return await _context.Families
              .FirstOrDefaultAsync(f => f.ComponentTypeId == typeId && f.Name == familyName, ct);
        }
      }
    }

    return null;
  }
}
