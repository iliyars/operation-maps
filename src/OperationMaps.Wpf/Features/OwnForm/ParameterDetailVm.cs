using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OperationMaps.Wpf.Features.OwnForm
{
  public sealed partial class ParameterDetailVm : ObservableObject
  {
    public int FormParameterId { get; init; }
    public int RowNumber { get; init; }
    public string DisplayName { get; init; } = "";
    public string NtdValue { get; init; } = "—";
    public string? Formula { get; init; }

    public bool IsDerived => Formula is not null;

    [ObservableProperty] private string _schemeValue = "";

    /// <summary>Parent column — used to persist value back when changed.</summary>
    private readonly FormColumnVm _column;

    public ParameterDetailVm(
      FormColumnVm column,
      int parameterId,
      int rowNumber,
      string displayName,
      string ntdValue,
      string formula = null)
    {
      _column = column;
      FormParameterId = parameterId;
      RowNumber = rowNumber;
      DisplayName = displayName;
      NtdValue = ntdValue;
      Formula = formula;
      _schemeValue = column.GetCellValue(parameterId);
    }

    partial void OnSchemeValueChanged(string value)
        => _column.SetCellValue(FormParameterId, value);
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
        if (double.TryParse(raw, System.Globalization.NumberStyles.Any,
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
  }
}
