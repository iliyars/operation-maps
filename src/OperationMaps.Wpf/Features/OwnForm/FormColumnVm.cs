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

      // Copy cell values — TODO: consider clearing or prompting user
      foreach (var (key, value) in CellValues)
        cloned.CellValues[key] = value;

      foreach (var (key, value) in NtdValues)
        cloned.NtdValues[key] = value;

      return cloned;
    }
  }
}
