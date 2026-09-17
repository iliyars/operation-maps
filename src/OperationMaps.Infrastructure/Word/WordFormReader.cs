using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Word;

namespace OperationMaps.Infrastructure.Word
{
  /// <summary>
  /// Reads an existing filled Word document back into <see cref="WordFormData"/>
  /// using <c>map.json</c> coordinates. The read half of what used to be
  /// <c>WordService</c> — split out so it can be reasoned about (and tested)
  /// independently of the write half (<see cref="WordFormWriter"/>).
  /// </summary>
  public static partial class WordFormReader
  {
    // Matches the "ФОРМА <number>" header every template's first cell starts
    // with (e.g. "ФОРМА 67 ", "ФОРМА 65А", "ФОРМА 64" with no trailing space).
    [GeneratedRegex(@"ФОРМА\s+([0-9]+[^\s|]*)", RegexOptions.IgnoreCase)]
    private static partial Regex HeaderFormNumberRegex();

    /// <summary>
    /// Scans every table in the document and returns the distinct form
    /// numbers found in their headers, in first-appearance order. Used to
    /// discover what a combined work-order document (Form4 + Form67 +
    /// Form68 + ... all in one file) actually contains before deciding
    /// which per-form map.json files to read it with.
    /// </summary>
    public static IReadOnlyList<string> DetectFormNumbers(string documentPath)
    {
      using var doc = WordprocessingDocument.Open(documentPath, isEditable: false);

      var result = new List<string>();
      var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach (var table in doc.MainDocumentPart!.Document.Body!.Elements<Table>())
      {
        var headerCell = WordTableHelper.TryGetCell(table, 0, 0);
        if (headerCell is null) continue;

        var firstLine = WordTableHelper.GetCellText(headerCell).Split('\n')[0];
        var match = HeaderFormNumberRegex().Match(firstLine);
        if (!match.Success) continue;

        var number = match.Groups[1].Value;
        if (seen.Add(number))
          result.Add(number);
      }

      return result;
    }

    public static WordFormData Read(
        string formNumber,
        string documentPath,
        WordFormMap map)
    {
      using var doc = WordprocessingDocument.Open(documentPath, isEditable: false);

      // A single document may contain many forms (a combined work-order
      // export has one table — or a run of tables, when a form spills onto
      // more pages than its componentsPerPage allows — per form number, back
      // to back). Only read tables that actually belong to THIS form number:
      // otherwise, reading e.g. Form 67 against a document that also has
      // Form 4/64/68/... tables would risk misinterpreting unrelated cells
      // that happen to land on the same coordinates as real Form 67 data.
      var allTables = doc.MainDocumentPart!.Document.Body!
          .Elements<Table>()
          .Where(t => TableBelongsToForm(t, formNumber))
          .ToList();

      var components = new List<WordComponentData>();

      foreach (var table in allTables)
      {
        bool anyOnThisTable = false;

        for (int slotIndex = 0; slotIndex < map.ComponentsPerPage; slotIndex++)
        {
          var slot = map.ComponentSlots[slotIndex];

          string name = ReadCoord(table, slot.MetaCells, MetaCellKey.ComponentName);
          if (string.IsNullOrWhiteSpace(name)) continue;

          anyOnThisTable = true;

          string typeName = ReadCoord(table, slot.MetaCells, MetaCellKey.ComponentType);
          string quantity = ReadCoord(table, slot.MetaCells, MetaCellKey.Quantity);

          var ntdValues = new Dictionary<int, string>();
          var schemeValues = new Dictionary<int, string>();
          var pinValues = new Dictionary<int, string>();

          foreach (var (rowKey, coord) in slot.ParameterCells)
          {
            if (!int.TryParse(rowKey, out var rowNumber)) continue;

            // Read NTD column
            var ntdCell = WordTableHelper.TryGetCell(table, coord.Row, coord.NtdCol);
            if (ntdCell is not null)
            {
              var val = WordTableHelper.GetCellText(ntdCell).Trim();
              if (!string.IsNullOrEmpty(val))
                ntdValues[rowNumber] = val;
            }

            // Read Scheme column (if defined)
            if (coord.SchemeCol.HasValue)
            {
              var schemeCell = WordTableHelper.TryGetCell(table, coord.Row, coord.SchemeCol.Value);
              if (schemeCell is not null)
              {
                var val = WordTableHelper.GetCellText(schemeCell).Trim();
                if (!string.IsNullOrEmpty(val))
                  schemeValues[rowNumber] = val;
              }
            }

            // Read Pins column (if defined — Form 64 only)
            if (coord.PinsCol.HasValue)
            {
              var pinsCell = WordTableHelper.TryGetCell(table, coord.Row, coord.PinsCol.Value);
              if (pinsCell is not null)
              {
                var val = WordTableHelper.GetCellText(pinsCell).Trim();
                if (!string.IsNullOrEmpty(val))
                  pinValues[rowNumber] = val;
              }
            }
          }

          string note = "";
          if (slot.NoteCell.HasValue)
          {
            var nc = slot.NoteCell.Value;
            var cell = WordTableHelper.TryGetCell(table, nc.Row, nc.Col);
            if (cell is not null)
              note = WordTableHelper.GetCellText(cell).Trim();
          }

          // NOTE: optional rows are NOT read back here — re-importing a
          // dynamically-inserted row would require detecting which physical
          // row index corresponds to which logical parameter after an
          // arbitrary number of insertions, which the current coordinate
          // model doesn't track. Optional-row values only flow one way
          // (app → Word) for now.

          components.Add(new WordComponentData
          {
            Name = name,
            ComponentTypeName = typeName,
            Quantity = quantity,
            NtdValues = ntdValues,
            SchemeValues = schemeValues,
            PinValues = pinValues,
            Note = note,
          });
        }

        // If this table had no filled slots and we already have components
        // from previous tables — stop (reached trailing empty tables)
        if (!anyOnThisTable && components.Count > 0) break;
      }

      return new WordFormData
      {
        FormNumber = formNumber,
        Components = components,
      };
    }

    private static string ReadCoord(
        Table table,
        IReadOnlyDictionary<string, CellCoord> cellDict,
        string key)
    {
      if (!cellDict.TryGetValue(key, out var coord)) return "";
      var cell = WordTableHelper.TryGetCell(table, coord.Row, coord.Col);
      return cell is null ? "" : WordTableHelper.GetCellText(cell).Trim();
    }

    /// <summary>
    /// True when the table's first cell's first line reads "ФОРМА
    /// &lt;formNumber&gt;" (case-insensitive). Used to pick out only the
    /// tables belonging to one form out of a document that may contain many.
    /// </summary>
    internal static bool TableBelongsToForm(Table table, string formNumber)
    {
      var headerCell = WordTableHelper.TryGetCell(table, 0, 0);
      if (headerCell is null) return false;

      var firstLine = WordTableHelper.GetCellText(headerCell).Split('\n')[0];
      var match = HeaderFormNumberRegex().Match(firstLine);
      if (!match.Success) return false;

      return string.Equals(match.Groups[1].Value, formNumber, StringComparison.OrdinalIgnoreCase);
    }
  }
}
