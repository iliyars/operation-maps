using System.Text;
using System.Xml;
using System.Xml.Linq;
using OperationMaps.Application.Importing;

namespace OperationMaps.Infrastructure.Importing;

public sealed class Pe3XmlImporter : IComponentListImporter
{
  private const string NameField = "field_5";
  private const string PositionField = "field_32";

  private readonly IComponentNameParser _nameParser;

  public Pe3XmlImporter(IComponentNameParser nameParser)
  {
    _nameParser = nameParser;
  }

  public bool CanImport(string filePath)
      => filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);

  public async Task<ImportResult> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(stream);
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    XDocument doc;
    using (var reader = XmlReader.Create(stream, new XmlReaderSettings
    {
      Async = true,
      CloseInput = false,
      DtdProcessing = DtdProcessing.Prohibit,
      XmlResolver = null,
    }))
    {
      try
      {
        doc = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
      }
      catch (XmlException ex)
      {
        throw new InvalidDataException($"Не удалось прочитать XML: проверьте структуру файла и объявленную кодировку. {ex.Message}", ex);
      }
    }

    var components = new List<ImportedComponent>();
    var warnings = new List<string>();

    // Паспортные данные
    string? docTitle = null, docNumber = null, developedBy = null, checkedBy = null, approvedBy = null;
    var passportData = doc.Descendants("PassportData")
                          .Elements("Record")
                          .FirstOrDefault();
    if (passportData != null)
    {
      docTitle = (string?)passportData.Attribute("field_2");
      docNumber = (string?)passportData.Attribute("field_3");
      developedBy = (string?)passportData.Attribute("field_9");
      checkedBy = (string?)passportData.Attribute("field_16");
      approvedBy = (string?)passportData.Attribute("field_18");
    }

    var sections = doc.Descendants("RecordsData").ToList();
    if (sections.Count != 1)
      throw new InvalidDataException("Ожидается один узел <RecordsData> — структура файла не соответствует перечню ПЭ3.");
    var recordsData = sections[0];

    int importIndex = 0;
    int recordNumber = 0;
    var seenPositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var rec in recordsData.Elements("Record"))
    {
      cancellationToken.ThrowIfCancellationRequested();
      recordNumber++;

      var typeRec = (string?)rec.Attribute("type_rec");

      // Skip section headings and footnotes
      if (string.Equals(typeRec, "X", StringComparison.OrdinalIgnoreCase)) continue;
      if (string.Equals(typeRec, "R", StringComparison.OrdinalIgnoreCase)) continue;

      var rawName = ((string?)rec.Attribute(NameField))?.Trim() ?? "";

      if (string.IsNullOrEmpty(rawName))
      {
        warnings.Add($"Строка {recordNumber}: пустое наименование; строка пропущена (позиции: {(string?)rec.Attribute(PositionField) ?? "не указаны"}).");
        continue;
      }

      var firstWord = rawName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
      var isBoard = string.Equals(firstWord, "Плата", StringComparison.OrdinalIgnoreCase);

      if (rawName.StartsWith("Примечание", StringComparison.OrdinalIgnoreCase) ||
          rawName.StartsWith("См.", StringComparison.OrdinalIgnoreCase))
        continue;

      // Используем парсер
      ParsedComponentName parsed;
      try
      {
        if (isBoard)
        {
          // A board designation can be in field_4 or already part of field_5.
          var name = rawName[firstWord.Length..].Trim();
          var designation = ((string?)rec.Attribute("field_4"))?.Trim();
          if (!string.IsNullOrEmpty(designation) && !name.Contains(designation, StringComparison.Ordinal))
            name = $"{name} {designation}".Trim();
          parsed = new ParsedComponentName
          {
            Raw = rawName,
            Type = "Плата",
            Name = string.IsNullOrEmpty(name) ? rawName : name,
            Family = string.IsNullOrEmpty(name) ? rawName : name
          };
        }
        else
          parsed = _nameParser.Parse(rawName);
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        warnings.Add($"Строка {recordNumber}: ошибка разбора «{rawName}» — {ex.Message}. Исходное наименование сохранено.");
        parsed = new ParsedComponentName { Raw = rawName, Name = rawName, Type = "", Family = "" };
      }

      // Unknown types still belong to the project and can be resolved manually.
      if (string.IsNullOrEmpty(parsed.Type))
      {
        warnings.Add($"Строка {recordNumber}: не удалось определить тип «{rawName}». Компонент сохранён для сопоставления.");
      }

      var rawPos = ((string?)rec.Attribute(PositionField))?.Trim() ?? "";
      var positions = PositionExpander.Expand(rawPos);

      if (positions.Count == 0)
        warnings.Add($"Строка {recordNumber}: нет позиционных обозначений у «{rawName}».");

      foreach (var position in positions)
      {
        if (seenPositions.TryGetValue(position, out var firstRecord))
          warnings.Add($"Строка {recordNumber}: позиция «{position}» повторяется (первое вхождение — строка {firstRecord}). Проверьте перечень.");
        else
          seenPositions.Add(position, recordNumber);
      }

      components.Add(new ImportedComponent
      {
        ImportIndex = importIndex++,
        RawName = string.IsNullOrWhiteSpace(parsed.Type) ? rawName : parsed.Name,
        DetectedCategory = parsed.Type,
        Positions = positions,
        RawPositions = rawPos
      });
    }

    return BuildResult(components, warnings,
                           docTitle, docNumber, developedBy, checkedBy, approvedBy);
  }

  private static ImportResult BuildResult(
        List<ImportedComponent> components,
        List<string> warnings,
        string? docTitle,
        string? docNumber,
        string? developedBy,
        string? checkedBy,
        string? approvedBy) => new()
        {
          Components = components,
          Warnings = warnings,
          DocumentTitle = docTitle,
          DocumentNumber = docNumber,
          DevelopedBy = developedBy,
          CheckedBy = checkedBy,
          ApprovedBy = approvedBy,
        };
}
