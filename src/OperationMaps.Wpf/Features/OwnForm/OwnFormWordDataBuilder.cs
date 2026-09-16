using OperationMaps.Application.Word;

namespace OperationMaps.Wpf.Features.OwnForm
{
  /// <summary>
  /// Builds a <see cref="WordFormData"/> snapshot from an OwnForm's live
  /// table state (<see cref="FormColumnVm"/>/<see cref="FormParameterRowVm"/>).
  /// Extracted from <see cref="OwnFormViewModel"/> so this data-shaping logic
  /// can be tested without a DbContext, IWordService or WordFormMapLoader.
  /// </summary>
  public static class OwnFormWordDataBuilder
  {
    public static WordFormData Build(
        string formNumber,
        string documentDesignation,
        IReadOnlyList<FormParameterRowVm> parameters,
        IReadOnlyList<FormColumnVm> columns)
    {
      var paramIdToRow = parameters
          .Where(p => !p.IsOptional)
          .ToDictionary(p => p.FormParameterId, p => p.RowNumber);

      // Optional parameters map their own FormParameterId → the PRIMARY
      // parameter's RowNumber (e.g. RowNumber=1 for "напряжение питания"),
      // because that's what map.json's "optionalRows" top-level keys are —
      // NOT the optional parameter's own RowNumber. FormParameter.OptionalForRowNumber
      // already stores exactly this primary RowNumber.
      var optionalParamIdToOptionalRowNumber = parameters
          .Where(p => p.IsOptional && p.OptionalForRowNumber.HasValue)
          .ToDictionary(p => p.FormParameterId, p => p.OptionalForRowNumber!.Value);

      var components = columns
          .Select(col => BuildComponentData(col, paramIdToRow, optionalParamIdToOptionalRowNumber))
          .ToList();

      return new WordFormData
      {
        FormNumber = formNumber,
        DocumentDesignation = documentDesignation,
        Components = components,
        HeaderFields = new Dictionary<string, string>
        {
          ["sheetNumber"] = "1",
        },
      };
    }

    private static WordComponentData BuildComponentData(
        FormColumnVm column,
        Dictionary<int, int> paramIdToRow,
        Dictionary<int, int> optionalParamIdToOptionalRowNumber)
    {
      var schemeValues = column.CellValues
          .Where(kv => paramIdToRow.ContainsKey(kv.Key) && !string.IsNullOrEmpty(kv.Value))
          .ToDictionary(kv => paramIdToRow[kv.Key], kv => kv.Value);

      var ntdValues = column.NtdValues
          .Where(kv => paramIdToRow.ContainsKey(kv.Key) && !string.IsNullOrEmpty(kv.Value))
          .ToDictionary(kv => paramIdToRow[kv.Key], kv => kv.Value);

      var pinValues = column.PinValues
          .Where(kv => paramIdToRow.ContainsKey(kv.Key) && !string.IsNullOrEmpty(kv.Value))
          .ToDictionary(kv => paramIdToRow[kv.Key], kv => kv.Value);

      // Optional rows (e.g. a second supply voltage) only exist for a
      // component when the catalog has an NTD value for that optional
      // parameter — OptionalNtdValues is the source of truth for "does
      // this row apply at all". Scheme value comes from user input
      // (OptionalCellValues); pins come from the same PinValues dict,
      // just keyed by the OPTIONAL parameter's id.
      var optionalRowValues = column.OptionalNtdValues
          .Where(kv => optionalParamIdToOptionalRowNumber.ContainsKey(kv.Key))
          .ToDictionary(
              kv => optionalParamIdToOptionalRowNumber[kv.Key],
              kv => new OptionalRowValues
              {
                NtdValue = kv.Value,
                SchemeValue = column.GetOptionalCellValue(kv.Key),
                PinsValue = column.GetPinValue(kv.Key),
              });

      var noteLines = column.Notes.Values
          .SelectMany(notes => notes)
          .OrderBy(n => n.Order)
          .Select(n => $"{n.Marker} {n.NoteText.Trim()}")
          .ToList();

      return new WordComponentData
      {
        Name = column.Name,
        Positions = PositionRangeFormatter.Format(column.Component.Entry.Imported.Positions),
        Quantity = column.Component.Entry.Imported.Positions.Count.ToString(),
        SchemeValues = schemeValues,
        NtdValues = ntdValues,
        PinValues = pinValues,
        OptionalRowValuesByParameter = optionalRowValues,
        Note = string.Join("\n", noteLines),
      };
    }
  }
}
