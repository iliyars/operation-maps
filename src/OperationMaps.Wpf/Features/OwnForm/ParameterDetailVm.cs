using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OperationMaps.Wpf.Features.OwnForm
{
  public sealed partial class ParameterDetailVm : ObservableObject
  {
    public int FormParameterId { get; init; }
    public int RowNumber { get; init; }
    public string DisplayName { get; init; } = "";
    public string NtdValue { get; init; } = "—";
    public string RegimeCondition { get; init; } = "";
    public string? Formula { get; init; }
    public bool IsRequired { get; init; }
    public bool IsDerived => Formula is not null;

    /// <summary>
    /// True for any row whose SchemeValue is computed by the app rather
    /// than typed by the user — covers both Formula-derived rows (e.g.
    /// "Суммарное напряжение") and the load-factor result row. The UI
    /// uses this single flag to decide TextBox vs. read-only display,
    /// so a new kind of computed row only needs to extend this property
    /// rather than touch the XAML again.
    /// </summary>
    public bool IsReadOnlyValue => IsDerived || IsLoadFactorResult;

    /// <summary>
    /// True for the load-factor result row (коэффициент нагрузки) —
    /// its SchemeValue is written automatically by OwnFormState
    /// whenever the chosen base parameter changes, so the UI shows it
    /// read-only instead of an editable TextBox, same treatment as
    /// IsDerived rows.
    /// </summary>
    public bool IsLoadFactorResult { get; init; }

    [ObservableProperty] private string _schemeValue = "";
    [ObservableProperty] private bool _isAddingNote;
    [ObservableProperty] private string _pendingNoteText = "";

    private readonly FormColumnVm _column;

    public ObservableCollection<OwnFormNoteVm> Notes
      => _column.GetNotes(FormParameterId);
    public ObservableCollection<OperationMaps.Wpf.Features.Components.CatalogNoteChoice> AvailableNotes
      => _column.GetAvailableNotes(FormParameterId);

    // ── Pins (Form 64 only) ───────────────────────────────────────────────────

    /// <summary>
    /// Whether this form shows the "номера выводов" column at all.
    /// Set by the caller when building rows for a form that has pin
    /// numbers — currently only Form 64.
    /// </summary>
    public bool ShowPins { get; init; }

    /// <summary>
    /// Read-only pin numbers from the catalog (ComponentPinValue), entered
    /// once via the "add component" wizard. Never edited from this screen.
    /// </summary>
    public string PinsValue { get; init; } = "";

    // ── Optional second-value row (e.g. Form 64's second supply voltage) ─────

    /// <summary>
    /// True when the catalog has a value for this row's optional
    /// counterpart (e.g. a second supply voltage) — i.e.
    /// FormColumnVm.HasOptionalRow(OptionalFormParameterId) was true when
    /// this row was built. There's no user-facing "add" affordance:
    /// the catalog data alone decides whether this section renders.
    /// </summary>
    public bool HasOptionalRow { get; init; }

    /// <summary>FormParameterId of the OPTIONAL parameter, when <see cref="HasOptionalRow"/> is true.</summary>
    public int OptionalFormParameterId { get; init; }

    /// <summary>Catalog "по НТД" value for the optional row.</summary>
    public string OptionalNtdValue { get; init; } = "—";

    /// <summary>Catalog pin numbers for the optional row (read-only, same source as <see cref="PinsValue"/>).</summary>
    public string OptionalPinsValue { get; init; } = "";

    [ObservableProperty] private string _optionalSchemeValue = "";

    partial void OnOptionalSchemeValueChanged(string value)
    {
      if (!HasOptionalRow) return;
      _column.SetOptionalCellValue(OptionalFormParameterId, value);
    }

    public ParameterDetailVm(
      FormColumnVm column,
      int parameterId,
      int rowNumber,
      string displayName,
      string ntdValue,
      string? formula = null,
      bool isRequired = false,
      bool showPins = false,
      string pinsValue = "",
      bool hasOptionalRow = false,
      int optionalFormParameterId = 0,
      string optionalNtdValue = "—",
      string optionalPinsValue = "",
      bool isLoadFactorResult = false)
    {
      _column = column;
      FormParameterId = parameterId;
      RowNumber = rowNumber;
      DisplayName = displayName;
      NtdValue = ntdValue;
      Formula = formula;
      _schemeValue = column.GetCellValue(parameterId);
      IsRequired = isRequired;
      IsLoadFactorResult = isLoadFactorResult;

      ShowPins = showPins;
      PinsValue = pinsValue;

      HasOptionalRow = hasOptionalRow;
      OptionalFormParameterId = optionalFormParameterId;
      OptionalNtdValue = optionalNtdValue;
      OptionalPinsValue = optionalPinsValue;

      if (hasOptionalRow)
        _optionalSchemeValue = column.GetOptionalCellValue(optionalFormParameterId);
    }

    partial void OnSchemeValueChanged(string value)
    {
      if (!IsReadOnlyValue) _column.SetCellValue(FormParameterId, value);
    }

    internal void RefreshCalculatedValue()
    {
      if (!IsReadOnlyValue) return;
      SchemeValue = _column.GetCellValue(FormParameterId);
    }
    [RelayCommand]
    private void StartAddNote()
    {
      PendingNoteText = "";
      IsAddingNote = true;
    }

    [RelayCommand]
    private void CancelAddNote()
    {
      PendingNoteText = "";
      IsAddingNote = false;
    }

    [RelayCommand]
    private void ConfirmAddNote()
    {
      if (string.IsNullOrWhiteSpace(PendingNoteText)) return;
      _column.AddNote(FormParameterId, PendingNoteText);
      PendingNoteText = "";
      IsAddingNote = false;
    }
  }
}
