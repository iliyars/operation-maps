using OperationMaps.Domain.Entities.Catalog;

namespace OperationMaps.Application.Services;

/// <summary>
/// Input for creating a new Family + Component from the "add unresolved
/// component" wizard. Covers both steps: Form 4 (family-level) NTD values
/// and own-form (Form67/68/64/...) NTD values, plus optional pin numbers
/// and optional second-value rows for forms that support them (Form 64).
/// </summary>
public sealed class NewComponentInput
{
  /// <summary>ComponentType.Id — already known from DetectedCategory.</summary>
  public required int ComponentTypeId { get; init; }

  /// <summary>
  /// Existing Family.Id to attach the new Component to, or null to create
  /// a new Family using <see cref="NewFamilyName"/>.
  /// </summary>
  public int? ExistingFamilyId { get; init; }

  /// <summary>Required when <see cref="ExistingFamilyId"/> is null.</summary>
  public string? NewFamilyName { get; init; }

  /// <summary>Full name of the component as it appears in the XML (field_5).</summary>
  public required string FullName { get; init; }

  /// <summary>Step 1 — Form 4 values. Key: FormParameterId.</summary>
  public required IReadOnlyDictionary<int, string> Form4Values { get; init; }

  /// <summary>Step 2 — selected own form (Form67/68/64/...).</summary>
  public required int OwnFormId { get; init; }

  /// <summary>
  /// Step 2 — own-form NTD values (the "по НТД" column), including any
  /// values entered for optional parameter rows (e.g. a second supply
  /// voltage). Key: FormParameterId.
  /// </summary>
  public required IReadOnlyDictionary<int, string> OwnFormValues { get; init; }

  /// <summary>
  /// Step 2 — pin numbers entered for the own-form's "номера выводов"
  /// column (Form 64 only). Key: FormParameterId. Empty for forms
  /// without this column.
  /// </summary>
  public IReadOnlyDictionary<int, string> PinValues { get; init; }
      = new Dictionary<int, string>();
}

public interface IComponentEntryService
{
  /// <summary>
  /// Creates (or reuses) a Family and creates a new Component inside it,
  /// with NeedsAdminReview = true. Persists FamilyNtdValue rows for any
  /// Form-4 parameter that didn't already have a value for this family
  /// (existing family values are never overwritten), ComponentNtdValue
  /// rows for the own-form values (including optional-row values), and
  /// ComponentPinValue rows for any pin numbers provided.
  /// Returns the freshly created Component, with Family and OwnForm loaded.
  /// </summary>
  Task<Component> CreateComponentAsync(NewComponentInput input, CancellationToken ct = default);
}
