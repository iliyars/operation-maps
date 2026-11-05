using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Application.Services;
using OperationMaps.Infrastructure.Word;

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
  private CancellationTokenSource? _typeResolutionCts;
  private int _typeResolutionVersion;
  private bool _typeResolutionFailed;
  public Task TypeResolutionTask { get; private set; } = Task.CompletedTask;

  public WordImportedComponent Model { get; }

  public string FullName => Model.FullName;
  public int Number { get; init; }
  public string OwnFormLabel => $"Форма {Model.OwnFormNumber} — {Model.OwnFormTitle}";

  /// <summary>Show the editable type field instead of plain text — whenever there's no existing ComponentType id yet (no type at all, or a new one to be created).</summary>
  public bool ShowTypeEditor => Model.ComponentTypeId is null;
  public bool IsTypeResolved => !ShowTypeEditor;
  public bool CanToggleSelection => !IsResolvingType && !_typeResolutionFailed
      && Model.ImportErrors.Count == 0
      && !string.IsNullOrWhiteSpace(Model.ComponentTypeName);
  public IReadOnlyList<string> Warnings => Model.Warnings.Concat(Model.ImportErrors).Distinct().ToArray();
  public string NotesDisplay => string.Join("\n\n", new[]
  {
    !WordNoteParser.HasContent(Model.Form4NoteText) || Model.ImportErrors.Count == 0 && Model.Form4NotesByRow.Count > 0 ? "" : $"Примечания формы 4 для проверки:\n{Model.Form4NoteText}",
    !WordNoteParser.HasContent(Model.OwnFormNoteText) || Model.ImportErrors.Count == 0 && Model.OwnFormNotesByRow.Count > 0 ? "" : $"Примечания формы {Model.OwnFormNumber} для проверки:\n{Model.OwnFormNoteText}"
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
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanToggleSelection))]
  private bool _isResolvingType;

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
    TypeResolutionTask = ResolveTypeAsync(value);
  }

  private async Task ResolveTypeAsync(string typeName)
  {
    _typeResolutionCts?.Cancel();
    using var operation = new CancellationTokenSource();
    _typeResolutionCts = operation;
    var version = ++_typeResolutionVersion;
    IsResolvingType = true;
    _typeResolutionFailed = false;
    IsSelected = false;
    ImportError = null;
    StatusText = "Определение типа и семейства…";
    // The service mutates its input; each request must have its own draft.
    var draft = new WordImportedComponent
    {
      FullName = Model.FullName, OwnFormNumber = Model.OwnFormNumber,
      OwnFormTitle = Model.OwnFormTitle, OwnFormId = Model.OwnFormId,
    };
    try
    {
      await _importService.ResolveComponentTypeAsync(draft, typeName, operation.Token);
      if (version != _typeResolutionVersion || operation.IsCancellationRequested) return;
      Model.ComponentTypeName = draft.ComponentTypeName;
      Model.ComponentTypeId = draft.ComponentTypeId;
      Model.FamilyName = draft.FamilyName;
      Model.ExistingFamilyId = draft.ExistingFamilyId;
      RefreshDisplay();
    }
    catch (OperationCanceledException) when (operation.IsCancellationRequested) { }
    catch (Exception ex)
    {
      if (version != _typeResolutionVersion) return;
      _typeResolutionFailed = true;
      ImportError = $"Не удалось определить тип компонента: {ex.Message}";
      StatusText = "Ошибка определения типа";
    }
    finally
    {
      if (version == _typeResolutionVersion)
      {
        _typeResolutionCts = null;
        IsResolvingType = false;
        OnPropertyChanged(nameof(ShowTypeEditor));
        OnPropertyChanged(nameof(IsTypeResolved));
        OnPropertyChanged(nameof(CanToggleSelection));
        IsSelected = !Model.AlreadyInCatalog && CanToggleSelection;
      }
    }
  }

  public void CancelTypeResolution()
  {
    ++_typeResolutionVersion;
    _typeResolutionCts?.Cancel();
    _typeResolutionCts = null;
    IsSelected = false;
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
    StatusText = Model.ImportErrors.Count > 0 ? "Исправьте ошибки в Word" : Model.AlreadyInCatalog
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
