using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Word;

namespace OperationMaps.Infrastructure.Word
{
  /// <summary>
  /// Fills a Word template with <see cref="WordFormData"/> using
  /// <c>map.json</c> coordinates. The write half of what used to be
  /// <c>WordService</c> — split out so it can be reasoned about (and
  /// tested) independently of the read half (<see cref="WordFormReader"/>).
  /// </summary>
  public static class WordFormWriter
  {
    public static byte[] Fill(
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
        WordTableHelper.RemoveTrailingParagraphsAfterTable(doc, originalTable);

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
          DebugFill(table, slot.MetaCells, MetaCellKey.PositionsNumber, component.Positions, "positionsNumber");

          foreach (var (rowKey, coord) in slot.ParameterCells)
          {
            if (!int.TryParse(rowKey, out var rowNumber)) continue;

            string ntdValue =
                component.NtdValues.TryGetValue(rowNumber, out var ntd) ? ntd : "—";

            string schemeValue =
                component.SchemeValues.TryGetValue(rowNumber, out var scheme) ? scheme : "—";

            string pinsValue =
                component.PinValues.TryGetValue(rowNumber, out var pins) ? pins : "—";

            // Fill NTD column (always)
            var ntdCell = WordTableHelper.TryGetCell(table, coord.Row, coord.NtdCol);
            if (ntdCell is not null)
              WordTableHelper.SetCellText(ntdCell, ntdValue);

            // Fill Scheme column (only if schemeCol is defined in map.json)
            if (coord.SchemeCol.HasValue)
            {
              var schemeCell = WordTableHelper.TryGetCell(table, coord.Row, coord.SchemeCol.Value);
              if (schemeCell is not null)
                WordTableHelper.SetCellText(schemeCell, schemeValue);
            }

            // Fill Pins column (only if pinsCol is defined — Form 64 only)
            if (coord.PinsCol.HasValue)
            {
              var pinsCell = WordTableHelper.TryGetCell(table, coord.Row, coord.PinsCol.Value);
              if (pinsCell is not null)
                WordTableHelper.SetCellText(pinsCell, pinsValue);
            }
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

        // ── Optional rows (e.g. Form 64's second supply voltage) ──────────────
        // Must run AFTER the main fill loop above, because inserting a row
        // shifts every row index below it by +1 — doing this first would
        // desynchronize all the static ParameterCells coordinates used above.
        // Each page is handled independently; within a page, each slot that
        // actually has optional-row data gets its own inserted row, so two
        // components sharing a page can differ (one has a second voltage,
        // the other doesn't) without affecting each other.
        if (map.OptionalRows.Count > 0)
          FillOptionalRows(data, map, pageTables);

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

    /// <summary>
    /// Inserts and fills the optional parameter row (e.g. Form 64's second
    /// supply voltage) once PER PAGE, for every primary parameter that has
    /// at least one component with optional-row data on that page.
    /// <para>
    /// The inserted row spans the WHOLE table width — it's a real extra
    /// table row, so every slot on the page gets a cell in it. A component
    /// that has no optional-row value of its own still gets its slot's
    /// cells filled with an em dash ("—"), so the row reads as one
    /// consistent line across the page rather than leaving gaps for the
    /// slots that didn't trigger the insertion.
    /// </para>
    /// <para>
    /// Critical ordering detail: if MULTIPLE slots on the SAME page each
    /// need their own optional row, inserting them naively at the same
    /// template row index would interleave them in the wrong order — the
    /// second insertion's target row index is no longer correct once the
    /// first insertion has shifted the table. To avoid this, all optional
    /// rows for a page are inserted in slot order, and each insertion's
    /// target index is offset by how many rows have ALREADY been inserted
    /// on that page so far.
    /// </para>
    /// </summary>
    private static void FillOptionalRows(
        WordFormData data,
        WordFormMap map,
        List<Table> pageTables)
    {
      int componentCount = data.Components.Count;

      // Group components by (page, optionalRowNumber) so we know, for each
      // page and each optional parameter, which slot(s) actually have a
      // value and what that value is — needed to fill every slot's cell
      // in the SAME inserted row, not just the slot that triggered it.
      var byPageAndRow = new Dictionary<(int Page, string RowKey), Dictionary<int, OptionalRowValues>>();

      for (int i = 0; i < componentCount; i++)
      {
        var component = data.Components[i];
        if (component.OptionalRowValuesByParameter.Count == 0) continue;

        int pageIndex = i / map.ComponentsPerPage;
        int slotIndex = i % map.ComponentsPerPage;

        foreach (var (optionalRowNumber, values) in component.OptionalRowValuesByParameter)
        {
          var key = (pageIndex, optionalRowNumber.ToString());
          if (!byPageAndRow.TryGetValue(key, out var bySlot))
          {
            bySlot = new Dictionary<int, OptionalRowValues>();
            byPageAndRow[key] = bySlot;
          }
          bySlot[slotIndex] = values;
        }
      }

      // Track how many optional rows have already been inserted per page,
      // so later insertions on the same page offset their target index.
      var insertedRowsPerPage = new Dictionary<int, int>();

      foreach (var ((pageIndex, rowKey), bySlot) in byPageAndRow)
      {
        // Skip if every slot's value is actually empty (nothing to show at all).
        bool anyRealValue = bySlot.Values.Any(v =>
            !string.IsNullOrWhiteSpace(v.NtdValue) ||
            !string.IsNullOrWhiteSpace(v.SchemeValue) ||
            !string.IsNullOrWhiteSpace(v.PinsValue));
        if (!anyRealValue) continue;

        var table = pageTables[pageIndex];
        insertedRowsPerPage.TryGetValue(pageIndex, out var alreadyInserted);

        // All slots on this page share the same TemplateRowIndex (the
        // primary parameter's row index is the same regardless of slot) —
        // read it once from whichever slot's map entry exists.
        var anyLookupKey = $"{rowKey}:{bySlot.Keys.First()}";
        if (!map.OptionalRows.TryGetValue(anyLookupKey, out var anyRowMap)) continue;

        var targetTemplateIndex = anyRowMap.TemplateRowIndex + alreadyInserted;

        // Determine which physical columns get a real split cell — only
        // the Ntd/Scheme/Pins columns belonging to slots that actually
        // have a (non-empty) value. Everything else — left-hand labels,
        // and slots with no value at all — merges invisibly with the row
        // above via vMerge="continue".
        var splitColumns = new HashSet<int>();
        for (int slotIndex = 0; slotIndex < map.ComponentsPerPage; slotIndex++)
        {
          var lookupKey = $"{rowKey}:{slotIndex}";
          if (!map.OptionalRows.TryGetValue(lookupKey, out var rowMap)) continue;
          if (!bySlot.TryGetValue(slotIndex, out var values)) continue;

          if (!string.IsNullOrWhiteSpace(values.NtdValue))
            splitColumns.Add(rowMap.Coord.NtdCol);
          if (rowMap.Coord.SchemeCol.HasValue && !string.IsNullOrWhiteSpace(values.SchemeValue))
            splitColumns.Add(rowMap.Coord.SchemeCol.Value);
          if (rowMap.Coord.PinsCol.HasValue && !string.IsNullOrWhiteSpace(values.PinsValue))
            splitColumns.Add(rowMap.Coord.PinsCol.Value);
        }

        WordTableHelper.InsertSplitRow(table, targetTemplateIndex, targetTemplateIndex, splitColumns);
        var insertedRowIndex = targetTemplateIndex + 1;

        // Fill only the columns we actually split — the rest are merged
        // (vMerge="continue") and must NOT receive any text.
        for (int slotIndex = 0; slotIndex < map.ComponentsPerPage; slotIndex++)
        {
          var lookupKey = $"{rowKey}:{slotIndex}";
          if (!map.OptionalRows.TryGetValue(lookupKey, out var rowMap)) continue;
          if (!bySlot.TryGetValue(slotIndex, out var values)) continue;

          if (!string.IsNullOrWhiteSpace(values.NtdValue))
          {
            var ntdCell = WordTableHelper.TryGetCell(table, insertedRowIndex, rowMap.Coord.NtdCol);
            if (ntdCell is not null)
              WordTableHelper.SetCellText(ntdCell, values.NtdValue!);
          }

          if (rowMap.Coord.SchemeCol.HasValue && !string.IsNullOrWhiteSpace(values.SchemeValue))
          {
            var schemeCell = WordTableHelper.TryGetCell(table, insertedRowIndex, rowMap.Coord.SchemeCol.Value);
            if (schemeCell is not null)
              WordTableHelper.SetCellText(schemeCell, values.SchemeValue!);
          }

          if (rowMap.Coord.PinsCol.HasValue && !string.IsNullOrWhiteSpace(values.PinsValue))
          {
            var pinsCell = WordTableHelper.TryGetCell(table, insertedRowIndex, rowMap.Coord.PinsCol.Value);
            if (pinsCell is not null)
              WordTableHelper.SetCellText(pinsCell, values.PinsValue!);
          }

          Dbg($"  optional row [{lookupKey}] row={insertedRowIndex} ntd='{values.NtdValue}' scheme='{values.SchemeValue}' pins='{values.PinsValue}'");
        }

        alreadyInserted++;
        insertedRowsPerPage[pageIndex] = alreadyInserted;
      }
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

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
