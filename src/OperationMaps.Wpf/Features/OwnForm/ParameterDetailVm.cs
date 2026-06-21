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
    public string? Formula { get; init; }
    public bool IsRequired { get; init; }
    public bool IsDerived => Formula is not null;

    [ObservableProperty] private string _schemeValue = "";
    [ObservableProperty] private bool _isAddingNote;
    [ObservableProperty] private string _pendingNoteText = "";

    private readonly FormColumnVm _column;

    public ObservableCollection<OwnFormNoteVm> Notes
      => _column.GetNotes(FormParameterId);

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
      string optionalPinsValue = "")
    {
      _column = column;
      FormParameterId = parameterId;
      RowNumber = rowNumber;
      DisplayName = displayName;
      NtdValue = ntdValue;
      Formula = formula;
      _schemeValue = column.GetCellValue(parameterId);
      IsRequired = isRequired;

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
      _column.SetCellValue(FormParameterId, value);
    }

    //TODO: Парсер жёстко связан со строкой. Парсит только один тип "row1+row2+row3"
    public void Recalculate(IReadOnlyList<ParameterDetailVm> AllRows)
    {
      if (Formula is null) return;
      var rowNumbers = Formula
            .Split('+', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.StartsWith("row", StringComparison.OrdinalIgnoreCase))
            .Select(t => int.TryParse(t["row".Length..], out var n) ? n : -1)
            .Where(n => n > 0)
            .ToList();
      double sum = 0;
      bool hasAnyValue = false;
      foreach (var rowNum in rowNumbers)
      {
        var sourceRow = AllRows.FirstOrDefault(r => r.RowNumber == rowNum);
        if (sourceRow is null) continue;
        var raw = sourceRow.SchemeValue?.Trim() ?? "";
        if (string.IsNullOrEmpty(raw) || raw == "-") continue;
        raw = raw.Replace(',', '.');
        if (double.TryParse(raw,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var val))
        {
          sum += val;
          hasAnyValue = true;
        }
      }
      SchemeValue = hasAnyValue
            ? sum.ToString("G", System.Globalization.CultureInfo.CurrentCulture)
            : "";
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
      var note = new OwnFormNoteVm
      {
        FormParameterId = FormParameterId,
        NoteText = PendingNoteText,
        Order = 1,
      };
      note.DeleteRequested += OnNoteDeleteRequested;
      Notes.Add(note);
      _column.RecalculateNoteOrders();
      PendingNoteText = "";
      IsAddingNote = false;
    }

    private void OnNoteDeleteRequested(OwnFormNoteVm note)
    {
      Notes.Remove(note);
      _column.RecalculateNoteOrders();
    }
  }
}
