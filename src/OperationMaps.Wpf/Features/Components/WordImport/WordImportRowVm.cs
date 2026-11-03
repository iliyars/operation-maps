using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Application.Services;

namespace OperationMaps.Wpf.Features.Components.WordImport;

/// <summary>Suggestion item for the per-row "тип компонента" autocomplete field.</summary>
public sealed record ComponentTypeOption(int Id, string Name)
{
  public override string ToString() => Name;
}

/// <summary>
/// One reviewable row in the Word-import dialog, wrapping a
/// <see cref="WordImportedComponent"/> resolved by
/// <see cref="IWordCatalogImportService.ScanAsync"/>. The type field is free
/// text pre-filled straight from Word's Form-4 field, not a closed pick list
/// — typing (or picking a suggestion) re-runs family resolution via
/// <see cref="IWordCatalogImportService.ResolveComponentTypeAsync"/>, and an
/// unrecognized type name is created in the catalog on import rather than
/// blocking the user.
/// </summary>
public sealed partial class WordImportRowVm : ObservableObject
{
  private readonly IWordCatalogImportService _importService;

  public WordImportedComponent Model { get; }

  public string FullName => Model.FullName;
  public string OwnFormLabel => $"Форма {Model.OwnFormNumber} — {Model.OwnFormTitle}";

  /// <summary>Show the editable type field instead of plain text — whenever there's no existing ComponentType id yet (no type at all, or a new one to be created).</summary>
  public bool ShowTypeEditor => Model.ComponentTypeId is null;
  public bool IsTypeResolved => !ShowTypeEditor;
  public bool CanToggleSelection => !string.IsNullOrWhiteSpace(Model.ComponentTypeName);
  public IReadOnlyList<string> Warnings => Model.Warnings;
  public string NotesDisplay => string.Join("\n\n", new[]
  {
    string.IsNullOrWhiteSpace(Model.Form4NoteText) || Model.Form4NoteText.Trim() == "—" || Model.Form4NotesByRow.Count > 0 ? "" : $"Непривязанные примечания формы 4:\n{Model.Form4NoteText}",
    string.IsNullOrWhiteSpace(Model.OwnFormNoteText) || Model.OwnFormNoteText.Trim() == "—" || Model.OwnFormNotesByRow.Count > 0 ? "" : $"Непривязанные примечания формы {Model.OwnFormNumber}:\n{Model.OwnFormNoteText}"
  }.Where(t => t.Length > 0));
  public bool HasNotes => NotesDisplay.Length > 0;
  public bool HasConditionalValues => Model.Form4NotesByRow.Count > 0 || Model.OwnFormNotesByRow.Count > 0
    || Model.AdditionalVariants.Any(v => v.Form4Notes.Count > 0 || v.OwnNotes.Count > 0);
  public string AdditionalVariantsDisplay => string.Join("\n", Model.AdditionalVariants
    .SelectMany(v => v.Form4Notes.Concat(v.OwnNotes).SelectMany(n => n.Value.Select(t => $"Примечание: {t}")))
    .Distinct());

  [ObservableProperty] private bool _isSelected;
  [ObservableProperty] private string _typeText = "";
  [ObservableProperty] private string _familyDisplay = "";
  [ObservableProperty] private string _statusText = "";
  [ObservableProperty] private bool _isImported;
  [ObservableProperty] private string? _importError;

  public WordImportRowVm(
      WordImportedComponent model,
      IWordCatalogImportService importService)
  {
    Model = model;
    _importService = importService;

    _typeText = model.ComponentTypeName ?? model.DetectedTypeName ?? "";
    _isSelected = !model.AlreadyInCatalog && CanToggleSelection;

    RefreshDisplay();
  }

  partial void OnTypeTextChanged(string value)
  {
    if (value == Model.ComponentTypeName) return;
    _ = ResolveTypeAsync(value);
  }

  private async Task ResolveTypeAsync(string typeName)
  {
    await _importService.ResolveComponentTypeAsync(Model, typeName);

    // A later keystroke may have started its own resolve while this one was
    // in flight — only apply the result that matches the current text.
    if (TypeText != typeName) return;

    IsSelected = !Model.AlreadyInCatalog && CanToggleSelection;
    OnPropertyChanged(nameof(ShowTypeEditor));
    OnPropertyChanged(nameof(IsTypeResolved));
    OnPropertyChanged(nameof(CanToggleSelection));
    RefreshDisplay();
  }

  public void MarkImported()
  {
    IsImported = true;
    IsSelected = false;
    StatusText = "Импортировано";
  }

  public void MarkFailed(string error)
  {
    ImportError = error;
    StatusText = "Ошибка";
  }

  private void RefreshDisplay()
  {
    FamilyDisplay = Model.FamilyName ?? "—";
    StatusText = Model.AlreadyInCatalog
        ? "Уже есть в каталоге"
        : string.IsNullOrWhiteSpace(Model.ComponentTypeName)
            ? "Нужен тип компонента"
            : Model.IsNewComponentType
                ? "Новый тип и семейство"
                : Model.ExistingFamilyId is not null
                    ? "Новый компонент"
                    : "Новое семейство";
  }
}
