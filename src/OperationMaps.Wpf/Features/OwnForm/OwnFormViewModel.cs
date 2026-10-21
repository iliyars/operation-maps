using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Application.Services;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Word;
using OperationMaps.Wpf.Features.OwnForm.Commands;
using OperationMaps.Wpf.Infrastructure.Commands;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Stores;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OperationMaps.Wpf.Features.OwnForm
{
  public sealed partial class OwnFormViewModel : ScreenViewModelBase, INavigatedTo, INavigatedFrom
  {
    private readonly ProjectStore _store;
    private readonly OwnFormStateService _stateService;
    private readonly IWordService _wordService;
    private readonly WordFormMapLoader _mapLoader;
    private readonly IDialogService _dialogService;

    // ── Form metadata ─────────────────────────────────────────────────────────

    public int FormId { get; private set; }
    public string FormNumber => _state?.FormNumber ?? "";
    public string FormTitle => _state?.FormTitle ?? "";

    /// <summary>
    /// The form's table data. Owned by <see cref="ProjectStore"/> (via
    /// <see cref="OwnFormStateService"/>), not by this transient ViewModel —
    /// so edits survive navigating to another screen and back.
    /// </summary>
    private OwnFormState? _state;

    /// <summary>
    /// True when the current form has a "номера выводов" column (Form 64).
    /// Drives whether the pins TextBox is rendered for parameter rows.
    /// </summary>
    [ObservableProperty] private bool _showsPins;

    /// <summary>
    /// Candidate parameters for load-factor calculation (CanBeLoadFactorBase=true)
    /// for the current form. Empty if the form has no such parameter at all.
    /// The picker in OwnFormView is only shown when this has more than one entry —
    /// with exactly one candidate there's nothing to choose.
    /// </summary>
    public ObservableCollection<LoadFactorOptionVm> LoadFactorOptions { get; } = [];

    // ── Table data ────────────────────────────────────────────────────────────

    /// <summary>Same collection instance as <see cref="OwnFormState.Columns"/> once loaded.</summary>
    public ObservableCollection<FormColumnVm> Columns => _state?.Columns ?? _noColumns;
    private readonly ObservableCollection<FormColumnVm> _noColumns = [];
    public ObservableCollection<ColumnListItemVm> ColumnItems { get; } = [];
    public ObservableCollection<FormParameterRowVm> Parameters { get; } = [];

    // ── Undo / Redo ───────────────────────────────────────────────────────────

    // Per-form history lives in OwnFormState next to the columns it edits,
    // so undo keeps working after navigating away and back. Not the same
    // object as ProjectStore.History, which is project-wide.
    private UndoRedoStack _history => _state?.History ?? _noHistory;
    private readonly UndoRedoStack _noHistory = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUndo))]
    [NotifyPropertyChangedFor(nameof(CanRedo))]
    private bool _hasHistory;

    public bool CanUndo => _history.CanUndo;
    public bool CanRedo => _history.CanRedo;

    // ── Selection ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSplit))]
    [NotifyPropertyChangedFor(nameof(CanMerge))]
    private FormColumnVm? _selectedColumn;

    [ObservableProperty] private ColumnListItemVm? _selectedItem;
    [ObservableProperty] private IReadOnlyList<ParameterDetailVm> _parameterDetails = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isExporting;

    public ObservableCollection<FormColumnVm> SelectedColumns { get; } = [];

    public bool CanSplit => SelectedColumn?.Component.Entry.Imported.Positions.Count > 1;
    public bool CanMerge => SelectedColumns.Count == 2;
    private bool CanExport => Columns.Count > 0 && !IsExporting && !string.IsNullOrEmpty(FormNumber);

    public event Action<FormColumnVm>? SplitRequested;

    // ── Constructor ───────────────────────────────────────────────────────────

    public OwnFormViewModel(
        ProjectStore store,
        OwnFormStateService stateService,
        IWordService wordService,
        WordFormMapLoader mapLoader,
        IDialogService dialogService)
    {
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
      _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
      _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
      _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    }

    // ── INavigatedTo ──────────────────────────────────────────────────────────

    public async Task OnNavigatedToAsync(
        object? parameter = null,
        CancellationToken cancellationToken = default)
    {
      if (parameter is not int formId) return;

      FormId = formId;
      IsLoading = true;

      try
      {
        await LoadStateAsync(cancellationToken);
      }
      finally
      {
        IsLoading = false;
      }
    }

    public Task OnNavigatedFromAsync(CancellationToken cancellationToken = default)
    {
      // The state outlives this ViewModel — don't let it keep us alive.
      if (_state is not null)
        _state.LoadFactorRecalculated -= OnLoadFactorRecalculated;
      return Task.CompletedTask;
    }

    // ── Commands: Undo / Redo ─────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
      if (!_history.Undo()) return;
      RefreshHistory();
      RebuildColumnItems();
      ClearStaleSelection();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
      if (!_history.Redo()) return;
      RefreshHistory();
      RebuildColumnItems();
      ClearStaleSelection();
    }

    /// <summary>
    /// After Undo/Redo, Columns may no longer contain the previously
    /// selected FormColumnVm instance (Split/Merge replace columns
    /// rather than mutate them in place). Reset selection in that case
    /// to avoid the detail panel pointing at a removed column.
    /// </summary>
    private void ClearStaleSelection()
    {
      if (SelectedColumn is not null && !Columns.Contains(SelectedColumn))
      {
        SelectedColumn = null;
        if (SelectedItem is not null)
          SelectedItem.IsSelected = false;
        SelectedItem = null;
        ParameterDetails = [];
      }
      ClearMultiSelection();
    }

    // ── Commands: Split / Merge ───────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanSplit))]
    private void Split()
    {
      if (SelectedColumn is null) return;
      SplitRequested?.Invoke(SelectedColumn);
    }

    [RelayCommand(CanExecute = nameof(CanMerge))]
    private void Merge()
    {
      var first = SelectedColumns[0];
      var second = SelectedColumns[1];
      ExecuteCommand(new MergeColumnsCommand(Columns, first, second));
      RebuildColumnItems();
      ClearMultiSelection();
    }

    [RelayCommand]
    private void SelectColumn(FormColumnVm column)
    {
      // Plain click resets any active multi-selection for Merge
      ClearMultiSelection();

      if (SelectedItem is not null)
        SelectedItem.IsSelected = false;

      SelectedColumn = column;
      SelectedItem = ColumnItems.FirstOrDefault(i => i.Column == column);

      if (SelectedItem is not null)
        SelectedItem.IsSelected = true;

      var details = Parameters
          .Where(p => !p.IsOptional) // optional rows render via the primary row, never as their own row
          .Select(p =>
          {
            var optionalParamId = p.OptionalCounterpartId ?? 0;
            var hasOptionalRow = optionalParamId != 0 && column.HasOptionalRow(optionalParamId);

            return new ParameterDetailVm(
                column: column,
                parameterId: p.FormParameterId,
                rowNumber: p.RowNumber,
                displayName: p.DisplayName,
                ntdValue: column.GetNtdValue(p.FormParameterId),
                formula: p.Formula,
                isRequired: p.IsRequired,
                showPins: ShowsPins,
                pinsValue: column.GetPinValue(p.FormParameterId),
                hasOptionalRow: hasOptionalRow,
                optionalFormParameterId: optionalParamId,
                optionalNtdValue: hasOptionalRow ? column.OptionalNtdValues[optionalParamId] : "—",
                optionalPinsValue: hasOptionalRow ? column.GetPinValue(optionalParamId) : "",
                isLoadFactorResult: p.IsLoadFactorResult);
          })
          .ToList();

      var derivedRows = details.Where(d => d.IsDerived).ToList();
      if (derivedRows.Count > 0)
      {
        foreach (var sourceRow in details.Where(d => !d.IsDerived))
        {
          sourceRow.PropertyChanged += (_, e) =>
          {
            if (e.PropertyName == nameof(ParameterDetailVm.SchemeValue))
              foreach (var derived in derivedRows)
                derived.Recalculate(details);

            SelectedItem?.Refresh();
          };
        }

        foreach (var derived in derivedRows)
        {
          derived.PropertyChanged += (_, e) =>
          {
            if (e.PropertyName == nameof(ParameterDetailVm.SchemeValue))
              SelectedItem?.Refresh();
          };
        }

        foreach (var derived in derivedRows)
          derived.Recalculate(details);
      }

      ParameterDetails = details;
    }

    /// <summary>
    /// Ctrl+Click handler — toggles a column in/out of <see cref="SelectedColumns"/>
    /// for the Merge operation, without touching the single-selection detail panel.
    /// At most 2 columns are kept: a third Ctrl+Click drops the oldest one
    /// before adding the new one (FIFO), matching common "pick exactly 2" UX.
    /// </summary>
    [RelayCommand]
    private void ToggleColumnSelection(FormColumnVm column)
    {
      var item = ColumnItems.FirstOrDefault(i => i.Column == column);

      if (SelectedColumns.Contains(column))
      {
        SelectedColumns.Remove(column);
        if (item is not null) item.IsMultiSelected = false;
      }
      else
      {
        if (SelectedColumns.Count >= 2)
        {
          var oldest = SelectedColumns[0];
          SelectedColumns.RemoveAt(0);
          var oldestItem = ColumnItems.FirstOrDefault(i => i.Column == oldest);
          if (oldestItem is not null) oldestItem.IsMultiSelected = false;
        }

        SelectedColumns.Add(column);
        if (item is not null) item.IsMultiSelected = true;
      }

      MergeCommand.NotifyCanExecuteChanged();
    }

    private void ClearMultiSelection()
    {
      foreach (var col in SelectedColumns)
      {
        var item = ColumnItems.FirstOrDefault(i => i.Column == col);
        if (item is not null) item.IsMultiSelected = false;
      }
      SelectedColumns.Clear();
      MergeCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SaveColumn() => SelectedItem?.Refresh();

    // ── Commands: Word export ─────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportWordAsync(CancellationToken ct = default)
    {
      var formsFolder = _store.FormsFolder;
      if (formsFolder is null) return;

      Directory.CreateDirectory(formsFolder);

      var outputPath = _store.GetFormDocumentPath(FormNumber)!;

      // Check if the file is currently open (e.g. in Word)
      if (File.Exists(outputPath))
      {
        try
        {
          using var fs = File.Open(outputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
          _dialogService.ShowWarning(
              $"Файл уже открыт в другой программе:\n{outputPath}\n\nЗакройте файл и повторите экспорт.",
              "Файл занят");
          return;
        }
      }

      IsExporting = true;
      try
      {
        var templatePath = _mapLoader.GetTemplatePath(FormNumber);
        var data = BuildWordFormData();
        var bytes = await _wordService.ExportAsync(data, templatePath, ct);

        var tmpPath = outputPath + ".tmp";
        await File.WriteAllBytesAsync(tmpPath, bytes, ct);
        if (File.Exists(outputPath)) File.Delete(outputPath);
        File.Move(tmpPath, outputPath);
      }
      catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
      {
        // map.json/template.docx missing or malformed for this form — a
        // config gap, not something the user can fix by retrying. Surface
        // it instead of crashing (no global exception handler exists).
        _dialogService.ShowError(
            $"Не удалось экспортировать форму {FormNumber}: отсутствуют или повреждены " +
            $"файлы шаблона (map.json/template.docx).\n\n{ex.Message}",
            "Экспорт невозможен");
      }
      finally
      {
        IsExporting = false;
      }
    }

    partial void OnIsExportingChanged(bool value)
        => ExportWordCommand.NotifyCanExecuteChanged();

    // ── Word data builder ─────────────────────────────────────────────────────

    public WordFormData BuildWordFormData()
        => OwnFormWordDataBuilder.Build(
            FormNumber,
            _store.DocumentNumber ?? _store.ProjectName ?? "",
            Parameters,
            Columns);

    // ── Split helpers (called from View code-behind) ──────────────────────────

    public void ExecuteSplit(
        FormColumnVm original,
        IReadOnlyList<string> leftPositions,
        IReadOnlyList<string> rightPositions)
    {
      ExecuteCommand(new SplitColumnCommand(Columns, original, leftPositions, rightPositions));
      RebuildColumnItems();
    }

    public void ExecuteMultiSplit(
        FormColumnVm original,
        IReadOnlyList<IReadOnlyList<string>> groups)
    {
      if (groups.Count < 2) return;
      ExecuteCommand(new MultiSplitColumnCommand(Columns, original, groups));
      RebuildColumnItems();
    }

    // ── Property-changed hooks ────────────────────────────────────────────────

    partial void OnSelectedColumnChanged(FormColumnVm? value)
    {
      SplitCommand.NotifyCanExecuteChanged();
      MergeCommand.NotifyCanExecuteChanged();
    }

    // ── Private: table builder ────────────────────────────────────────────────

    private async Task LoadStateAsync(CancellationToken ct)
    {
      if (_state is not null)
        _state.LoadFactorRecalculated -= OnLoadFactorRecalculated;

      Parameters.Clear();
      ColumnItems.Clear();
      LoadFactorOptions.Clear();

      _state = await _stateService.GetAsync(FormId, ct);

      OnPropertyChanged(nameof(Columns));
      OnPropertyChanged(nameof(FormNumber));
      OnPropertyChanged(nameof(FormTitle));
      RefreshHistory();
      ExportWordCommand.NotifyCanExecuteChanged();

      if (_state is null) return;

      _state.LoadFactorRecalculated += OnLoadFactorRecalculated;

      foreach (var option in _state.LoadFactorOptions)
        LoadFactorOptions.Add(option);

      foreach (var param in _state.Parameters)
        Parameters.Add(param);

      ShowsPins = _state.ShowsPins;

      RebuildColumnItems();
      ExportWordCommand.NotifyCanExecuteChanged();
    }

    // ── Private: helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="OwnFormState"/> already wrote the new load-factor result
    /// into the column; refresh what's on screen for it.
    /// </summary>
    private void OnLoadFactorRecalculated(FormColumnVm column)
    {
      var resultParamId = _state?.LoadFactorResultParameterId;
      if (resultParamId is null) return;

      // Each ParameterDetailVm snapshots SchemeValue at construction time
      // rather than binding live to the column, so push the value into the
      // visible detail row of the selected column explicitly.
      if (SelectedColumn == column)
      {
        var resultDetail = ParameterDetails.FirstOrDefault(
            d => d.FormParameterId == resultParamId.Value);
        if (resultDetail is not null)
          resultDetail.SchemeValue = column.LoadFactorValue;
      }

      // Fill status changed without going through the normal SchemeValue setter.
      ColumnItems.FirstOrDefault(i => i.Column == column)?.Refresh();
    }

    private void RebuildColumnItems()
    {
      ColumnItems.Clear();
      foreach (var col in Columns)
        ColumnItems.Add(new ColumnListItemVm(col, Parameters));
    }

    private void RefreshAllItems()
    {
      foreach (var item in ColumnItems)
        item.Refresh();
    }

    private void ExecuteCommand(IUndoableCommand command)
    {
      _history.Execute(command);
      RefreshHistory();
    }

    /// <summary>
    /// Updates CanUndo/CanRedo-dependent state and notifies the generated
    /// RelayCommands so their bound buttons re-evaluate IsEnabled.
    /// OnPropertyChanged alone does NOT do this — [RelayCommand(CanExecute=...)]
    /// commands only re-check when NotifyCanExecuteChanged() is called explicitly.
    /// </summary>
    private void RefreshHistory()
    {
      HasHistory = _history.CanUndo || _history.CanRedo;
      OnPropertyChanged(nameof(CanUndo));
      OnPropertyChanged(nameof(CanRedo));

      UndoCommand.NotifyCanExecuteChanged();
      RedoCommand.NotifyCanExecuteChanged();
    }
  }
}
