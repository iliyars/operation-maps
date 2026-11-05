using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Wpf.Features.Components;

namespace OperationMaps.Wpf.Features.OwnForm
{
  /// <summary>
  /// Represents one column in an own-form table.
  /// Corresponds to one ProjectComponentVm (a group of position designators).
  /// </summary>
  public partial class FormColumnVm : ObservableObject
  {
    public Guid Id { get; } = Guid.NewGuid();
    public ProjectComponentVm Component { get; }

    public string Name => Component.Name;
    public string Positions => Component.Positions;
    public Dictionary<int, string> RegimeConditions { get; } = [];
    private Dictionary<int, string>? _defaultNtd;
    private Dictionary<int, string>? _defaultOptionalNtd;
    private OperationMaps.Application.Models.ComponentRegimeSelection? _appliedRegime;
    public bool NeedsRegimeRefresh => !Equals(_appliedRegime, Component.ActiveRegime);
    public bool HasRegimeOverride { get; private set; }

    public void SelectRegimeOverride(OperationMaps.Application.Models.ComponentRegimeSelection? selection)
    {
      HasRegimeOverride = true;
      Component.SelectRegime(selection);
      Edited?.Invoke();
    }

    public void RefreshRegime(string formNumber, IEnumerable<OperationMaps.Domain.Entities.Forms.FormParameter> parameters)
    {
      _defaultNtd ??= new(NtdValues);
      _appliedRegime = Component.ActiveRegime;
      _defaultOptionalNtd ??= new(OptionalNtdValues);
      NtdValues.Clear(); OptionalNtdValues.Clear(); RegimeConditions.Clear();
      foreach (var pair in _defaultNtd) NtdValues[pair.Key] = pair.Value;
      foreach (var pair in _defaultOptionalNtd) OptionalNtdValues[pair.Key] = pair.Value;
      if (Component.ActiveRegime is not { } regime) return;
      foreach (var value in regime.Values.Where(v => v.FormNumber == formNumber))
      {
        var parameter = parameters.FirstOrDefault(p => p.RowNumber == value.RowNumber);
        if (parameter is null) continue;
        if (parameter.IsOptional) OptionalNtdValues[parameter.Id] = value.Value;
        else NtdValues[parameter.Id] = value.Value;
        RegimeConditions[parameter.Id] = value.Condition ?? regime.Condition;
      }
    }

    public string RegimeMarker(string condition) => new('*', Notes.Values.Sum(n => n.Count)
      + RegimeConditions.Values.Distinct().ToList().IndexOf(condition) + 1);

    public string ValueWithNotes(int parameterId, string value)
    {
      var markers = Notes.TryGetValue(parameterId, out var notes) ? notes.OrderBy(n => n.Order).Select(n => n.Marker).ToList() : [];
      if (RegimeConditions.TryGetValue(parameterId, out var condition)) markers.Add(RegimeMarker(condition));
      return markers.Count == 0 ? value : $"{value} {string.Join(", ", markers)}";
    }

    [ObservableProperty] private bool _isSelected;

    // ── Load factor (коэффициент нагрузки) ────────────────────────────────────

    /// <summary>
    /// FormParameterId of the base parameter currently selected for this
    /// component's load-factor calculation. Mirrors
    /// Component.LoadFactorParameterId — null means "use the form's
    /// default". Settable by the user via a picker when multiple
    /// candidate parameters exist; persisted back to the catalog via
    /// IComponentEntryService-style update when the column is saved.
    /// </summary>
    [ObservableProperty] private int? _loadFactorBaseParameterId;

    /// <summary>
    /// The calculated "{ratio} ({rowNumber})" string for the load-factor
    /// result row, recomputed whenever the base parameter's SchemeValue
    /// changes. Empty until a base parameter is selected and has both a
    /// scheme and NTD value.
    /// </summary>
    [ObservableProperty] private string _loadFactorValue = "";

    /// <summary>
    /// Raised whenever this column's SchemeValue or
    /// LoadFactorBaseParameterId changes — OwnFormState subscribes
    /// to recompute formula rows and the load factor.
    /// </summary>
    public event Action<FormColumnVm>? CalculationInputsChanged;

    partial void OnLoadFactorBaseParameterIdChanged(int? value)
    {
      CalculationInputsChanged?.Invoke(this);
      Edited?.Invoke();
    }

    /// <summary>
    /// Raised on every user edit of this column (values, notes, load-factor
    /// base) — drives the project's "unsaved changes" flag. Not raised for
    /// the calculated load-factor result, which isn't user input.
    /// </summary>
    public event Action? Edited;

    // ── Cell values "в схеме" ─────────────────────────────────────────────────

    /// <summary>
    /// User-entered "in-circuit" values keyed by FormParameterId.
    /// </summary>
    public Dictionary<int, string> CellValues { get; } = new();

    public string GetCellValue(int formParameterId) => CellValues.TryGetValue(formParameterId, out var v) ? v : "";

    public void SetCellValue(int formParameterId, string value)
    {
      CellValues[formParameterId] = value;
      OnPropertyChanged(nameof(CellValues));

      CalculationInputsChanged?.Invoke(this);

      Edited?.Invoke();
    }

    /// <summary>
    /// Notifies that CellValues changed without going through
    /// <see cref="SetCellValue"/> — used by OwnFormState when it
    /// writes calculated results directly into the
    /// dictionary (writing through SetCellValue there would re-trigger
    /// CalculationInputsChanged and recurse).
    /// </summary>
    public void NotifyCellValuesChanged() => OnPropertyChanged(nameof(CellValues));

    // ── Pin values "номера выводов" (Form 64 only) ────────────────────────────

    /// <summary>
    /// Pin numbers loaded from ComponentPinValue (catalog data, set once via
    /// the "add component" wizard for this specific Component). Read-only
    /// from the OwnForm screen's point of view — the user never types these
    /// in here, they're just displayed alongside "по НТД". Keyed by
    /// FormParameterId. Empty for forms without a pins column or for
    /// components that don't have catalog pin data.
    /// </summary>
    public Dictionary<int, string> PinValues { get; } = new();

    public string GetPinValue(int formParameterId)
        => PinValues.TryGetValue(formParameterId, out var v) ? v : "";

    // ── Optional second-value row (e.g. Form 64's second supply voltage) ─────

    /// <summary>
    /// Catalog NTD value for an OPTIONAL parameter row (e.g. a component's
    /// second supply voltage), keyed by the OPTIONAL FormParameter's id.
    /// Presence of a key here — set once via the "add component" wizard and
    /// loaded alongside NtdValues — is what determines whether OwnFormView
    /// shows this row at all; there's no user-facing "add" affordance here,
    /// the catalog data decides.
    /// </summary>
    public Dictionary<int, string> OptionalNtdValues { get; } = new();

    public bool HasOptionalRow(int optionalFormParameterId)
        => OptionalNtdValues.ContainsKey(optionalFormParameterId);

    /// <summary>User-entered "в схеме" value for the optional row, keyed by the OPTIONAL FormParameterId.</summary>
    public Dictionary<int, string> OptionalCellValues { get; } = new();

    public string GetOptionalCellValue(int optionalFormParameterId)
        => OptionalCellValues.TryGetValue(optionalFormParameterId, out var v) ? v : "";

    public void SetOptionalCellValue(int optionalFormParameterId, string value)
    {
      OptionalCellValues[optionalFormParameterId] = value;
      OnPropertyChanged(nameof(OptionalCellValues));
      Edited?.Invoke();
    }

    // ── NTD values "по НТД" ───────────────────────────────────────────────────

    /// <summary>
    /// NTD values from catalog keyed by FormParameterId.
    /// Populated when the column is created.
    /// </summary>
    public Dictionary<int, string> NtdValues { get; } = new();

    public string GetNtdValue(int formParameterId)
      => NtdValues.TryGetValue(formParameterId, out var v) ? v : "-";

    // ── Notes (per parameter, cross-parameter sequential) ─────────────────────

    /// <summary>Notes keyed by FormParameterId.</summary>
    public Dictionary<int, ObservableCollection<OwnFormNoteVm>> Notes { get; } = new();
    private readonly Dictionary<int, ObservableCollection<OperationMaps.Wpf.Features.Components.CatalogNoteChoice>> _availableNotes = new();
    public ObservableCollection<OperationMaps.Wpf.Features.Components.CatalogNoteChoice> GetAvailableNotes(int parameterId)
    {
      if (!_availableNotes.TryGetValue(parameterId, out var choices)) _availableNotes[parameterId] = choices = [];
      return choices;
    }

    public void AddCatalogNote(int parameterId, string text)
    {
      var choices = GetAvailableNotes(parameterId);
      if (choices.Any(c => c.Text == text)) return;
      var choice = new OperationMaps.Wpf.Features.Components.CatalogNoteChoice(text,
        () => GetNotes(parameterId).Any(n => n.NoteText == text), selected =>
        {
          if (selected) AddNote(parameterId, text);
          else foreach (var note in GetNotes(parameterId).Where(n => n.NoteText == text).ToArray()) GetNotes(parameterId).Remove(note);
        });
      GetNotes(parameterId).CollectionChanged += (_, _) => choice.Refresh();
      choices.Add(choice);
      AddNote(parameterId, text);
    }

    public ObservableCollection<OwnFormNoteVm> GetNotes(int formParameterId)
    {
      if (!Notes.TryGetValue(formParameterId, out var notes))
      {
        notes = [];
        notes.CollectionChanged += (_, _) =>
        {
          RecalculateNoteOrders();
          Edited?.Invoke();
        };
        Notes[formParameterId] = notes;
      }
      return notes;
    }

    /// <summary>
    /// Appends a note to a parameter of this column; the note removes itself
    /// from the column when its Delete command is invoked.
    /// </summary>
    public OwnFormNoteVm AddNote(int formParameterId, string text)
    {
      var notes = GetNotes(formParameterId);
      var note = new OwnFormNoteVm
      {
        FormParameterId = formParameterId,
        NoteText = text,
        Order = 1,
      };
      note.DeleteRequested += n => notes.Remove(n);
      notes.Add(note);
      return note;
    }

    /// <summary>
    /// Recalculates note Order across all parameters in this column sequentially.
    /// First note = *, second = **, etc.
    /// </summary>
    public void RecalculateNoteOrders()
    {
      int order = 1;
      foreach (var noteList in Notes.Values)
        foreach (var note in noteList)
          note.Order = order++;
    }

    public void OrderNoteParameters(IEnumerable<int> parameterIds)
    {
      var ordered = parameterIds.Concat(Notes.Keys).Distinct()
          .Where(Notes.ContainsKey).Select(id => (Id: id, Items: Notes[id])).ToArray();
      Notes.Clear();
      foreach (var entry in ordered) Notes.Add(entry.Id, entry.Items);
      RecalculateNoteOrders();
    }

    public void MergeNotesFrom(FormColumnVm first, FormColumnVm second)
    {
      var selected = first.Notes.Concat(second.Notes).SelectMany(pair => pair.Value.Select(n => (Id: pair.Key, Text: n.NoteText.Trim())))
        .Where(n => n.Text.Length > 0).Distinct().ToArray();
      foreach (var source in new[] { first, second })
        foreach (var (id, choices) in source._availableNotes)
          foreach (var choice in choices.Where(c => !string.IsNullOrWhiteSpace(c.Text))) AddCatalogNote(id, choice.Text.Trim());
      foreach (var notes in Notes.Values) notes.Clear();
      foreach (var note in selected) AddNote(note.Id, note.Text);
      OrderNoteParameters(selected.Select(n => n.Id));
    }

    public void FillMissingCatalogValuesFrom(FormColumnVm other)
    {
      Fill(NtdValues, other.NtdValues);
      Fill(OptionalNtdValues, other.OptionalNtdValues);
      Fill(PinValues, other.PinValues);
      if (_defaultNtd is not null) Fill(_defaultNtd, other._defaultNtd ?? other.NtdValues);
      if (_defaultOptionalNtd is not null) Fill(_defaultOptionalNtd, other._defaultOptionalNtd ?? other.OptionalNtdValues);
      static void Fill(Dictionary<int, string> target, Dictionary<int, string> source)
      {
        foreach (var (id, value) in source)
          if (!target.TryGetValue(id, out var existing) || string.IsNullOrWhiteSpace(existing) || existing.Trim() is "-" or "—") target[id] = value;
      }
    }


    /// <summary>
    /// Counts applicable input and calculated rows; hidden optional rows are excluded.
    /// </summary>
    public FillStatus GetFillStatus(IReadOnlyList<FormParameterRowVm> parameters)
    {
      var relevant = FormCompletion.Relevant(this, parameters).ToList();

      if (relevant.Count == 0) return FillStatus.Complete;

      var filled = relevant
          .Count(p => FormCompletion.IsFilled(this, p));

      if (filled == 0) return FillStatus.Empty;
      if (filled == relevant.Count) return FillStatus.Complete;
      return FillStatus.Partial;
    }

    /// <summary>Count of filled relevant parameters.</summary>
    public int GetFilledCount(IReadOnlyList<FormParameterRowVm> parameters)
        => FormCompletion.Relevant(this, parameters).Count(p => FormCompletion.IsFilled(this, p));

    /// <summary>Total count of relevant parameters.</summary>
    public int GetTotalCount(IReadOnlyList<FormParameterRowVm> parameters)
        => FormCompletion.Relevant(this, parameters).Count();

    // ── Constructor ───────────────────────────────────────────────────────────

    public FormColumnVm(ProjectComponentVm component)
    {
      Component = component ?? throw new ArgumentNullException(nameof(component));
    }

    /// <summary>
    /// Creates a copy of this column with different positions.
    /// Used by SplitColumnCommand — copies cell values to both halves.
    /// </summary>
    public FormColumnVm CloneWithPositions(IReadOnlyList<string> positions)
    {
      var cloned = new FormColumnVm(Component.CloneWithPositions(positions));

      foreach (var (key, value) in CellValues)
        cloned.CellValues[key] = value;

      foreach (var (key, value) in NtdValues)
        cloned.NtdValues[key] = value;

      foreach (var (key, value) in PinValues)
        cloned.PinValues[key] = value;

      foreach (var (key, value) in OptionalNtdValues)
        cloned.OptionalNtdValues[key] = value;

      foreach (var (key, value) in OptionalCellValues)
        cloned.OptionalCellValues[key] = value;

      cloned.LoadFactorBaseParameterId = LoadFactorBaseParameterId;
      cloned.HasRegimeOverride = HasRegimeOverride;
      cloned._defaultNtd = _defaultNtd is null ? null : new(_defaultNtd);
      cloned._defaultOptionalNtd = _defaultOptionalNtd is null ? null : new(_defaultOptionalNtd);
      cloned._appliedRegime = _appliedRegime;
      foreach (var pair in RegimeConditions) cloned.RegimeConditions[pair.Key] = pair.Value;
      foreach (var (id, choices) in _availableNotes)
        foreach (var choice in choices) cloned.AddCatalogNote(id, choice.Text);
      foreach (var notes in cloned.Notes.Values) notes.Clear();
      foreach (var (id, notes) in Notes)
        foreach (var note in notes) cloned.AddNote(id, note.NoteText);
      cloned.OrderNoteParameters(Notes.Keys);

      return cloned;
    }
  }

  public enum FillStatus { Empty, Partial, Complete }
}
