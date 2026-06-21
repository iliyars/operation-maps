using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Domain.Entities.Forms;

namespace OperationMaps.Wpf.Features.OwnForm
{
  public sealed partial class FormParameterRowVm : ObservableObject
  {
    public int FormParameterId { get; }
    public int RowNumber { get; }
    public string Name { get; }
    public string? Unit { get; }
    public string? Formula { get; }
    public bool IsRequired { get; }
    public string DisplayName => Unit is not null ? $"{Name}, {Unit}" : Name;
    public IReadOnlyList<FormColumnVm> Columns { get; }

    /// <summary>
    /// True for parameters that are themselves an optional second value
    /// of another parameter (e.g. Form 64's second supply voltage row).
    /// These rows are never rendered directly in the parameter list — the
    /// primary parameter (see <see cref="OptionalForRowNumber"/> being null
    /// but <see cref="CanHaveOptionalRow"/> true on IT) renders the
    /// "+ Добавить" affordance instead.
    /// </summary>
    public bool IsOptional { get; }

    /// <summary>RowNumber of the primary parameter this one extends, if <see cref="IsOptional"/>.</summary>
    public int? OptionalForRowNumber { get; }

    /// <summary>
    /// FormParameterId of this row's optional counterpart, if one exists
    /// in the form (e.g. the primary "напряжение питания" row's id pointing
    /// to the optional second-voltage parameter's id). Null when this
    /// parameter has no optional counterpart.
    /// </summary>
    public int? OptionalCounterpartId { get; set; }

    /// <summary>True when this parameter has an optional counterpart — i.e. <see cref="OptionalCounterpartId"/> is set.</summary>
    public bool CanHaveOptionalRow => OptionalCounterpartId.HasValue;

    public FormParameterRowVm(
      FormParameter parameter,
      IReadOnlyList<FormColumnVm> columns)
    {
      ArgumentNullException.ThrowIfNull(parameter);
      FormParameterId = parameter.Id;
      RowNumber = parameter.RowNumber;
      Name = parameter.Name;
      Unit = parameter.Unit;
      Columns = columns;
      Formula = parameter.Formula;
      IsRequired = parameter.IsRequired;
      IsOptional = parameter.IsOptional;
      OptionalForRowNumber = parameter.OptionalForRowNumber;
    }

    // ── Per-column note state ─────────────────────────────────────────────────
    // Tracks which column is currently in "adding note" mode
    private FormColumnVm? _addingNoteColumn;
    [ObservableProperty] private bool _isAddingNote;
    [ObservableProperty] private string _pendingNoteText = "";
    public FormColumnVm? AddingNoteColumn => _addingNoteColumn;

    public void StartAddNote(FormColumnVm column)
    {
      _addingNoteColumn = column;
      PendingNoteText = "";
      IsAddingNote = true;
    }

    [RelayCommand]
    private void CancelAddNote()
    {
      _addingNoteColumn = null;
      PendingNoteText = "";
      IsAddingNote = false;
    }

    [RelayCommand]
    private void ConfirmAddNote()
    {
      if (_addingNoteColumn is null) return;
      if (string.IsNullOrWhiteSpace(PendingNoteText)) return;
      var note = new OwnFormNoteVm
      {
        FormParameterId = FormParameterId,
        NoteText = PendingNoteText,
        Order = 1,
      };
      var notes = _addingNoteColumn.GetNotes(FormParameterId);
      note.DeleteRequested += n =>
      {
        notes.Remove(n);
        _addingNoteColumn.RecalculateNoteOrders();
      };
      notes.Add(note);
      _addingNoteColumn.RecalculateNoteOrders();
      PendingNoteText = "";
      IsAddingNote = false;
      _addingNoteColumn = null;
    }
  }
}
