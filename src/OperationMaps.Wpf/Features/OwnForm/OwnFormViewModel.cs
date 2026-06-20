using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Word;
using OperationMaps.Wpf.Features.Form4;
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
  public sealed partial class OwnFormViewModel : ScreenViewModelBase, INavigatedTo
  {
    private readonly ProjectStore _store;
    private readonly CatalogDbContext _db;
    private readonly IWordService _wordService;
    private readonly WordFormMapLoader _mapLoader;

    // ── Form metadata ─────────────────────────────────────────────────────────

    public int FormId { get; private set; }
    public string FormNumber { get; private set; } = "";
    public string FormTitle { get; private set; } = "";

    // ── Table data ────────────────────────────────────────────────────────────

    public ObservableCollection<FormColumnVm> Columns { get; } = [];
    public ObservableCollection<ColumnListItemVm> ColumnItems { get; } = [];
    public ObservableCollection<FormParameterRowVm> Parameters { get; } = [];

    // ── Undo / Redo ───────────────────────────────────────────────────────────

    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUndo))]
    [NotifyPropertyChangedFor(nameof(CanRedo))]
    private bool _hasHistory;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

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

    public bool CanSplit  => SelectedColumn?.Component.Entry.Imported.Positions.Count > 1;
    public bool CanMerge  => SelectedColumns.Count == 2;
    private bool CanExport => Columns.Count > 0 && !IsExporting && !string.IsNullOrEmpty(FormNumber);

    public event Action<FormColumnVm>? SplitRequested;

    // ── Constructor ───────────────────────────────────────────────────────────

    public OwnFormViewModel(
        ProjectStore store,
        CatalogDbContext db,
        IWordService wordService,
        WordFormMapLoader mapLoader)
    {
      _store       = store       ?? throw new ArgumentNullException(nameof(store));
      _db          = db          ?? throw new ArgumentNullException(nameof(db));
      _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
      _mapLoader   = mapLoader   ?? throw new ArgumentNullException(nameof(mapLoader));
    }

    // ── INavigatedTo ──────────────────────────────────────────────────────────

    public async Task OnNavigatedToAsync(
        object? parameter = null,
        CancellationToken cancellationToken = default)
    {
      if (parameter is not int formId) return;

      FormId    = formId;
      IsLoading = true;

      try
      {
        await BuildTableAsync(cancellationToken);
      }
      finally
      {
        IsLoading = false;
      }
    }

    // ── Commands: Undo / Redo ─────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
      if (!CanUndo) return;
      var cmd = _undoStack.Pop();
      cmd.Undo();
      _redoStack.Push(cmd);
      RefreshHistory();
      RebuildColumnItems();
      ClearStaleSelection();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
      if (!CanRedo) return;
      var cmd = _redoStack.Pop();
      cmd.Execute();
      _undoStack.Push(cmd);
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
        SelectedItem     = null;
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
      var first  = SelectedColumns[0];
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
      SelectedItem   = ColumnItems.FirstOrDefault(i => i.Column == column);

      if (SelectedItem is not null)
        SelectedItem.IsSelected = true;

      var details = Parameters
          .Select(p => new ParameterDetailVm(
              column: column,
              parameterId: p.FormParameterId,
              rowNumber: p.RowNumber,
              displayName: p.DisplayName,
              ntdValue: column.GetNtdValue(p.FormParameterId),
              formula: p.Formula,
              isRequired: p.IsRequired))
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
          System.Windows.MessageBox.Show(
              $"Файл уже открыт в другой программе:\n{outputPath}\n\nЗакройте файл и повторите экспорт.",
              "Файл занят",
              System.Windows.MessageBoxButton.OK,
              System.Windows.MessageBoxImage.Warning);
          return;
        }
      }

      IsExporting = true;
      try
      {
        var templatePath = _mapLoader.GetTemplatePath(FormNumber);
        var data         = BuildWordFormData();
        var bytes        = await _wordService.ExportAsync(data, templatePath, ct);

        var tmpPath = outputPath + ".tmp";
        await File.WriteAllBytesAsync(tmpPath, bytes, ct);
        if (File.Exists(outputPath)) File.Delete(outputPath);
        File.Move(tmpPath, outputPath);
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
    {
      var paramIdToRow = Parameters
          .ToDictionary(p => p.FormParameterId, p => p.RowNumber);

      var components = Columns
          .Select(col => BuildComponentData(col, paramIdToRow))
          .ToList();

      return new WordFormData
      {
        FormNumber          = FormNumber,
        DocumentDesignation = _store.DocumentNumber ?? _store.ProjectName ?? "",
        Components          = components,
        HeaderFields        = new Dictionary<string, string>
        {
          ["sheetNumber"] = "1",
        },
      };
    }

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

    private async Task BuildTableAsync(CancellationToken ct)
    {
      Columns.Clear();
      Parameters.Clear();
      _undoStack.Clear();
      _redoStack.Clear();
      RefreshHistory(); // ← reset Undo/Redo buttons state for the new form

      var form = await _db.Forms
          .Include(f => f.Parameters)
          .FirstOrDefaultAsync(f => f.Id == FormId, ct);

      if (form is null) return;

      FormNumber = form.Number;
      FormTitle  = form.Title;

      var parameters = form.Parameters.OrderBy(p => p.RowNumber).ToList();

      var relevantComponents = _store.Components
          .Where(c => c.Entry.MatchResult.MatchedComponent?.OwnFormId == FormId
                   || c.Entry.MatchResult.MatchedComponent?.OwnForm?.Id == FormId)
          .ToList();

      var componentIds = relevantComponents
          .Select(c => c.Entry.MatchResult.MatchedComponent?.Id)
          .Where(id => id is not null)
          .Select(id => id!.Value)
          .ToHashSet();

      var allNtdValues = componentIds.Count > 0
          ? await _db.ComponentNtdValues
              .Where(v => componentIds.Contains(v.ComponentId)
                       && v.FormParameter.FormId == FormId)
              .ToListAsync(ct)
          : [];

      var ntdByComponent = allNtdValues
          .GroupBy(v => v.ComponentId)
          .ToDictionary(g => g.Key, g => g.ToList());

      foreach (var component in relevantComponents)
      {
        var column      = new FormColumnVm(component);
        var componentId = component.Entry.MatchResult.MatchedComponent?.Id;

        if (componentId is not null && ntdByComponent.TryGetValue(componentId.Value, out var ntdValues))
          foreach (var ntd in ntdValues)
            column.NtdValues[ntd.FormParameterId] = ntd.Value;

        Columns.Add(column);
      }

      foreach (var param in parameters)
        Parameters.Add(new FormParameterRowVm(param, Columns));

      foreach (var col in Columns)
        ColumnItems.Add(new ColumnListItemVm(col, Parameters));
    }

    // ── Private: helpers ──────────────────────────────────────────────────────

    private static WordComponentData BuildComponentData(
        FormColumnVm column,
        Dictionary<int, int> paramIdToRow)
    {
      var schemeValues = column.CellValues
          .Where(kv => paramIdToRow.ContainsKey(kv.Key) && !string.IsNullOrEmpty(kv.Value))
          .ToDictionary(kv => paramIdToRow[kv.Key], kv => kv.Value);

      var ntdValues = column.NtdValues
          .Where(kv => paramIdToRow.ContainsKey(kv.Key) && !string.IsNullOrEmpty(kv.Value))
          .ToDictionary(kv => paramIdToRow[kv.Key], kv => kv.Value);

      var noteLines = column.Notes.Values
          .SelectMany(notes => notes)
          .OrderBy(n => n.Order)
          .Select(n => $"{n.Marker} {n.NoteText.Trim()}")
          .ToList();

      return new WordComponentData
      {
        Name         = column.Name,
        Positions    = PositionRangeFormatter.Format(column.Component.Entry.Imported.Positions),
        Quantity     = column.Component.Entry.Imported.Positions.Count.ToString(),
        SchemeValues = schemeValues,
        NtdValues    = ntdValues,
        Note         = string.Join("\n", noteLines),
      };
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
      command.Execute();
      _undoStack.Push(command);
      _redoStack.Clear();
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
      HasHistory = _undoStack.Count > 0 || _redoStack.Count > 0;
      OnPropertyChanged(nameof(CanUndo));
      OnPropertyChanged(nameof(CanRedo));

      UndoCommand.NotifyCanExecuteChanged();
      RedoCommand.NotifyCanExecuteChanged();
    }
  }
}
 