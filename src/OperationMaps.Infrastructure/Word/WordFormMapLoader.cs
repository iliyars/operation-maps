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

      return new WordFormMap
      {
        FormNumber = formNumber,
        TableIndex = tableIndex,
        ComponentsPerPage = componentsPerPage,
        ComponentSlots = slots,
        HeaderReplacements = headerReplacements,
        OptionalRowInsertIndex = optionalRowInsertIndex,
        OptionalRowTemplateIndex = optionalRowTemplateIndex,
      };
    }

    private static ComponentSlotMap ParseSlot(JsonNode? node, int slotIndex, string mapPath)
    {
      var obj = node?.AsObject()
          ?? throw new InvalidDataException($"Slot {slotIndex} is not an object in {mapPath}");

      return new ComponentSlotMap
      {
        MetaCells = ParseCellDict(obj["metaCells"], slotIndex, mapPath),
        ParameterCells = ParseCellDict(obj["parameterCells"], slotIndex, mapPath),
        NoteCell = ParseOptionalCoord(obj["noteCell"]),
      };
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
