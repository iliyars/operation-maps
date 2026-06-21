using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Infrastructure.Services;

public sealed class ComponentEntryService : IComponentEntryService
{
  private readonly CatalogDbContext _db;

  public ComponentEntryService(CatalogDbContext db)
  {
    _db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task<Component> CreateComponentAsync(NewComponentInput input, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(input);

    // ── 1. Resolve or create the Family ─────────────────────────────────────
    Family family;

    if (input.ExistingFamilyId is { } familyId)
    {
      family = await _db.Families
          .Include(f => f.NtdValues)
          .FirstOrDefaultAsync(f => f.Id == familyId, ct)
          ?? throw new InvalidOperationException($"Family {familyId} not found.");
    }
    else
    {
      if (string.IsNullOrWhiteSpace(input.NewFamilyName))
        throw new InvalidOperationException("NewFamilyName is required when ExistingFamilyId is null.");

      family = new Family
      {
        Name = input.NewFamilyName.Trim(),
        ComponentTypeId = input.ComponentTypeId,
      };
      _db.Families.Add(family);
      await _db.SaveChangesAsync(ct); // need family.Id before adding NtdValues
    }

    // Link the family to the chosen own-form, if not already linked.
    var hasOwnFormLink = await _db.FamilyForms
        .AnyAsync(ff => ff.FamilyId == family.Id && ff.FormId == input.OwnFormId, ct);

    if (!hasOwnFormLink)
      _db.FamilyForms.Add(new FamilyForm { FamilyId = family.Id, FormId = input.OwnFormId });

    // ── 2. Form 4 values — only fill gaps, never overwrite existing ─────────
    var existingParamIds = family.NtdValues.Select(v => v.FormParameterId).ToHashSet();

    foreach (var (formParameterId, value) in input.Form4Values)
    {
      if (string.IsNullOrWhiteSpace(value)) continue;
      if (existingParamIds.Contains(formParameterId)) continue; // don't overwrite

      _db.FamilyNtdValues.Add(new FamilyNtdValue
      {
        FamilyId = family.Id,
        FormParameterId = formParameterId,
        Value = value.Trim(),
      });
    }

    // ── 3. Create the Component ──────────────────────────────────────────────
    var component = new Component
    {
      FullName = input.FullName,
      FamilyId = family.Id,
      OwnFormId = input.OwnFormId,
      NeedsAdminReview = true,
    };
    _db.Components.Add(component);
    await _db.SaveChangesAsync(ct); // need component.Id before adding NtdValues/PinValues

    // ── 4. Own-form NTD values (includes any optional-row values) ────────────
    foreach (var (formParameterId, value) in input.OwnFormValues)
    {
      if (string.IsNullOrWhiteSpace(value)) continue;

      _db.ComponentNtdValues.Add(new ComponentNtdValue
      {
        ComponentId = component.Id,
        FormParameterId = formParameterId,
        Value = value.Trim(),
      });
    }

    // ── 5. Pin numbers (Form 64's "номера выводов" column) ───────────────────
    foreach (var (formParameterId, pins) in input.PinValues)
    {
      if (string.IsNullOrWhiteSpace(pins)) continue;

      _db.ComponentPinValues.Add(new ComponentPinValue
      {
        ComponentId = component.Id,
        FormParameterId = formParameterId,
        Pins = pins.Trim(),
      });
    }

    await _db.SaveChangesAsync(ct);

    // Reload with navigation properties populated for the caller (matcher/UI).
    return await _db.Components
        .Include(c => c.Family)
        .Include(c => c.OwnForm)
        .FirstAsync(c => c.Id == component.Id, ct);
  }
}
