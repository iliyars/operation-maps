using System.Text.Json.Nodes;

namespace OperationMaps.Infrastructure.Word
{
  /// <summary>
  /// Loads <c>map.json</c> files from the Resources directory and caches them
  /// for the lifetime of the application.
  /// <para>
  /// Expected directory layout under <c>baseDirectory</c>:
  /// <code>
  /// Form4/
  ///   template.docx
  ///   map.json
  /// Form67/
  ///   template.docx
  ///   map.json
  /// </code>
  /// </para>
  /// </summary>
  public class WordFormMapLoader
  {
    private readonly string _baseDirectory;
    private readonly Dictionary<string, WordFormMap> _cache = new();
    private readonly Lock _lock = new();

    /// <param name="baseDirectory">
    /// Absolute path to the folder that contains <c>Form{N}/</c> subdirectories.
    /// Typically <c>Path.Combine(AppContext.BaseDirectory, "Word", "Resources")</c>.
    /// </param>
    public WordFormMapLoader(string baseDirectory)
    {
      if (!Directory.Exists(baseDirectory))
        throw new DirectoryNotFoundException(
            $"Word resources directory not found: {baseDirectory}");

      _baseDirectory = baseDirectory;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="WordFormMap"/> for the given form number.
    /// Result is cached — the file is read only once per process lifetime.
    /// </summary>
    /// <param name="formNumber">Form number as a string, e.g. "4" or "67".</param>
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
    /// <summary>
    /// Returns the absolute path to <c>template.docx</c> for the given form.
    /// Throws <see cref="FileNotFoundException"/> when the file does not exist.
    /// </summary>
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

      // ── Top-level scalars ────────────────────────────────────────────────
      var tableIndex = root["tableIndex"]?.GetValue<int>() ?? 0;
      var componentsPerPage = root["componentsPerPage"]?.GetValue<int>()
          ?? throw new InvalidDataException($"componentsPerPage missing in {mapPath}");

      var pageFirstRow = root["pageFirstRow"]?.GetValue<int>() ?? 0;

      // pageLastRow is required — without it we cannot do pagination
      var pageLastRow = root["pageLastRow"]?.GetValue<int>()
          ?? throw new InvalidDataException($"pageLastRow missing in {mapPath}");

      // ── Optional dynamic row ─────────────────────────────────────────────
      int? optionalRowInsertIndex = root["optionalRowInsertIndex"]?.GetValue<int>();
      int? optionalRowTemplateIndex = root["optionalRowTemplateIndex"]?.GetValue<int>();

      // ── Component slots ──────────────────────────────────────────────────
      var slotsNode = root["componentSlots"]?.AsArray()
          ?? throw new InvalidDataException($"componentSlots missing in {mapPath}");

      var slots = slotsNode
          .Select((node, i) => ParseSlot(node, i, mapPath))
          .ToList();

      if (slots.Count != componentsPerPage)
        throw new InvalidDataException(
            $"componentSlots count ({slots.Count}) does not match " +
            $"componentsPerPage ({componentsPerPage}) in {mapPath}");

      // ── Header replacements ───────────────────────────────────────────────
      var headerReplacements = new Dictionary<string, string>();
      if (root["headerReplacements"] is JsonObject hrObj)
      {
        foreach (var (key, value) in hrObj)
        {
          if (value?.GetValue<string>() is string v)
            headerReplacements[key] = v;
        }
      }

      return new WordFormMap
      {
        FormNumber = formNumber,
        TableIndex = tableIndex,
        ComponentsPerPage = componentsPerPage,
        PageFirstRow = pageFirstRow,   // already 0-based from JSON
        PageLastRow = pageLastRow,     // already 0-based from JSON
        ComponentSlots = slots,
        HeaderReplacements = headerReplacements,
        OptionalRowInsertIndex = optionalRowInsertIndex,
        OptionalRowTemplateIndex = optionalRowTemplateIndex,
      };
    }

    private static ComponentSlotMap ParseSlot(JsonNode? node, int slotIndex, string mapPath)
    {
      var obj = node?.AsObject()
          ?? throw new InvalidDataException(
              $"Slot {slotIndex} is not an object in {mapPath}");

      var metaCells = ParseCellDict(obj["metaCells"], slotIndex, "metaCells", mapPath);
      var parameterCells = ParseCellDict(obj["parameterCells"], slotIndex, "parameterCells", mapPath);
      var noteCell = ParseOptionalCoord(obj["noteCell"]);

      return new ComponentSlotMap
      {
        MetaCells = metaCells,
        ParameterCells = parameterCells,
        NoteCell = noteCell,
      };
    }

    private static IReadOnlyDictionary<string, CellCoord> ParseCellDict(
        JsonNode? node, int slotIndex, string fieldName, string mapPath)
    {
      if (node is null) return new Dictionary<string, CellCoord>();

      var obj = node.AsObject();
      var result = new Dictionary<string, CellCoord>();

      foreach (var (key, value) in obj)
      {
        if (key == "comment") continue;          // skip annotation fields
        if (value is null) continue;

        result[key] = ParseCoord(value, slotIndex, key, mapPath);
      }

      return result;
    }

    /// <summary>
    /// Parses a coord node. JSON uses 1-based indices; we convert to 0-based here
    /// so the rest of the codebase never has to think about it.
    /// </summary>
    private static CellCoord ParseCoord(
        JsonNode node, int slotIndex, string key, string mapPath)
    {
      // Each coord is either { "row": N, "col": M } or { "row": N, "col": M, "comment": "..." }
      var obj = node.AsObject();

      var row = obj["row"]?.GetValue<int>()
          ?? throw new InvalidDataException(
              $"Missing 'row' in slot {slotIndex} key '{key}' in {mapPath}");
      var col = obj["col"]?.GetValue<int>()
          ?? throw new InvalidDataException(
              $"Missing 'col' in slot {slotIndex} key '{key}' in {mapPath}");

      // Convert 1-based (JSON/human-friendly) → 0-based (C# arrays)
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
