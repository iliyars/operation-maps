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
using OperationMaps.Wpf.Services;

namespace OperationMaps.Wpf.Features.OwnForm
{
  public sealed partial class OwnFormViewModel : ScreenViewModelBase, INavigatedTo, INavigatedFrom
  {
    private readonly ProjectStore _store;
    private readonly OwnFormStateService _stateService;
    private readonly IWordService _wordService;
    private readonly WordFormMapLoader _mapLoader;
    private readonly IDialogService _dialogService;
    private readonly SessionStore _session;
    private readonly ICatalogEditService _catalogEdit;
    private readonly OperationMaps.Wpf.Shell.ShellViewModel _shell;
    private readonly UserPreferences _preferences;
    internal UserPreferences WorkspacePreferences => _preferences;

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
    public ObservableCollection<ColumnListItemVm> VisibleColumnItems { get; } = [];
    [ObservableProperty] private bool _showOnlyIncomplete;
    [ObservableProperty] private string _missingFieldHint = "F8 — перейти к следующему пропуску";
    public string ProgressSummary => $"Групп заполнено: {ColumnItems.Count(i => i.FillStatus == FillStatus.Complete)} из {ColumnItems.Count} · Значений: {ColumnItems.Sum(i => i.FilledCount)} из {ColumnItems.Sum(i => i.TotalCount)}";
    public bool IsFilteredListEmpty => ShowOnlyIncomplete && ColumnItems.Count > 0 && VisibleColumnItems.Count == 0;
    private bool CanNextMissing => ColumnItems.Any(i => i.FilledCount < i.TotalCount);
    private (Guid ColumnId, int ParameterId)? _lastMissing;
    public event Action<FormParameterRowVm>? MissingParameterRequested;

    partial void OnShowOnlyIncompleteChanged(bool value)
    {
      ClearMultiSelection();
      RefreshProgress();
      _preferences.OwnFormOnlyIncomplete = value;
      _preferences.Save();
    }

    private void RefreshProgress()
    {
      var visible = ColumnItems.Where(i => !ShowOnlyIncomplete || i.FillStatus != FillStatus.Complete).ToList();
      foreach (var item in VisibleColumnItems.Where(i => !visible.Contains(i)).ToList()) VisibleColumnItems.Remove(item);
      for (var index = 0; index < visible.Count; index++)
        if (!VisibleColumnItems.Contains(visible[index])) VisibleColumnItems.Insert(index, visible[index]);
      OnPropertyChanged(nameof(ProgressSummary));
      OnPropertyChanged(nameof(IsFilteredListEmpty));
      NextMissingCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanNextMissing))]
    private void NextMissing()
    {
      var missing = Columns.SelectMany(c => FormCompletion.Missing(c, Parameters)
          .OrderBy(p => p.IsLoadFactorResult || p.Formula is not null ? 1 : 0)
          .ThenBy(p => p.RowNumber).Select(p => (Column: c, Parameter: p))).ToList();
      if (missing.Count == 0) return;
      var previous = missing.FindIndex(m => m.Column.Id == _lastMissing?.ColumnId && m.Parameter.FormParameterId == _lastMissing?.ParameterId);
      var next = previous >= 0 ? missing[(previous + 1) % missing.Count]
          : missing.FirstOrDefault(m => m.Column == SelectedColumn);
      if (next.Column is null) next = missing[0];
      SelectColumn(next.Column);
      _lastMissing = (next.Column.Id, next.Parameter.FormParameterId);
      MissingFieldHint = $"{next.Column.Positions} · Строка {next.Parameter.RowNumber}: {next.Parameter.Name}";
      if (next.Parameter.IsLoadFactorResult || next.Parameter.Formula is not null)
        MissingFieldHint += " — рассчитывается автоматически; проверьте исходные значения";
      MissingParameterRequested?.Invoke(next.Parameter);
    }
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
    public bool CanMerge => SelectedColumns.Count == 2
      && !ReferenceEquals(SelectedColumns[0], SelectedColumns[1])
      && string.Equals(SelectedColumns[0].Name, SelectedColumns[1].Name, StringComparison.Ordinal);
    private bool CanExport => Columns.Count > 0 && !IsExporting && !string.IsNullOrEmpty(FormNumber);

    public event Action<FormColumnVm>? SplitRequested;
    public event Action<ColumnMergePlan>? MergeConflictsRequested;

    // ── Constructor ───────────────────────────────────────────────────────────

    public OwnFormViewModel(
        ProjectStore store,
        OwnFormStateService stateService,
        IWordService wordService,
        WordFormMapLoader mapLoader,
        IDialogService dialogService,
        SessionStore session,
        ICatalogEditService catalogEdit,
        OperationMaps.Wpf.Shell.ShellViewModel shell,
        UserPreferences preferences)
    {
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
      _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
      _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
      _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
      _session = session ?? throw new ArgumentNullException(nameof(session));
      _catalogEdit = catalogEdit ?? throw new ArgumentNullException(nameof(catalogEdit));
      _shell = shell ?? throw new ArgumentNullException(nameof(shell));
      _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
      _showOnlyIncomplete = preferences.OwnFormOnlyIncomplete;
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
      // The state and the session outlive this ViewModel — don't let them keep us alive.
      if (_state is not null)
      {
        _state.CalculatedValuesChanged -= OnCalculatedValuesChanged;
        _state.Edited -= RefreshAllItems;
      }
      _session.PropertyChanged -= OnSessionChanged;
      WatchSelectedColumn(null);
      return Task.CompletedTask;
    }

    // ── Load factor: default base parameter of the form ───────────────────────

    public bool IsAdmin => _session.IsAdmin;

    /// <summary>
    /// What the form calculates the load factor from when a component has no
    /// choice of its own. Not known yet for most forms — then each component
    /// needs a choice until an admin sets the default.
    /// </summary>
    public string DefaultLoadFactorLabel
    {
      get
      {
        if (_state?.DefaultLoadFactorParameterId is { } id
            && LoadFactorOptions.FirstOrDefault(o => o.FormParameterId == id) is { } option)
          return $"По умолчанию для формы: {option.Label}";

        if (LoadFactorOptions.Any(o => o.FormParameterId == SelectedColumn?.LoadFactorBaseParameterId))
          return "Для этого компонента параметр выбран. Общий параметр по умолчанию для формы пока не задан.";

        return "Выберите параметр для этого компонента, чтобы рассчитывать коэффициент нагрузки. Общий параметр по умолчанию для формы пока не задан.";
      }
    }

    /// <summary>Admin: make the selected component's choice the form's default for everyone.</summary>
    [RelayCommand(CanExecute = nameof(CanMakeDefaultLoadFactor))]
    private async Task MakeDefaultLoadFactorAsync()
    {
      if (_state is null || SelectedColumn?.LoadFactorBaseParameterId is not { } parameterId) return;

      try
      {
        await _catalogEdit.SetDefaultLoadFactorParameterAsync(FormId, parameterId);
      }
      catch (InvalidOperationException ex)
      {
        _dialogService.ShowWarning(ex.Message, "Коэффициент нагрузки");
        return;
      }

      _state.SetDefaultLoadFactorParameter(parameterId);
      OnPropertyChanged(nameof(DefaultLoadFactorLabel));
      MakeDefaultLoadFactorCommand.NotifyCanExecuteChanged();
    }

    private bool CanMakeDefaultLoadFactor
        => IsAdmin
           && SelectedColumn?.LoadFactorBaseParameterId is { } id
           && id != _state?.DefaultLoadFactorParameterId;

    private void OnSessionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName != nameof(SessionStore.IsAdmin)) return;
      OnPropertyChanged(nameof(IsAdmin));
      MakeDefaultLoadFactorCommand.NotifyCanExecuteChanged();
    }

    private FormColumnVm? _watchedColumn;

    /// <summary>The button depends on the selected column's current choice.</summary>
    private void WatchSelectedColumn(FormColumnVm? column)
    {
      if (_watchedColumn is not null)
        _watchedColumn.PropertyChanged -= OnWatchedColumnChanged;
      _watchedColumn = column;
      if (column is not null)
        column.PropertyChanged += OnWatchedColumnChanged;
    }

    private void OnWatchedColumnChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(FormColumnVm.LoadFactorBaseParameterId))
      {
        MakeDefaultLoadFactorCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(DefaultLoadFactorLabel));
      }
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
      if (!CanMerge) return;
      var first = SelectedColumns[0];
      var second = SelectedColumns[1];
      var plan = new ColumnMergePlan(first, second,
        Parameters.ToDictionary(p => p.FormParameterId, p => $"{p.RowNumber}. {p.Name}"),
        Parameters.Where(p => p.IsLoadFactorResult || p.Formula is not null).Select(p => p.FormParameterId));
      if (plan.Conflicts.Count > 0)
      {
        MergeConflictsRequested?.Invoke(plan);
        if (!plan.Confirmed || !plan.IsResolved) return;
      }
      ExecuteCommand(new MergeColumnsCommand(Columns, first, second, plan));
      RebuildColumnItems();
      ClearMultiSelection();
    }

    [RelayCommand]
    private void SelectColumn(FormColumnVm column)
    {
      _lastMissing = null;
      MissingFieldHint = "F8 — перейти к следующему пропуску";
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
                isLoadFactorResult: p.IsLoadFactorResult)
            { RegimeCondition = column.RegimeConditions.GetValueOrDefault(p.FormParameterId, "") };
          })
          .ToList();

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
      if (!SelectedColumns.Contains(column) && SelectedColumns.Count > 0
        && !string.Equals(SelectedColumns[0].Name, column.Name, StringComparison.Ordinal)) return;
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
      using var operation = _shell.Operation.Begin($"Экспорт формы {FormNumber}…");
      try
      {
        await operation.ReportAsync($"Экспорт формы {FormNumber}…", ct);
        var templatePath = _mapLoader.GetTemplatePath(FormNumber);
        var data = BuildWordFormData();
        // "Лист" in the frame continues the set's numbering, as in the contents.
        data.FirstSheetNumber = await _shell.FirstSheetOfAsync(FormNumber, ct);
        var bytes = await _wordService.ExportAsync(data, templatePath, ct);

        await AtomicFileWriter.WriteAsync(outputPath, bytes, ct);
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
      WatchSelectedColumn(value);
      MakeDefaultLoadFactorCommand.NotifyCanExecuteChanged();
      OnPropertyChanged(nameof(DefaultLoadFactorLabel));
    }

    // ── Private: table builder ────────────────────────────────────────────────

    private async Task LoadStateAsync(CancellationToken ct)
    {
      if (_state is not null)
      {
        _state.CalculatedValuesChanged -= OnCalculatedValuesChanged;
        _state.Edited -= RefreshAllItems;
      }

      Parameters.Clear();
      ColumnItems.Clear();
      VisibleColumnItems.Clear();
      SelectedColumn = null;
      SelectedItem = null;
      ParameterDetails = [];
      _lastMissing = null;
      LoadFactorOptions.Clear();

      _state = await _stateService.GetAsync(FormId, ct);

      _session.PropertyChanged -= OnSessionChanged;
      _session.PropertyChanged += OnSessionChanged;

      OnPropertyChanged(nameof(Columns));
      OnPropertyChanged(nameof(FormNumber));
      OnPropertyChanged(nameof(FormTitle));
      RefreshHistory();
      ExportWordCommand.NotifyCanExecuteChanged();

      if (_state is null)
      {
        RefreshProgress();
        return;
      }

      _state.CalculatedValuesChanged += OnCalculatedValuesChanged;
      _state.Edited += RefreshAllItems;

      foreach (var option in _state.LoadFactorOptions)
        LoadFactorOptions.Add(option);

      foreach (var param in _state.Parameters)
        Parameters.Add(param);

      ShowsPins = _state.ShowsPins;
      OnPropertyChanged(nameof(DefaultLoadFactorLabel));

      RebuildColumnItems();
      ExportWordCommand.NotifyCanExecuteChanged();
    }

    // ── Private: helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="OwnFormState"/> already wrote the calculated values
    /// into the column; refresh them on screen without writing them back.
    /// </summary>
    private void OnCalculatedValuesChanged(FormColumnVm column)
    {
      if (SelectedColumn == column)
        foreach (var detail in ParameterDetails.Where(d => d.IsReadOnlyValue))
          detail.RefreshCalculatedValue();
      // Fill status changed without going through the normal SchemeValue setter.
      ColumnItems.FirstOrDefault(i => i.Column == column)?.Refresh();
    }

    private void RebuildColumnItems()
    {
      ColumnItems.Clear();
      foreach (var col in Columns)
        ColumnItems.Add(new ColumnListItemVm(col, Parameters));
      SelectedItem = ColumnItems.FirstOrDefault(i => i.Column == SelectedColumn);
      if (SelectedItem is not null) SelectedItem.IsSelected = true;
      if (SelectedColumn is not null && !Columns.Contains(SelectedColumn)) ClearStaleSelection();
      RefreshProgress();
    }

    private void RefreshAllItems()
    {
      foreach (var item in ColumnItems)
        item.Refresh();
      RefreshProgress();
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
