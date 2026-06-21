using System.Text.Json.Nodes;

namespace OperationMaps.Infrastructure.Word
{
  //// <summary>
  /// Loads <c>map.json</c> files from the Resources directory and caches them
  /// for the lifetime of the application.
  /// </summary>
  public sealed class WordFormMapLoader
  {
    private readonly string _baseDirectory;
    private readonly Dictionary<string, WordFormMap> _cache = new();
    private readonly Lock _lock = new();

    public WordFormMapLoader(string baseDirectory)
    {
      if (!Directory.Exists(baseDirectory))
        throw new DirectoryNotFoundException(
            $"Word resources directory not found: {baseDirectory}");

      _baseDirectory = baseDirectory;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public WordFormMap Load(string formNumber)
    {
      lock (_lock)
      {
        if (_cache.TryGetValue(formNumber, out var cached))
          return cached;

        var map = ParseFile(formNumber);
        _cache[formNumber] = map;
        return map;
      }
    }

    public string GetTemplatePath(string formNumber)
    {
      var path = Path.Combine(_baseDirectory, $"Form{formNumber}", "template.docx");

      if (!File.Exists(path))
        throw new FileNotFoundException(
            $"Word template not found for form {formNumber}: {path}");

      return path;
    }

    // ── Parsing ───────────────────────────────────────────────────────────────

    private WordFormMap ParseFile(string formNumber)
    {
      var mapPath = Path.Combine(_baseDirectory, $"Form{formNumber}", "map.json");

      if (!File.Exists(mapPath))
        throw new FileNotFoundException(
            $"map.json not found for form {formNumber}: {mapPath}");

      using var stream = File.OpenRead(mapPath);
      var root = JsonNode.Parse(stream)?.AsObject()
          ?? throw new InvalidDataException($"map.json is not a JSON object: {mapPath}");

      var tableIndex = root["tableIndex"]?.GetValue<int>() ?? 0;
      var componentsPerPage = root["componentsPerPage"]?.GetValue<int>()
          ?? throw new InvalidDataException($"componentsPerPage missing in {mapPath}");

      int? optionalRowInsertIndex = root["optionalRowInsertIndex"]?.GetValue<int>();
      int? optionalRowTemplateIndex = root["optionalRowTemplateIndex"]?.GetValue<int>();

      var slotsNode = root["componentSlots"]?.AsArray()
          ?? throw new InvalidDataException($"componentSlots missing in {mapPath}");

      var slots = slotsNode
          .Select((node, i) => ParseSlot(node, i, mapPath))
          .ToList();

      if (slots.Count != componentsPerPage)
        throw new InvalidDataException(
            $"componentSlots count ({slots.Count}) does not match " +
            $"componentsPerPage ({componentsPerPage}) in {mapPath}");

      var headerReplacements = new Dictionary<string, string>();
      if (root["headerReplacements"] is JsonObject hrObj)
        foreach (var (key, value) in hrObj)
          if (value?.GetValue<string>() is string v)
            headerReplacements[key] = v;

      var operatingConditionCells = new Dictionary<string, CellCoord>();
      if (root["operatingConditionCells"] is JsonObject ocObj)
        foreach (var (key, value) in ocObj)
        {
          if (key == "comment" || value is null) continue;
          operatingConditionCells[key] = ParseCoord(value, -1, key, mapPath);
        }

      var optionalRows = ParseOptionalRows(root["optionalRows"], componentsPerPage, mapPath);

      return new WordFormMap
      {
        FormNumber = formNumber,
        TableIndex = tableIndex,
        ComponentsPerPage = componentsPerPage,
        ComponentSlots = slots,
        HeaderReplacements = headerReplacements,
        OperatingConditionCells = operatingConditionCells,
        OptionalRowInsertIndex = optionalRowInsertIndex,
        OptionalRowTemplateIndex = optionalRowTemplateIndex,
        OptionalRows = optionalRows,
      };
    }

    private static ComponentSlotMap ParseSlot(JsonNode? node, int slotIndex, string mapPath)
    {
      var obj = node?.AsObject()
          ?? throw new InvalidDataException($"Slot {slotIndex} is not an object in {mapPath}");

      return new ComponentSlotMap
      {
        MetaCells = ParseCellDict(obj["metaCells"], slotIndex, mapPath),
        ParameterCells = ParseParameterCellDict(obj["parameterCells"], slotIndex, mapPath),
        NoteCell = ParseOptionalCoord(obj["noteCell"]),
      };
    }

    /// <summary>
    /// Parses the top-level "optionalRows" section, e.g.:
    /// <code>
    /// "optionalRows": {
    ///   "1": {
    ///     "primaryRowNumber": 1,
    ///     "templateRowIndex": 4,
    ///     "slots": [
    ///       { "ntdCol": 5, "schemeCol": 4 },
    ///       { "ntdCol": 9, "schemeCol": 8 }
    ///     ]
    ///   }
    /// }
    /// </code>
    /// Key is the OPTIONAL FormParameter.RowNumber as a string. "slots" must
    /// have exactly <paramref name="componentsPerPage"/> entries, one per
    /// component slot on the page, listing that slot's column coordinates
    /// for the cloned row. Returns one <see cref="OptionalRowMap"/> per
    /// (parameter, slotIndex) pair, keyed as "{rowNumber}:{slotIndex}" so the
    /// export service can look it up per-component without re-parsing slots.
    /// </summary>
    private static IReadOnlyDictionary<string, OptionalRowMap> ParseOptionalRows(
        JsonNode? node, int componentsPerPage, string mapPath)
    {
      if (node is null) return new Dictionary<string, OptionalRowMap>();

      var result = new Dictionary<string, OptionalRowMap>();

      foreach (var (rowKey, value) in node.AsObject())
      {
        if (value is null) continue;
        var obj = value.AsObject();

        var primaryRowNumber = obj["primaryRowNumber"]?.GetValue<int>()
            ?? throw new InvalidDataException($"Missing 'primaryRowNumber' in optionalRows['{rowKey}'] in {mapPath}");

        var templateRowIndex1Based = obj["templateRowIndex"]?.GetValue<int>()
            ?? throw new InvalidDataException($"Missing 'templateRowIndex' in optionalRows['{rowKey}'] in {mapPath}");

        var slotsArray = obj["slots"]?.AsArray()
            ?? throw new InvalidDataException($"Missing 'slots' in optionalRows['{rowKey}'] in {mapPath}");

        if (slotsArray.Count != componentsPerPage)
          throw new InvalidDataException(
              $"optionalRows['{rowKey}'].slots count ({slotsArray.Count}) does not match " +
              $"componentsPerPage ({componentsPerPage}) in {mapPath}");

        for (int slotIndex = 0; slotIndex < slotsArray.Count; slotIndex++)
        {
          var slotObj = slotsArray[slotIndex]?.AsObject()
              ?? throw new InvalidDataException($"optionalRows['{rowKey}'].slots[{slotIndex}] is not an object in {mapPath}");

          var ntdCol = slotObj["ntdCol"]?.GetValue<int>()
              ?? throw new InvalidDataException($"Missing 'ntdCol' in optionalRows['{rowKey}'].slots[{slotIndex}] in {mapPath}");
          var schemeCol = slotObj["schemeCol"]?.GetValue<int>();
          var pinsCol = slotObj["pins"]?.GetValue<int>();

          // Row index in the coord is left at the template position; the
          // export service computes the actual post-insertion row at runtime
          // (TemplateRowIndex + 1), since inserting a row shifts everything
          // below it down by one.
          var coord = new ParameterCoord(
              templateRowIndex1Based - 1,
              ntdCol - 1,
              schemeCol.HasValue ? schemeCol.Value - 1 : null,
              pinsCol.HasValue ? pinsCol.Value - 1 : null);

          var lookupKey = $"{rowKey}:{slotIndex}";
          result[lookupKey] = new OptionalRowMap
          {
            PrimaryRowNumber = primaryRowNumber,
            TemplateRowIndex = templateRowIndex1Based - 1,
            Coord = coord,
          };
        }
      }

      return result;
    }

    /// <summary>
    /// Parses parameterCells — supports both old format { row, col }
    /// and new format { row, ntdCol, schemeCol? }.
    /// Old format maps col → ntdCol with schemeCol = null (backward compatible).
    /// </summary>
    private static IReadOnlyDictionary<string, ParameterCoord> ParseParameterCellDict(
        JsonNode? node, int slotIndex, string mapPath)
    {
      if (node is null) return new Dictionary<string, ParameterCoord>();

      var result = new Dictionary<string, ParameterCoord>();
      foreach (var (key, value) in node.AsObject())
      {
        if (key == "comment" || value is null) continue;
        var obj = value.AsObject();
        var row = obj["row"]?.GetValue<int>()
            ?? throw new InvalidDataException($"Missing 'row' in parameterCells[{key}] slot {slotIndex}");

        // New format: ntdCol + optional schemeCol + optional pins (Form 64)
        if (obj["ntdCol"] is not null)
        {
          var ntdCol = obj["ntdCol"]!.GetValue<int>();
          var schemeCol = obj["schemeCol"]?.GetValue<int>();
          var pinsCol = obj["pins"]?.GetValue<int>();
          result[key] = new ParameterCoord(
              row - 1,
              ntdCol - 1,
              schemeCol.HasValue ? schemeCol.Value - 1 : null,
              pinsCol.HasValue ? pinsCol.Value - 1 : null);
        }
        // Old format: col only (backward compatible — only NTD)
        else
        {
          var col = obj["col"]?.GetValue<int>()
              ?? throw new InvalidDataException($"Missing 'col' in parameterCells[{key}] slot {slotIndex}");
          result[key] = new ParameterCoord(row - 1, col - 1, null);
        }
      }
      return result;
    }

    private static IReadOnlyDictionary<string, CellCoord> ParseCellDict(
        JsonNode? node, int slotIndex, string mapPath)
    {
      if (node is null) return new Dictionary<string, CellCoord>();

      var result = new Dictionary<string, CellCoord>();
      foreach (var (key, value) in node.AsObject())
      {
        if (key == "comment" || value is null) continue;
        result[key] = ParseCoord(value, slotIndex, key, mapPath);
      }
      return result;
    }

    private static CellCoord ParseCoord(JsonNode node, int slotIndex, string key, string mapPath)
    {
      var obj = node.AsObject();
      var row = obj["row"]?.GetValue<int>()
          ?? throw new InvalidDataException($"Missing 'row' in slot {slotIndex} key '{key}' in {mapPath}");
      var col = obj["col"]?.GetValue<int>()
          ?? throw new InvalidDataException($"Missing 'col' in slot {slotIndex} key '{key}' in {mapPath}");

      // Convert 1-based (JSON) → 0-based (C#)
      return new CellCoord(row - 1, col - 1);
    }

    private static CellCoord? ParseOptionalCoord(JsonNode? node)
    {
      if (node is null) return null;
      var obj = node.AsObject();
      var row = obj["row"]?.GetValue<int>();
      var col = obj["col"]?.GetValue<int>();
      if (row is null || col is null) return null;
      return new CellCoord(row.Value - 1, col.Value - 1);
    }
  }
}
