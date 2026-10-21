namespace OperationMaps.Application.Services;

/// <summary>
/// Admin edits of existing catalog records ("Справочник ЭРИ"). Values are
/// keyed by FormParameterId; an empty value removes the stored one (shown
/// as "—"). Invalid input throws <see cref="InvalidOperationException"/>
/// with a user-facing message.
/// </summary>
public interface ICatalogEditService
{
  /// <summary>Family name + its Form 4 ("по НТД") values.</summary>
  Task UpdateFamilyAsync(int familyId, string name, IReadOnlyDictionary<int, string> form4Values, CancellationToken ct = default);

  /// <summary>Component full name + its own-form values and pin numbers.</summary>
  Task UpdateComponentAsync(
      int componentId,
      string fullName,
      IReadOnlyDictionary<int, string> values,
      IReadOnlyDictionary<int, string> pins,
      CancellationToken ct = default);

  /// <summary>Deletes the component with its values, pins and notes. Returns true when its family is now empty.</summary>
  Task<bool> DeleteComponentAsync(int componentId, CancellationToken ct = default);

  /// <summary>Deletes an empty family (refuses one that still has components).</summary>
  Task DeleteFamilyAsync(int familyId, CancellationToken ct = default);
}
