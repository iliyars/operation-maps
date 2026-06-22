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

    /// <summary>True for parameters that represent an optional second row (e.g. Form 64's second voltage).</summary>
    public bool IsOptional { get; }

    /// <summary>RowNumber of the primary parameter this one extends, if <see cref="IsOptional"/>.</summary>
    public int? OptionalForRowNumber { get; }

    /// <summary>FormParameterId of this row's optional counterpart, if one exists in the form.</summary>
    public int? OptionalCounterpartId { get; set; }

    /// <summary>True when this parameter has an optional counterpart.</summary>
    public bool CanHaveOptionalRow => OptionalCounterpartId.HasValue;

    /// <summary>
    /// True for parameters eligible to be the "base" for load-factor
    /// calculation (e.g. row 13 "Постоянное напряжение"). A form may have
    /// several such candidates.
    /// </summary>
    public bool CanBeLoadFactorBase { get; }

    /// <summary>
    /// True for the ONE parameter that displays the calculated load
    /// factor result — OwnFormViewModel writes into this row
    /// automatically and the UI marks it read-only.
    /// </summary>
    public bool IsLoadFactorResult { get; }

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
      CanBeLoadFactorBase = parameter.CanBeLoadFactorBase;
      IsLoadFactorResult = parameter.IsLoadFactorResult;
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
