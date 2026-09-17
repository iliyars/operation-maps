using OperationMaps.Domain.Entities.Catalog;

namespace OperationMaps.Application.Services;

/// <summary>
/// One own-form component slot (e.g. a Form67 capacitor group) found while
/// scanning a filled Word document, resolved as far as possible against the
/// catalog. Carries both what the review UI displays and — in the
/// row-number-keyed dictionaries — what <see cref="IWordCatalogImportService.ImportAsync"/>
/// needs to actually create the component; those dictionaries aren't meant
/// for display.
/// </summary>
public sealed class WordImportedComponent
{
  public required string FullName { get; init; }
  public required string OwnFormNumber { get; init; }
  public required string OwnFormTitle { get; init; }
  public required int OwnFormId { get; init; }

  /// <summary>
  /// Component type read straight from the matching Form-4 slot's
  /// "Наименование ЭРИ" cell (e.g. "Конденсатор") — the source of truth,
  /// regardless of whether a <c>ComponentType</c> with this exact name
  /// exists in the catalog yet. Null when no Form-4 slot could be matched
  /// to this component at all (Form 4 missing from the document, or its
  /// "Обозначение" text didn't prefix-match this component's full name) —
  /// then the review UI must let the user type one in before import.
  /// </summary>
  public string? DetectedTypeName { get; init; }

  /// <summary>
  /// The type name that will actually be used on import — editable by the
  /// review UI, initialized from <see cref="DetectedTypeName"/>. If no
  /// <c>ComponentType</c> with this name exists yet, one is created on
  /// import (see <see cref="IsNewComponentType"/>).
  /// </summary>
  public string? ComponentTypeName { get; set; }

  /// <summary>Id of the existing <c>ComponentType</c> matching <see cref="ComponentTypeName"/>, or null if it doesn't exist yet.</summary>
  public int? ComponentTypeId { get; set; }

  /// <summary>True when <see cref="ComponentTypeName"/> doesn't match any existing <c>ComponentType</c> — a new one will be created on import.</summary>
  public bool IsNewComponentType => !string.IsNullOrWhiteSpace(ComponentTypeName) && ComponentTypeId is null;

  /// <summary>Family name to reuse or create. Editable by the review UI.</summary>
  public string? FamilyName { get; set; }

  /// <summary>Set when <see cref="FamilyName"/> matched an existing Family for the current <see cref="ComponentTypeId"/>.</summary>
  public int? ExistingFamilyId { get; set; }

  /// <summary>True when a Component with this exact FullName already exists — import skips it.</summary>
  public bool AlreadyInCatalog { get; init; }

  /// <summary>True when a Form-4 slot was matched (so Family-level NTD data will be filled in on create).</summary>
  public bool Form4DataFound { get; init; }

  public IReadOnlyList<string> Warnings { get; init; } = [];

  // ── Payload for ImportAsync — keyed by FormParameter.RowNumber. Not for
  //    display; consumed by IWordCatalogImportService.ImportAsync only. ──

  public IReadOnlyDictionary<int, string> Form4NtdValuesByRow { get; init; } = new Dictionary<int, string>();
  public IReadOnlyDictionary<int, string> OwnFormNtdValuesByRow { get; init; } = new Dictionary<int, string>();
  public IReadOnlyDictionary<int, string> OwnFormPinValuesByRow { get; init; } = new Dictionary<int, string>();
}

public sealed class WordImportScanResult
{
  public IReadOnlyList<WordImportedComponent> Components { get; init; } = [];

  /// <summary>
  /// Form numbers whose header was found in the document but which the app
  /// doesn't (yet) support importing — no map.json, or no matching
  /// <c>Form</c> row in the catalog. Shown to the user as a heads-up.
  /// </summary>
  public IReadOnlyList<string> UnsupportedFormNumbers { get; init; } = [];
}

/// <summary>
/// Reads a filled work-order Word document (which may bundle many forms —
/// Form4, Form67, Form68, ... — each possibly spilling across several pages)
/// and turns it into catalog components, reusing the exact same
/// Family/Component creation path as the "Ввести данные компонента" wizard
/// (<see cref="IComponentEntryService"/>).
/// </summary>
public interface IWordCatalogImportService
{
  Task<WordImportScanResult> ScanAsync(string documentPath, CancellationToken ct = default);

  /// <summary>
  /// Re-resolves <see cref="WordImportedComponent.ComponentTypeId"/>,
  /// <see cref="WordImportedComponent.FamilyName"/> and
  /// <see cref="WordImportedComponent.ExistingFamilyId"/> in place for the
  /// given type name — called whenever the review UI's type field changes
  /// (whether the user picked an existing type or typed a new one).
  /// </summary>
  Task ResolveComponentTypeAsync(WordImportedComponent item, string componentTypeName, CancellationToken ct = default);

  /// <summary>
  /// Creates the component via <see cref="IComponentEntryService"/>,
  /// creating a new <c>ComponentType</c> first if
  /// <see cref="WordImportedComponent.IsNewComponentType"/> is true.
  /// Requires a non-empty <see cref="WordImportedComponent.ComponentTypeName"/>.
  /// </summary>
  Task<Component> ImportAsync(WordImportedComponent item, CancellationToken ct = default);
}
