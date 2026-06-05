using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Services;
using OperationMaps.Application.Word;

namespace OperationMaps.Infrastructure.Word
{
  /// <summary>
  /// Unified Word service: fills templates (export) and reads existing
  /// documents (import) using the same <c>map.json</c> coordinates.
  /// </summary>
  public sealed class WordService : IWordService
  {
    private readonly WordFormMapLoader _mapLoader;

    public WordService(WordFormMapLoader mapLoader)
    {
      _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
    }

    // ── Export ────────────────────────────────────────────────────────────────

    public Task<byte[]> ExportAsync(
        WordFormData data,
        string templatePath,
        CancellationToken ct = default)
    {
      ct.ThrowIfCancellationRequested();

      var map = _mapLoader.Load(data.FormNumber);
      var bytes = FillTemplate(data, map, templatePath);
      return Task.FromResult(bytes);
    }

    // ── Import ────────────────────────────────────────────────────────────────

    public Task<WordFormData> ImportAsync(
        string formNumber,
        string documentPath,
        CancellationToken ct = default)
    {
      ct.ThrowIfCancellationRequested();

      if (!File.Exists(documentPath))
        throw new FileNotFoundException(
            $"Word document not found: {documentPath}");

      var map = _mapLoader.Load(formNumber);
      var data = ReadDocument(formNumber, documentPath, map);
      return Task.FromResult(data);
    }

    // ── Export implementation ─────────────────────────────────────────────────

    private static byte[] FillTemplate(
        WordFormData data,
        WordFormMap map,
        string templatePath)
    {
      var ms = new MemoryStream();
      using (var fs = File.OpenRead(templatePath))
        fs.CopyTo(ms);
      ms.Position = 0;

      // Use explicit using block so doc is fully closed (and file handles flushed)
      // before we read bytes from the stream.
      using (var doc = WordprocessingDocument.Open(ms, isEditable: true))
      {
        int componentCount = data.Components.Count;
        int totalPages = componentCount == 0
            ? 1
            : (int)Math.Ceiling((double)componentCount / map.ComponentsPerPage);

        var originalTable = WordTableHelper.GetTable(doc, map.TableIndex);
        var pageTables = new List<Table> { originalTable };

        for (int page = 1; page < totalPages; page++)
          pageTables.Add(WordTableHelper.CloneTable(doc, pageTables[page - 1]));

        for (int i = 0; i < componentCount; i++)
        {
          int pageIndex = i / map.ComponentsPerPage;
          int slotIndex = i % map.ComponentsPerPage;

          var table = pageTables[pageIndex];
          var component = data.Components[i];
          var slot = map.ComponentSlots[slotIndex];

          FillCoord(table, slot.MetaCells, MetaCellKey.ComponentName, component.Name);
          FillCoord(table, slot.MetaCells, MetaCellKey.ComponentTypeName, component.ComponentTypeName);
          FillCoord(table, slot.MetaCells, MetaCellKey.Quantity, component.Quantity);

          foreach (var (rowKey, coord) in slot.ParameterCells)
          {
            if (!int.TryParse(rowKey, out var rowNumber)) continue;

            string value =
                component.NtdValues.TryGetValue(rowNumber, out var ntd) ? ntd :
                component.SchemeValues.TryGetValue(rowNumber, out var scheme) ? scheme :
                "";

            var cell = WordTableHelper.TryGetCell(table, coord.Row, coord.Col);
            if (cell is not null)
              WordTableHelper.SetCellText(cell, value);
          }

          if (slot.NoteCell.HasValue && !string.IsNullOrEmpty(component.Note))
          {
            var nc = slot.NoteCell.Value;
            var cell = WordTableHelper.TryGetCell(table, nc.Row, nc.Col);
            if (cell is not null)
              WordTableHelper.SetCellText(cell, component.Note);
          }
        }

        if (map.HeaderReplacements.Count > 0)
        {
          var resolved = ResolveHeaderFields(map.HeaderReplacements, data, totalPages);
          WordTableHelper.ReplaceInHeadersAndFooters(doc, resolved);
          WordTableHelper.ReplaceInBody(doc, resolved);
        }

        doc.MainDocumentPart!.Document.Save();
        doc.Save(); // flush entire package to MemoryStream
      } // doc.Dispose()

      ms.Position = 0;
      return ms.ToArray();
    }

    // ── Import implementation ─────────────────────────────────────────────────

    private static WordFormData ReadDocument(
        string formNumber,
        string documentPath,
        WordFormMap map)
    {
      using var doc = WordprocessingDocument.Open(documentPath, isEditable: false);

      // Each page is a separate table — collect all tables in document order
      var allTables = doc.MainDocumentPart!.Document.Body!
          .Elements<Table>().ToList();

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

          string designation = ReadCoord(table, slot.MetaCells, MetaCellKey.Designation);
          string quantity = ReadCoord(table, slot.MetaCells, MetaCellKey.Quantity);

          var ntdValues = new Dictionary<int, string>();
          var schemeValues = new Dictionary<int, string>();

          foreach (var (rowKey, coord) in slot.ParameterCells)
          {
            if (!int.TryParse(rowKey, out var rowNumber)) continue;

            var cell = WordTableHelper.TryGetCell(table, coord.Row, coord.Col);
            if (cell is null) continue;

            var value = WordTableHelper.GetCellText(cell).Trim();
            if (string.IsNullOrEmpty(value)) continue;

            ntdValues[rowNumber] = value;
            schemeValues[rowNumber] = value;
          }

          string note = "";
          if (slot.NoteCell.HasValue)
          {
            var nc = slot.NoteCell.Value;
            var cell = WordTableHelper.TryGetCell(table, nc.Row, nc.Col);
            if (cell is not null)
              note = WordTableHelper.GetCellText(cell).Trim();
          }

          components.Add(new WordComponentData
          {
            Name = name,
            Designation = designation,
            Quantity = quantity,
            NtdValues = ntdValues,
            SchemeValues = schemeValues,
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

    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Writes <paramref name="value"/> into the cell identified by <paramref name="key"/>.
    /// Silently skips when the key is not in the dict or the cell doesn't exist.
    /// </summary>
    private static void FillCoord(
        Table table,
        IReadOnlyDictionary<string, CellCoord> cellDict,
        string key,
        string value,
        int rowOffset = 0,
        int colOffset = 0)
    {
      if (!cellDict.TryGetValue(key, out var coord)) return;

      var cell = WordTableHelper.TryGetCell(table, coord.Row + rowOffset, coord.Col + colOffset);
      if (cell is not null)
        WordTableHelper.SetCellText(cell, value);
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

    private static IReadOnlyDictionary<string, string> ResolveHeaderFields(
        IReadOnlyDictionary<string, string> mapReplacements,
        WordFormData data,
        int totalPages)
    {
      var fields = new Dictionary<string, string>(data.HeaderFields)
      {
        ["sheetNumber"] = data.HeaderFields.GetValueOrDefault("sheetNumber", "1"),
        ["totalSheets"] = data.HeaderFields.GetValueOrDefault("totalSheets", totalPages.ToString()),
        ["designation"] = data.HeaderFields.GetValueOrDefault("designation", data.DocumentDesignation),
      };

      var result = new Dictionary<string, string>();
      foreach (var (placeholder, fieldName) in mapReplacements)
        if (fields.TryGetValue(fieldName, out var fieldValue))
          result[placeholder] = fieldValue;

      return result;
    }
  }
}
