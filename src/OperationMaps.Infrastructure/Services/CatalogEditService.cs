using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Infrastructure.Services;

public sealed class CatalogEditService : ICatalogEditService
{
  private readonly CatalogDbContext _db;

  public CatalogEditService(CatalogDbContext db)
  {
    _db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task UpdateFamilyAsync(
      int familyId, string name, IReadOnlyDictionary<int, string> form4Values, CancellationToken ct = default)
  {
    var family = await _db.Families
        .Include(f => f.NtdValues)
        .FirstOrDefaultAsync(f => f.Id == familyId, ct)
        ?? throw new InvalidOperationException("Семейство не найдено — возможно, оно удалено.");

    var newName = name.Trim();
    if (newName.Length == 0)
      throw new InvalidOperationException("Введите название семейства.");

    // XML import finds a family by (type, name) — two with the same name
    // in one type would make that ambiguous.
    if (await _db.Families.AnyAsync(
            f => f.Id != familyId && f.ComponentTypeId == family.ComponentTypeId && f.Name == newName, ct))
      throw new InvalidOperationException($"Семейство «{newName}» уже есть у этого типа компонентов.");

    family.Name = newName;

    foreach (var (paramId, raw) in form4Values)
    {
      var value = raw.Trim();
      var existing = family.NtdValues.FirstOrDefault(v => v.FormParameterId == paramId);

      if (value.Length == 0)
      {
        if (existing is not null) _db.FamilyNtdValues.Remove(existing);
      }
      else if (existing is not null)
        existing.Value = value;
      else
        _db.FamilyNtdValues.Add(new() { FamilyId = familyId, FormParameterId = paramId, Value = value });
    }

    await _db.SaveChangesAsync(ct);
  }

  public async Task UpdateComponentAsync(
      int componentId,
      string fullName,
      IReadOnlyDictionary<int, string> values,
      IReadOnlyDictionary<int, string> pins,
      CancellationToken ct = default)
  {
    var component = await _db.Components
        .Include(c => c.NtdValues)
        .Include(c => c.PinValues)
        .FirstOrDefaultAsync(c => c.Id == componentId, ct)
        ?? throw new InvalidOperationException("Компонент не найден — возможно, он удалён.");

    var newName = fullName.Trim();
    if (newName.Length == 0)
      throw new InvalidOperationException("Введите полное наименование компонента.");

    if (await _db.Components.AnyAsync(c => c.Id != componentId && c.FullName == newName, ct))
      throw new InvalidOperationException($"Компонент «{newName}» уже есть в справочнике.");

    component.FullName = newName;

    foreach (var (paramId, raw) in values)
    {
      var value = raw.Trim();
      var existing = component.NtdValues.FirstOrDefault(v => v.FormParameterId == paramId);

      if (value.Length == 0)
      {
        if (existing is not null) _db.ComponentNtdValues.Remove(existing);
      }
      else if (existing is not null)
        existing.Value = value;
      else
        _db.ComponentNtdValues.Add(new() { ComponentId = componentId, FormParameterId = paramId, Value = value });
    }

    foreach (var (paramId, raw) in pins)
    {
      var value = raw.Trim();
      var existing = component.PinValues.FirstOrDefault(v => v.FormParameterId == paramId);

      if (value.Length == 0)
      {
        if (existing is not null) _db.ComponentPinValues.Remove(existing);
      }
      else if (existing is not null)
        existing.Pins = value;
      else
        _db.ComponentPinValues.Add(new() { ComponentId = componentId, FormParameterId = paramId, Pins = value });
    }

    await _db.SaveChangesAsync(ct);
  }

  public async Task<bool> DeleteComponentAsync(int componentId, CancellationToken ct = default)
  {
    var component = await _db.Components.FirstOrDefaultAsync(c => c.Id == componentId, ct)
        ?? throw new InvalidOperationException("Компонент не найден — возможно, он уже удалён.");

    var familyId = component.FamilyId;

    // Values, pins and notes go with it (cascade delete in the schema).
    _db.Components.Remove(component);
    await _db.SaveChangesAsync(ct);

    return !await _db.Components.AnyAsync(c => c.FamilyId == familyId, ct);
  }

  public async Task DeleteFamilyAsync(int familyId, CancellationToken ct = default)
  {
    var family = await _db.Families.FirstOrDefaultAsync(f => f.Id == familyId, ct)
        ?? throw new InvalidOperationException("Семейство не найдено — возможно, оно уже удалено.");

    if (await _db.Components.AnyAsync(c => c.FamilyId == familyId, ct))
      throw new InvalidOperationException(
          "В семействе ещё есть компоненты — сначала удалите их.");

    // Form 4 values, notes and form links go with it (cascade delete in the schema).
    _db.Families.Remove(family);
    await _db.SaveChangesAsync(ct);
  }

  public async Task SetDefaultLoadFactorParameterAsync(int formId, int? parameterId, CancellationToken ct = default)
  {
    var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == formId, ct)
        ?? throw new InvalidOperationException("Форма не найдена.");

    if (parameterId is { } id && !await _db.FormParameters.AnyAsync(p => p.Id == id && p.FormId == formId, ct))
      throw new InvalidOperationException("Параметр не принадлежит этой форме.");

    form.DefaultLoadFactorParameterId = parameterId;
    await _db.SaveChangesAsync(ct);
  }
}
