using System.Text;
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

  public ImportResult Import(Stream stream)
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    XDocument doc;
    using (var reader = new StreamReader(stream, Encoding.GetEncoding(1251)))
    {
      doc = XDocument.Load(reader);
    }

    var components = new List<ImportedComponent>();
    var warnings = new List<string>();

    // Паспортные данные
    string? docTitle = null, docNumber = null, developedBy = null, checkedBy = null, approvedBy = null;
    var passportData = doc.Descendants("PassportData").Elements("Record").FirstOrDefault();
    if (passportData != null)
    {
      docTitle = (string?)passportData.Attribute("field_2");
      docNumber = (string?)passportData.Attribute("field_3");
      developedBy = (string?)passportData.Attribute("field_16");
      checkedBy = (string?)passportData.Attribute("field_17");
      approvedBy = (string?)passportData.Attribute("field_18");
    }

    var recordsData = doc.Descendants("RecordsData").FirstOrDefault();
    if (recordsData is null)
    {
      warnings.Add("Не найден узел <RecordsData> — файл не похож на перечень ПЭ3.");
      return new ImportResult
      {
        Components = components,
        Warnings = warnings,
        DocumentTitle = docTitle,
        DocumentNumber = docNumber,
        DevelopedBy = developedBy,
        CheckedBy = checkedBy,
        ApprovedBy = approvedBy
      };
    }

    foreach (var rec in recordsData.Elements("Record"))
    {
      // Пропускаем групповые заголовки
      if (string.Equals((string?)rec.Attribute("type_rec"), "X", StringComparison.OrdinalIgnoreCase))
        continue;

      // Пропускаем примечания
      if (string.Equals((string?)rec.Attribute("type_rec"), "R", StringComparison.OrdinalIgnoreCase))
        continue;

      var rawName = ((string?)rec.Attribute(NameField))?.Trim() ?? "";

      if (string.IsNullOrEmpty(rawName))
        continue;

      if (rawName.StartsWith("Примечание", StringComparison.OrdinalIgnoreCase) ||
          rawName.StartsWith("См.", StringComparison.OrdinalIgnoreCase))
        continue;

      // Используем парсер
      ParsedComponentName parsed;
      try
      {
        parsed = _nameParser.Parse(rawName);
      }
      catch (Exception)
      {
        warnings.Add($"Ошибка парсинга: «{rawName}».");
        continue;
      }

      // Если тип не определён — пропускаем
      if (string.IsNullOrEmpty(parsed.Type))
      {
        warnings.Add($"Не удалось определить тип: «{rawName}».");
        continue;
      }

      var rawPos = ((string?)rec.Attribute(PositionField))?.Trim() ?? "";
      var positions = PositionExpander.Expand(rawPos);

      if (positions.Count == 0)
        warnings.Add($"Нет позиционных обозначений: «{rawName}».");

      components.Add(new ImportedComponent
      {
        RawName = parsed.Name,           // чистое имя без типа и ТУ
        DetectedCategory = parsed.Type,  // тип из парсера
        Positions = positions,
        RawPositions = rawPos
      });
    }

    return new ImportResult
    {
      Components = components,
      Warnings = warnings,
      DocumentTitle = docTitle,
      DocumentNumber = docNumber,
      DevelopedBy = developedBy,
      CheckedBy = checkedBy,
      ApprovedBy = approvedBy
    };
  }
}
