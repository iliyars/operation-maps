using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Models;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Infrastructure.Services;

/// <summary>Relief data never substitutes for missing normal operating data.</summary>
public static class NormalRegimeDataCheck
{
  public static async Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> FindAsync(
      CatalogDbContext db, IReadOnlyCollection<int> componentIds, CancellationToken ct = default)
  {
    if (componentIds.Count == 0) return new Dictionary<int, IReadOnlyList<string>>();
    var regimes = await db.ComponentRegimes.AsNoTracking().Where(r => componentIds.Contains(r.ComponentId))
        .Select(r => new
        {
          r.ComponentId, r.Component.FamilyId, r.Condition,
          Values = r.Values.Select(v => new
          {
            v.FormParameterId, v.Value, v.FormParameter.RowNumber, v.FormParameter.Name,
            FormNumber = v.FormParameter.Form.Number,
          }).ToList(),
        }).ToListAsync(ct);
    var relief = regimes.Where(r => ReliefNote.IsMatch(r.Condition)).ToList();
    if (relief.Count == 0) return new Dictionary<int, IReadOnlyList<string>>();
    var ids = relief.Select(r => r.ComponentId).Distinct().ToArray();
    var families = relief.Select(r => r.FamilyId).Distinct().ToArray();
    var own = await db.ComponentNtdValues.AsNoTracking().Where(v => ids.Contains(v.ComponentId))
        .Select(v => new { v.ComponentId, v.FormParameterId, v.Value }).ToListAsync(ct);
    var family = await db.FamilyNtdValues.AsNoTracking().Where(v => families.Contains(v.FamilyId))
        .Select(v => new { v.FamilyId, v.FormParameterId, v.Value }).ToListAsync(ct);
    var normalOwn = own.Where(v => Known(v.Value)).Select(v => (v.ComponentId, v.FormParameterId)).ToHashSet();
    var normalFamily = family.Where(v => Known(v.Value)).Select(v => (v.FamilyId, v.FormParameterId)).ToHashSet();
    var ordinary = regimes.Where(r => !ReliefNote.IsMatch(r.Condition))
        .SelectMany(r => r.Values.Where(v => Known(v.Value)).Select(v => (r.ComponentId, v.FormParameterId))).ToHashSet();
    return relief.GroupBy(r => r.ComponentId).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g
        .SelectMany(r => r.Values.Where(v => Known(v.Value)
            && !ordinary.Contains((r.ComponentId, v.FormParameterId))
            && !(v.FormNumber == "4" ? normalFamily.Contains((r.FamilyId, v.FormParameterId))
                                    : normalOwn.Contains((r.ComponentId, v.FormParameterId))))
          .Select(v => $"Форма {v.FormNumber}, строка {v.RowNumber} «{v.Name}»: задано значение для облегчённого режима, но отсутствует значение для нормального режима."))
        .Distinct().ToArray());
  }

  private static bool Known(string? value)
    => !string.IsNullOrWhiteSpace(value) && value.Replace("*", "").Trim().ToLowerInvariant() is not
        ("" or "—" or "–" or "-" or "?" or "неизвестно" or "не задано");
}
