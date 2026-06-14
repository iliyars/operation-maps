using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Bibliography;
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

    public bool CanSplit => SelectedColumn?.Component.Entry.Imported.Positions.Count > 1;
    public bool CanMerge => SelectedColumns.Count == 2;
    private bool CanExport => Columns.Count > 0 && !IsExporting && !string.IsNullOrEmpty(FormNumber);

    public event Action<FormColumnVm>? SplitRequested;

    // ── Constructor ───────────────────────────────────────────────────────────

    public OwnFormViewModel(
        ProjectStore store,
        CatalogDbContext db,
        IWordService wordService,
        WordFormMapLoader mapLoader)
    {
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _db = db ?? throw new ArgumentNullException(nameof(db));
      _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
      _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
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
      RefreshAllItems();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
      if (!CanRedo) return;
      var cmd = _redoStack.Pop();
      cmd.Execute();
      _undoStack.Push(cmd);
      RefreshHistory();
      RefreshAllItems();
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
    }

    [RelayCommand]
    private void SelectColumn(FormColumnVm column)
    {
      if (SelectedItem is not null)
        SelectedItem.IsSelected = false;

      SelectedColumn = column;
      SelectedItem = ColumnItems.FirstOrDefault(i => i.Column == column);

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
        var data = BuildWordFormData();
        var bytes = await _wordService.ExportAsync(data, templatePath, ct);

        // Write to temp then replace — safety if file is locked between check and write
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

    /// <summary>
    /// Converts the current table state into a <see cref="WordFormData"/>.
    /// Called from <see cref="ExportWordCommand"/> and from
    /// <see cref="Shell.ShellViewModel"/> during full-report export.
    /// </summary>
    public WordFormData BuildWordFormData()
    {
      var paramIdToRow = Parameters
          .ToDictionary(p => p.FormParameterId, p => p.RowNumber);

      var components = Columns
          .Select(col => BuildComponentData(col, paramIdToRow))
          .ToList();

      return new WordFormData
      {
        FormNumber = FormNumber,
        DocumentDesignation = _store.DocumentNumber ?? _store.ProjectName ?? "",
        Components = components,
        HeaderFields = new Dictionary<string, string>
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

      var form = await _db.Forms
          .Include(f => f.Parameters)
          .FirstOrDefaultAsync(f => f.Id == FormId, ct);

      if (form is null) return;

      FormNumber = form.Number;
      FormTitle = form.Title;

      var parameters = form.Parameters.OrderBy(p => p.RowNumber).ToList();

      var relevantComponents = _store.Components
          .Where(c => c.Entry.MatchResult.MatchedComponent?.OwnFormId == FormId
                   || c.Entry.MatchResult.MatchedComponent?.OwnForm?.Id == FormId)
          .ToList();

      // Load ALL ComponentNtdValues for this form in one query — avoid N+1
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

      // Group by ComponentId for fast lookup
      var ntdByComponent = allNtdValues
          .GroupBy(v => v.ComponentId)
          .ToDictionary(g => g.Key, g => g.ToList());

      foreach (var component in relevantComponents)
      {
        var column = new FormColumnVm(component);
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
        Name = column.Name,
        Positions = column.Positions,
        Quantity = column.Component.Entry.Imported.Positions.Count.ToString(),
        SchemeValues = schemeValues,
        NtdValues = ntdValues,
        Note = string.Join("\n", noteLines),
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

    private void RefreshHistory()
    {
      HasHistory = _undoStack.Count > 0 || _redoStack.Count > 0;
      OnPropertyChanged(nameof(CanUndo));
      OnPropertyChanged(nameof(CanRedo));
    }
  }
}
