using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.Form4;
using OperationMaps.Wpf.Features.OwnForm.Commands;
using OperationMaps.Wpf.Infrastructure.Commands;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Stores;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OperationMaps.Wpf.Features.OwnForm
{

  public partial class OwnFormViewModel : ScreenViewModelBase, INavigatedTo
  {

    private readonly ProjectStore _store;
    private readonly CatalogDbContext _db;

    // Form metadata
    public int FormId { get; private set; }
    public string FormNumber { get; private set; } = "";
    public string FormTitle { get; private set; } = "";

    // Table data
    public ObservableCollection<FormColumnVm> Columns { get; } = [];
    public ObservableCollection<ColumnListItemVm> ColumnItems { get; } = [];
    public ObservableCollection<FormParameterRowVm> Parameters { get; } = [];

    // Undo/Redo — per form instance
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUndo))]
    [NotifyPropertyChangedFor(nameof(CanRedo))]
    private bool _hasHistory;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    // Selection
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSplit))]
    [NotifyPropertyChangedFor(nameof(CanMerge))]
    private FormColumnVm? _selectedColumn;

    [ObservableProperty]
    private ColumnListItemVm? _selectedItem;

    public ObservableCollection<FormColumnVm> SelectedColumns { get; } = [];

    public bool CanSplit => SelectedColumn?.Component.Entry.Imported.Positions.Count > 1;
    public bool CanMerge => SelectedColumns.Count == 2;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty]
    private IReadOnlyList<ParameterDetailVm> _parameterDetails = [];
    public OwnFormViewModel(ProjectStore store, CatalogDbContext db)
    {
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _db = db ?? throw new ArgumentNullException(nameof(db));
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

    // ── Commands ──────────────────────────────────────────────────────────────

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
      if (SelectedColumn is not null)
        SelectedColumn.IsSelected = false;

      SelectedColumn = column;
      // Find and select the list item
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

            // Refresh status in list
            SelectedItem?.Refresh();
          };
        }
        // Подписываемся и на derived строки
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

    /// <summary>Saves current column state to memory and refreshes status.</summary>
    [RelayCommand]
    private void SaveColumn()
    {
      SelectedItem?.Refresh();
    }

    public void ExecuteSplit(
    FormColumnVm original,
    IReadOnlyList<string> leftPositions,
    IReadOnlyList<string> rightPositions)
    {
      ExecuteCommand(new SplitColumnCommand(
          Columns, original, leftPositions, rightPositions));
      RebuildColumnItems();
    }

    partial void OnSelectedColumnChanged(FormColumnVm? value)
    {
      SplitCommand.NotifyCanExecuteChanged();
      MergeCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Splits one column into N columns (N >= 2).
    /// Each split is recorded as a separate undoable command.
    /// </summary>
    public void ExecuteMultiSplit(
        FormColumnVm original,
        IReadOnlyList<IReadOnlyList<string>> groups)
    {
      if (groups.Count < 2) return;

      ExecuteCommand(new MultiSplitColumnCommand(Columns, original, groups));
      RebuildColumnItems();

    }

    public event Action<FormColumnVm>? SplitRequested;

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task BuildTableAsync(CancellationToken ct)
    {
      Columns.Clear();
      Parameters.Clear();
      _undoStack.Clear();
      _redoStack.Clear();

      // Load form with parameters
      var form = await _db.Forms
          .Include(f => f.Parameters)
          .FirstOrDefaultAsync(f => f.Id == FormId, ct);

      if (form is null) return;

      FormNumber = form.Number;
      FormTitle = form.Title;

      var parameters = form.Parameters.OrderBy(p => p.RowNumber).ToList();

      // Find components that have this form as their own form
      var relevantComponents = _store.Components
          .Where(c => c.Entry.MatchResult.MatchedComponent?.OwnFormId == FormId
                   || c.Entry.MatchResult.MatchedComponent?.OwnForm?.Id == FormId)
          .ToList();

      // Build columns
      foreach (var component in relevantComponents)
      {
        var column = new FormColumnVm(component);

        // Load ComponentNtdValues for THIS form specifically
        var componentId = component.Entry.MatchResult.MatchedComponent?.Id;
        if (componentId is not null)
        {
          var componentNtdValues = await _db.ComponentNtdValues
              .Include(v => v.FormParameter)
              .Where(v => v.ComponentId == componentId
                       && v.FormParameter.FormId == FormId)
              .ToListAsync(ct);

          foreach (var ntd in componentNtdValues)
            column.NtdValues[ntd.FormParameterId] = ntd.Value;
        }

        Columns.Add(column);
      }

      // Build parameter rows
      foreach (var param in parameters)
        Parameters.Add(new FormParameterRowVm(param, Columns));

      // Build list items after Parameters are ready
      foreach (var col in Columns)
        ColumnItems.Add(new ColumnListItemVm(col, Parameters));
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
