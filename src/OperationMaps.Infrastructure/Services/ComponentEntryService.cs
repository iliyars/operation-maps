using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Infrastructure.Services;

public sealed class ComponentEntryService : IComponentEntryService
{
  private readonly IDbContextFactory<CatalogDbContext> _dbFactory;

  public ComponentEntryService(IDbContextFactory<CatalogDbContext> dbFactory)
  {
    _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
  }

  public async Task<Component> CreateComponentAsync(NewComponentInput input, CancellationToken ct = default)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    ArgumentNullException.ThrowIfNull(input);

    // A second component with the same full name would make matching
    // ambiguous (which one does a re-import pick?) — refuse it up front,
    // before a family gets created for nothing.
    var fullName = input.FullName.Trim();
    if (fullName.Length == 0)
      throw new InvalidOperationException("Введите полное наименование компонента.");

    // All writes belong to one operation, including intermediate saves for IDs.
    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    if (await db.Components.AnyAsync(c => c.FullName == fullName, ct))
      throw new InvalidOperationException($"Компонент «{fullName}» уже есть в справочнике.");

    // ── 1. Resolve or create the Family ─────────────────────────────────────
    Family family;

    if (input.ExistingFamilyId is { } familyId)
    {
      family = await db.Families
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
      db.Families.Add(family);
      await db.SaveChangesAsync(ct); // need family.Id before adding NtdValues
    }

    // Link the family to the chosen own-form, if not already linked.
    var hasOwnFormLink = await db.FamilyForms
        .AnyAsync(ff => ff.FamilyId == family.Id && ff.FormId == input.OwnFormId, ct);

    if (!hasOwnFormLink)
      db.FamilyForms.Add(new FamilyForm { FamilyId = family.Id, FormId = input.OwnFormId });

    // ── 2. Form 4 values — only fill gaps, never overwrite existing ─────────
    var existingParamIds = family.NtdValues.Select(v => v.FormParameterId).ToHashSet();

    foreach (var (formParameterId, value) in input.Form4Values)
    {
      if (string.IsNullOrWhiteSpace(value)) continue;
      if (existingParamIds.Contains(formParameterId)) continue; // don't overwrite

      db.FamilyNtdValues.Add(new FamilyNtdValue
      {
        FamilyId = family.Id,
        FormParameterId = formParameterId,
        Value = value.Trim(),
      });
    }

    // ── 3. Create the Component ──────────────────────────────────────────────
    var component = new Component
    {
      FullName = fullName,
      FamilyId = family.Id,
      OwnFormId = input.OwnFormId,
      NeedsAdminReview = input.NeedsAdminReview,
    };
    db.Components.Add(component);
    await db.SaveChangesAsync(ct); // need component.Id before adding NtdValues/PinValues

    // ── 4. Own-form NTD values (includes any optional-row values) ────────────
    foreach (var (formParameterId, value) in input.OwnFormValues)
    {
      if (string.IsNullOrWhiteSpace(value)) continue;

      db.ComponentNtdValues.Add(new ComponentNtdValue
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

      db.ComponentPinValues.Add(new ComponentPinValue
      {
        ComponentId = component.Id,
        FormParameterId = formParameterId,
        Pins = pins.Trim(),
      });
    }

    await db.SaveChangesAsync(ct);

    // Reload with navigation properties populated for the caller (matcher/UI).
    var created = await db.Components
        .Include(c => c.Family)
        .Include(c => c.OwnForm)
        .FirstAsync(c => c.Id == component.Id, ct);

    await transaction.CommitAsync(ct);
    return created;
  }

  public async Task MarkVerifiedAsync(int componentId, CancellationToken ct = default)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    var component = await db.Components.FirstOrDefaultAsync(c => c.Id == componentId, ct)
        ?? throw new InvalidOperationException("Компонент не найден — возможно, он удалён.");

    component.NeedsAdminReview = false;
    await db.SaveChangesAsync(ct);
  }
}
