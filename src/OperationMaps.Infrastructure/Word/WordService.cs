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

      // Replace header/footer placeholders directly in the ZIP XML before opening
      // via SDK — needed because TextBox content uses wne:txbxContent which SDK
      // does not traverse via Descendants<Paragraph>().
      if (!string.IsNullOrEmpty(data.DocumentDesignation) && map.HeaderReplacements.Count > 0)
      {
        var resolved = ResolveHeaderFields(map.HeaderReplacements, data, 1);
        ms = ReplaceInZipXml(ms, resolved,
            "word/header1.xml", "word/header2.xml", "word/header3.xml",
            "word/footer1.xml", "word/footer2.xml", "word/footer3.xml");
      }

      // Use explicit using block so doc is fully closed (and file handles flushed)
      // before we read bytes from the stream.
      using (var doc = WordprocessingDocument.Open(ms, isEditable: true))
      {
        int componentCount = data.Components.Count;
        int totalPages = componentCount == 0
            ? 1
            : (int)Math.Ceiling((double)componentCount / map.ComponentsPerPage);

        var originalTable = WordTableHelper.GetTable(doc, map.TableIndex);

        // Remove any stray paragraphs that follow the original table
        // (templates often have trailing empty paragraphs that cause a blank page)
        WordTableHelper.RemoveTrailingParagraphsAfterTable(
            doc, originalTable);

        // Fill operating conditions into the original table BEFORE cloning —
        // they will be copied to all subsequent pages automatically.
        if (map.OperatingConditionCells.Count > 0 && data.OperatingConditions is not null)
        {
          var oc = data.OperatingConditions;
          var ocValues = new Dictionary<string, string?>
          {
            ["resource"] = oc.Resource,
            ["serviceLife"] = oc.ServiceLife,
            ["storageLife"] = oc.StorageLife,
            ["acousticNoiseFreq"] = oc.AcousticNoiseFrequency,
            ["acousticNoisePressure"] = oc.AcousticNoisePressure,
            ["linearAcceleration"] = oc.LinearAcceleration,
            ["pressureLow"] = oc.PressureLow,
            ["pressureHigh"] = oc.PressureHigh,
            ["temperatureLow"] = oc.TemperatureLow,
            ["temperatureHigh"] = oc.TemperatureHigh,
            ["humidity"] = oc.Humidity,
            ["humidityTemperature"] = oc.HumidityTemperature,
          };

          foreach (var (key, coord) in map.OperatingConditionCells)
          {
            if (!ocValues.TryGetValue(key, out var val)) continue;
            var cell = WordTableHelper.TryGetCell(originalTable, coord.Row, coord.Col);
            if (cell is not null)
              WordTableHelper.SetCellText(cell, val ?? "—");
          }
        }

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

          Dbg($"Component[{i}] '{component.Name}' page={pageIndex} slot={slotIndex} NTD={component.NtdValues.Count}");

          DebugFill(table, slot.MetaCells, MetaCellKey.ComponentName, component.Name, "name");
          DebugFill(table, slot.MetaCells, MetaCellKey.ComponentType, component.ComponentTypeName, "componentType");
          DebugFill(table, slot.MetaCells, MetaCellKey.Quantity, component.Quantity, "quantity");

          foreach (var (rowKey, coord) in slot.ParameterCells)
          {
            if (!int.TryParse(rowKey, out var rowNumber)) continue;

            string value =
                component.NtdValues.TryGetValue(rowNumber, out var ntd) ? ntd :
                component.SchemeValues.TryGetValue(rowNumber, out var scheme) ? scheme :
                "—";

            var cell = WordTableHelper.TryGetCell(table, coord.Row, coord.Col);
            Dbg($"  param#{rowNumber} -> [{coord.Row},{coord.Col}] cell={cell is not null} val='{value}'");
            if (cell is not null)
              WordTableHelper.SetCellText(cell, value);
          }

          if (slot.NoteCell.HasValue && !string.IsNullOrEmpty(component.Note))
          {
            var nc = slot.NoteCell.Value;
            var cell = WordTableHelper.TryGetCell(table, nc.Row, nc.Col);
            Dbg($"  note -> [{nc.Row},{nc.Col}] cell={cell is not null}");
            if (cell is not null)
              WordTableHelper.SetCellText(cell, component.Note);
          }
        }

        // Header/footer placeholders are already replaced at ZIP level above.
        // Only replace in body (cover-page placeholders etc.)
        if (map.HeaderReplacements.Count > 0)
        {
          var resolved = ResolveHeaderFields(map.HeaderReplacements, data, totalPages);
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

          string typeName = ReadCoord(table, slot.MetaCells, MetaCellKey.ComponentType);
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
            ComponentTypeName = typeName,
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

    [System.Diagnostics.Conditional("DEBUG")]
    private static void Dbg(string msg)
        => System.Diagnostics.Debug.WriteLine($"[Word] {msg}");

    private static void DebugFill(
        Table table,
        IReadOnlyDictionary<string, CellCoord> cellDict,
        string key,
        string value,
        string label)
    {
      if (!cellDict.TryGetValue(key, out var coord))
      {
        Dbg($"  {label} -> key '{key}' NOT FOUND in map");
        return;
      }
      var cell = WordTableHelper.TryGetCell(table, coord.Row, coord.Col);
      Dbg($"  {label} -> [{coord.Row},{coord.Col}] cell={cell is not null} val='{value}'");
      if (cell is not null)
        WordTableHelper.SetCellText(cell, value);
    }

    /// <summary>
    /// Opens the docx ZIP in memory, replaces all placeholder strings in the
    /// specified XML entry files, and returns a new MemoryStream with the result.
    /// This bypasses the OpenXml SDK object model and works on raw XML text,
    /// so it reaches content inside TextBoxes (wne:txbxContent) that the SDK
    /// does not traverse.
    /// </summary>
    private static MemoryStream ReplaceInZipXml(
        MemoryStream source,
        IReadOnlyDictionary<string, string> replacements,
        params string[] entryNames)
    {
      source.Position = 0;
      var output = new MemoryStream();

      using (var zin = new System.IO.Compression.ZipArchive(source, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true))
      using (var zout = new System.IO.Compression.ZipArchive(output, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
      {
        var entrySet = new HashSet<string>(entryNames, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in zin.Entries)
        {
          var outEntry = zout.CreateEntry(entry.FullName, System.IO.Compression.CompressionLevel.Optimal);

          using var inStream = entry.Open();
          using var outStream = outEntry.Open();

          if (entrySet.Contains(entry.FullName))
          {
            // Read, replace, write as UTF-8
            using var reader = new StreamReader(inStream, System.Text.Encoding.UTF8);
            var xml = reader.ReadToEnd();

            foreach (var (placeholder, value) in replacements)
              xml = xml.Replace(placeholder, System.Security.SecurityElement.Escape(value));

            var bytes = System.Text.Encoding.UTF8.GetBytes(xml);
            outStream.Write(bytes, 0, bytes.Length);
          }
          else
          {
            inStream.CopyTo(outStream);
          }
        }
      }

      output.Position = 0;
      return output;
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
