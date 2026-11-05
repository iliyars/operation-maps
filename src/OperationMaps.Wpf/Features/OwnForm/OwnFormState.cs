using System.Collections.ObjectModel;
using System.Collections.Specialized;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Infrastructure.Commands;

namespace OperationMaps.Wpf.Features.OwnForm
{
  /// <summary>
  /// Project-lifetime state of one own-form table: columns with their
  /// user-entered values/notes/splits/merges, parameter rows and the
  /// per-form undo history.
  /// <para>
  /// Lives in <see cref="Stores.ProjectStore"/> rather than in
  /// <see cref="OwnFormViewModel"/> — the ViewModel is transient and is
  /// re-created on every navigation, so anything kept only there was lost
  /// as soon as the user switched to another form. The ViewModel now just
  /// presents this object; report export reads it directly too.
  /// </para>
  /// </summary>
  public sealed class OwnFormState
  {
    public int FormId { get; }
    public string FormNumber { get; }
    public string FormTitle { get; }

    /// <summary>True when the form has a "номера выводов" column (Form 64).</summary>
    public bool ShowsPins => FormNumber == "64";

    public IReadOnlyList<FormParameterRowVm> Parameters { get; private set; } = [];
    public ObservableCollection<FormColumnVm> Columns { get; } = [];
    public IReadOnlyList<LoadFactorOptionVm> LoadFactorOptions { get; }

    /// <summary>
    /// Per-form history: undoing in Form 67 must not revert an edit made in
    /// Form 68. Survives navigation together with the columns it operates on.
    /// </summary>
    public UndoRedoStack History { get; } = new();

    /// <summary>FormParameterId of the row that displays the calculated load factor, if any.</summary>
    public int? LoadFactorResultParameterId { get; }

    /// <summary>Form.DefaultLoadFactorParameterId (or the single candidate), used when a Component has no explicit override.</summary>
    public int? DefaultLoadFactorParameterId { get; private set; }

    /// <summary>
    /// The admin changed the form's default base parameter: columns that
    /// have no base chosen yet pick it up; explicit choices stay as they are.
    /// </summary>
    public void SetDefaultLoadFactorParameter(int? parameterId)
    {
      DefaultLoadFactorParameterId = parameterId;
      foreach (var column in Columns.Where(c => c.LoadFactorBaseParameterId is null))
        column.LoadFactorBaseParameterId = parameterId;
    }

    /// <summary>
    /// Raised after a column's formulas and load factor were recomputed, so the
    /// currently shown ViewModel (if any) can refresh what's on screen.
    /// </summary>
    public event Action<FormColumnVm>? CalculatedValuesChanged;

    /// <summary>
    /// Raised on any user edit of this form: a column's values/notes, or the
    /// column set itself (split/merge/undo). Drives "unsaved changes".
    /// </summary>
    public event Action? Edited;

    public OwnFormState(
        int formId,
        string formNumber,
        string formTitle,
        IReadOnlyList<LoadFactorOptionVm> loadFactorOptions,
        int? loadFactorResultParameterId,
        int? defaultLoadFactorParameterId)
    {
      FormId = formId;
      FormNumber = formNumber;
      FormTitle = formTitle;
      LoadFactorOptions = loadFactorOptions;
      LoadFactorResultParameterId = loadFactorResultParameterId;
      DefaultLoadFactorParameterId = defaultLoadFactorParameterId;

      // Columns are replaced (not mutated) by split/merge and their undo —
      // track additions/removals so clones also get load-factor recalculation.
      Columns.CollectionChanged += OnColumnsChanged;
    }

    public void SetParameters(IReadOnlyList<FormParameterRowVm> parameters)
    {
      Parameters = parameters;
      foreach (var column in Columns) RecalculateCalculatedValues(column);
    }

    /// <summary>
    /// Swaps the whole column set (used when restoring a saved project).
    /// Done item by item rather than Clear() — a Reset notification carries
    /// no OldItems, so the removed columns' handlers couldn't be detached.
    /// </summary>
    public void ReplaceColumns(IEnumerable<FormColumnVm> columns)
    {
      foreach (var col in Columns.ToList())
        Columns.Remove(col);
      foreach (var col in columns)
        Columns.Add(col);
      foreach (var col in Columns)
        RecalculateCalculatedValues(col);
      History.Clear();
    }

    /// <summary>
    /// Recomputes formula rows before the load factor, independently of screens.
    /// Writes derived values directly so calculation does not trigger user edits.
    /// </summary>
    public void RecalculateCalculatedValues(FormColumnVm column)
    {
      var rows = Parameters.Where(p => !p.IsOptional).ToDictionary(p => p.RowNumber);
      var results = new Dictionary<int, string?>();
      var visiting = new HashSet<int>();
      var derived = rows.Values.Where(p => p.Formula is not null).ToArray();
      foreach (var parameter in derived) Evaluate(parameter.RowNumber);
      foreach (var parameter in derived)
        column.CellValues[parameter.FormParameterId] = results[parameter.RowNumber] ?? "";

      string? Evaluate(int rowNumber)
      {
        if (!rows.TryGetValue(rowNumber, out var row)) return null;
        if (row.Formula is null) return column.GetCellValue(row.FormParameterId);
        if (results.TryGetValue(rowNumber, out var cached)) return cached;
        if (!visiting.Add(rowNumber))
          throw new InvalidOperationException($"Циклическая формула в форме {FormNumber}, строка {rowNumber}.");
        var value = ParameterFormulaCalculator.Calculate(row.Formula, Evaluate);
        visiting.Remove(rowNumber);
        results[rowNumber] = value;
        return value;
      }

      if (LoadFactorResultParameterId is { } resultParamId)
      {
        var baseParam = Parameters.FirstOrDefault(p => p.FormParameterId == column.LoadFactorBaseParameterId);
        var result = baseParam is null ? null : LoadFactorCalculator.Calculate(
            column.GetCellValue(baseParam.FormParameterId),
            column.GetNtdValue(baseParam.FormParameterId), baseParam.RowNumber);
        column.LoadFactorValue = result ?? "";
        column.CellValues[resultParamId] = result ?? "";
      }
      column.NotifyCellValuesChanged();

      CalculatedValuesChanged?.Invoke(column);
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
      if (e.OldItems is not null)
        foreach (FormColumnVm col in e.OldItems)
        {
          col.CalculationInputsChanged -= RecalculateCalculatedValues;
          col.Edited -= OnColumnEdited;
        }

      if (e.NewItems is not null)
        foreach (FormColumnVm col in e.NewItems)
        {
          // -= first: undo re-inserts the very same instance that was removed
          col.CalculationInputsChanged -= RecalculateCalculatedValues;
          col.CalculationInputsChanged += RecalculateCalculatedValues;
          col.Edited -= OnColumnEdited;
          col.Edited += OnColumnEdited;
          RecalculateCalculatedValues(col);
        }

      Edited?.Invoke();
    }

    private void OnColumnEdited() => Edited?.Invoke();
  }
}
