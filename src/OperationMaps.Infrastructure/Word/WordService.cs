using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Services;
using OperationMaps.Application.Word;

namespace OperationMaps.Infrastructure.Word
{
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

      using var doc = WordprocessingDocument.Open(ms, isEditable: true);

      var table = WordTableHelper.GetTable(doc, map.TableIndex);

      int componentCount = data.Components.Count;
      int totalPages = componentCount == 0
          ? 1
          : (int)Math.Ceiling((double)componentCount / map.ComponentsPerPage);

      // Expand table for extra pages
      for (int page = 1; page < totalPages; page++)
        WordTableHelper.CloneRowBlock(table, map.PageFirstRow, map.PageLastRow);

      int pageHeight = map.PageLastRow - map.PageFirstRow + 1;

      // Extra rows inserted per page by dynamic row logic
      var pageRowOffsets = new Dictionary<int, int>();

      for (int i = 0; i < componentCount; i++)
      {
        int pageIndex = i / map.ComponentsPerPage;
        int slotIndex = i % map.ComponentsPerPage;
        int pageOffset = pageIndex * pageHeight;
        int extraRows = pageRowOffsets.GetValueOrDefault(pageIndex);

        var component = data.Components[i];
        var slot = map.ComponentSlots[slotIndex];

        // Optional dynamic row
        if (data.HasOptionalRows
            && component.HasOptionalRow
            && map.OptionalRowInsertIndex.HasValue
            && map.OptionalRowTemplateIndex.HasValue)
        {
          int insertAt = pageOffset + extraRows + map.OptionalRowInsertIndex.Value;
          int templateRow = pageOffset + extraRows + map.OptionalRowTemplateIndex.Value;

          WordTableHelper.InsertRowBefore(table, insertAt, templateRow);

          var optCell = WordTableHelper.TryGetCell(table, insertAt, 0);
          if (optCell is not null)
            WordTableHelper.SetCellText(optCell, component.OptionalRowValue);

          pageRowOffsets[pageIndex] = extraRows + 1;
          extraRows++;
        }

        // Meta cells
        FillCoord(table, slot.MetaCells, MetaCellKey.ComponentName,
                  component.Name, pageOffset, extraRows);
        FillCoord(table, slot.MetaCells, MetaCellKey.Designation,
                  component.Designation, pageOffset, extraRows);
        FillCoord(table, slot.MetaCells, MetaCellKey.Quantity,
                  component.Quantity, pageOffset, extraRows);

        // Parameter cells — NTD first, then Scheme as fallback
        foreach (var (rowKey, coord) in slot.ParameterCells)
        {
          if (!int.TryParse(rowKey, out var rowNumber)) continue;

          string value =
              component.NtdValues.TryGetValue(rowNumber, out var ntd) ? ntd :
              component.SchemeValues.TryGetValue(rowNumber, out var scheme) ? scheme :
              "";

          var cell = WordTableHelper.TryGetCell(
              table,
              coord.Row + pageOffset + extraRows,
              coord.Col);

          if (cell is not null)
            WordTableHelper.SetCellText(cell, value);
        }

        // Note cell
        if (slot.NoteCell.HasValue && !string.IsNullOrEmpty(component.Note))
        {
          var nc = slot.NoteCell.Value;
          var cell = WordTableHelper.TryGetCell(
              table,
              nc.Row + pageOffset + extraRows,
              nc.Col);

          if (cell is not null)
            WordTableHelper.SetCellText(cell, component.Note);
        }
      }

      // Header/footer placeholders
      if (map.HeaderReplacements.Count > 0)
      {
        var resolved = ResolveHeaderFields(map.HeaderReplacements, data, totalPages);
        WordTableHelper.ReplaceInHeadersAndFooters(doc, resolved);
        WordTableHelper.ReplaceInBody(doc, resolved);
      }

      doc.MainDocumentPart!.Document.Save();
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

      var table = WordTableHelper.GetTable(doc, map.TableIndex);

      // Count filled slots by checking if the component name cell is non-empty
      var components = new List<WordComponentData>();
      int pageHeight = map.PageLastRow - map.PageFirstRow + 1;
      int maxComponents = CountFilledSlots(table, map, pageHeight);

      for (int i = 0; i < maxComponents; i++)
      {
        int pageIndex = i / map.ComponentsPerPage;
        int slotIndex = i % map.ComponentsPerPage;
        int pageOffset = pageIndex * pageHeight;

        var slot = map.ComponentSlots[slotIndex];

        // Meta
        string name = ReadCoord(table, slot.MetaCells, MetaCellKey.ComponentName, pageOffset);
        string designation = ReadCoord(table, slot.MetaCells, MetaCellKey.Designation, pageOffset);
        string quantity = ReadCoord(table, slot.MetaCells, MetaCellKey.Quantity, pageOffset);

        if (string.IsNullOrWhiteSpace(name)) continue;  // empty slot — stop reading

        // Parameter cells
        var ntdValues = new Dictionary<int, string>();
        var schemeValues = new Dictionary<int, string>();

        foreach (var (rowKey, coord) in slot.ParameterCells)
        {
          if (!int.TryParse(rowKey, out var rowNumber)) continue;

          var cell = WordTableHelper.TryGetCell(
              table,
              coord.Row + pageOffset,
              coord.Col);

          if (cell is null) continue;

          var value = WordTableHelper.GetCellText(cell).Trim();
          if (string.IsNullOrEmpty(value)) continue;

          // During import we don't know which column type (NTD vs Scheme),
          // so we populate both. The caller decides which to persist.
          ntdValues[rowNumber] = value;
          schemeValues[rowNumber] = value;
        }

        // Note cell
        string note = "";
        if (slot.NoteCell.HasValue)
        {
          var nc = slot.NoteCell.Value;
          var cell = WordTableHelper.TryGetCell(table, nc.Row + pageOffset, nc.Col);
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

      return new WordFormData
      {
        FormNumber = formNumber,
        Components = components,
      };
    }

    /// <summary>
    /// Estimates the number of filled component slots by scanning the first
    /// meta-cell (component name) of every possible slot across all pages.
    /// Stops at the first page where all slots are empty.
    /// </summary>
    private static int CountFilledSlots(Table table, WordFormMap map, int pageHeight)
    {
      // Upper bound: check up to 50 pages (arbitrary safety cap)
      int maxPages = 50;
      int count = 0;

      for (int page = 0; page < maxPages; page++)
      {
        int pageOffset = page * pageHeight;
        bool anyOnPage = false;

        for (int slot = 0; slot < map.ComponentsPerPage; slot++)
        {
          var slotMap = map.ComponentSlots[slot];
          if (!slotMap.MetaCells.TryGetValue(MetaCellKey.ComponentName, out var coord))
            continue;

          var cell = WordTableHelper.TryGetCell(
              table,
              coord.Row + pageOffset,
              coord.Col);

          if (cell is null) goto done;  // row doesn't exist → no more pages

          var text = WordTableHelper.GetCellText(cell).Trim();
          if (!string.IsNullOrEmpty(text))
          {
            count++;
            anyOnPage = true;
          }
        }

        if (!anyOnPage) break;  // whole page empty → stop
      }

    done:
      return count;
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static void FillCoord(
        Table table,
        IReadOnlyDictionary<string, CellCoord> cellDict,
        string key,
        string value,
        int pageOffset,
        int extraRows)
    {
      if (!cellDict.TryGetValue(key, out var coord)) return;

      var cell = WordTableHelper.TryGetCell(
          table,
          coord.Row + pageOffset + extraRows,
          coord.Col);

      if (cell is not null)
        WordTableHelper.SetCellText(cell, value);
    }

    private static string ReadCoord(
        Table table,
        IReadOnlyDictionary<string, CellCoord> cellDict,
        string key,
        int pageOffset)
    {
      if (!cellDict.TryGetValue(key, out var coord)) return "";

      var cell = WordTableHelper.TryGetCell(
          table,
          coord.Row + pageOffset,
          coord.Col);

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
      {
        if (fields.TryGetValue(fieldName, out var fieldValue))
          result[placeholder] = fieldValue;
      }

      return result;
    }

  }
}
