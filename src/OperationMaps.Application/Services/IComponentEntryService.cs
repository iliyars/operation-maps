
using OperationMaps.Domain.Entities.Catalog;

namespace OperationMaps.Application.Services;


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

  /// <summary>Step 2 — selected own form (Form67/68/...).</summary>
  public required int OwnFormId { get; init; }

  /// <summary>Step 2 — own-form values. Key: FormParameterId.</summary>
  public required IReadOnlyDictionary<int, string> OwnFormValues { get; init; }
}

public interface IComponentEntryService
{
  /// <summary>
  /// Creates (or reuses) a Family and creates a new Component inside it,
  /// with NeedsAdminReview = true. Persists FamilyNtdValue rows for any
  /// Form-4 parameter that didn't already have a value for this family
  /// (existing family values are never overwritten), and ComponentNtdValue
  /// rows for the own-form values.
  /// Returns the freshly created Component, with Family and OwnForm loaded.
  /// </summary>
  Task<Component> CreateComponentAsync(NewComponentInput input, CancellationToken ct = default);
}
