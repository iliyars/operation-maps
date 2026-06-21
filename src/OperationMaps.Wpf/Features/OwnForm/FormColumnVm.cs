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

    [ObservableProperty] private bool _isSelected;

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
    }

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

    public ObservableCollection<OwnFormNoteVm> GetNotes(int formParameterId)
    {
      if (!Notes.TryGetValue(formParameterId, out var notes))
      {
        notes = [];
        notes.CollectionChanged += (_, _) => RecalculateNoteOrders();
        Notes[formParameterId] = notes;
      }
      return notes;
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


    /// <summary>
    /// Calculates fill status based on required parameters.
    /// If no required params — uses all params with NTD ≠ "—".
    /// </summary>
    public FillStatus GetFillStatus(IReadOnlyList<FormParameterRowVm> parameters)
    {
      var relevant = parameters
          .Where(p => p.IsRequired || (!p.IsRequired && GetNtdValue(p.FormParameterId) != "—"))
          .ToList();

      if (relevant.Count == 0) return FillStatus.Empty;

      var filled = relevant
          .Count(p => !string.IsNullOrWhiteSpace(GetCellValue(p.FormParameterId)));

      if (filled == 0) return FillStatus.Empty;
      if (filled == relevant.Count) return FillStatus.Complete;
      return FillStatus.Partial;
    }

    /// <summary>Count of filled relevant parameters.</summary>
    public int GetFilledCount(IReadOnlyList<FormParameterRowVm> parameters)
        => parameters
            .Where(p => p.IsRequired || GetNtdValue(p.FormParameterId) != "—")
            .Count(p => !string.IsNullOrWhiteSpace(GetCellValue(p.FormParameterId)));

    /// <summary>Total count of relevant parameters.</summary>
    public int GetTotalCount(IReadOnlyList<FormParameterRowVm> parameters)
        => parameters
            .Count(p => p.IsRequired || GetNtdValue(p.FormParameterId) != "—");

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

      return cloned;
    }
  }

  public enum FillStatus { Empty, Partial, Complete }
}
