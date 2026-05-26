using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using OperationMaps.Application.Importing;
using SQLitePCL;

namespace OperationMaps.Infrastructure.Importing;

public sealed partial class Pe3XmlImporter : IComponentListImporter
{
  private const string NameField = "field_5";   // Наименование
  private const string PositionField = "filed_32"; // Поз. обозначение

  [GeneratedRegex(@"^([А-ЯЁA-Z][а-яёa-z]+)")]
  private static partial Regex CategoryRegex();

  public bool CanImport(string filePath)
        => filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);

  public ImportResult Import(Stream stream)
  {
    // windows-1251 требует регистрации провайдера кодировок
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    // читаем поток в строку через объявленную в XML кодировку(cp1251)
    XDocument doc;
    using (var reader = new StreamReader(stream, Encoding.GetEncoding(1251)))
    {
      doc = XDocument.Load(reader);
    }

    var components = new List<ImportedComponent>();
    var warnings = new List<string>();

    // === Чтение паспортных данных ===
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
      // служебные записи (примечания) пропускаем
      if (string.Equals((string?)rec.Attribute("type_rec"), "R", StringComparison.OrdinalIgnoreCase))
        continue;

      var rawName = ((string?)rec.Attribute(NameField))?.Trim() ?? "";
      if (rawName.Length == 0 || rawName.StartsWith("Примечание", StringComparison.OrdinalIgnoreCase))
        continue;

      var cleanName = CleanComponentName(rawName);

      var rawPos = ((string?)rec.Attribute(PositionField))?.Trim() ?? "";
      var positions = PositionExpander.Expand(rawPos);

      var catMatch = CategoryRegex().Match(cleanName);
      var category = catMatch.Success ? catMatch.Value : "(не определено)";

      if (positions.Count == 0)
        warnings.Add($"Нет позиционных обозначений: «{rawName}».");

      components.Add(new ImportedComponent
      {
        RawName = cleanName,
        DetectedCategory = category,
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

  private static string CleanComponentName(string rawName)
  {
    if (string.IsNullOrEmpty(rawName)) return rawName;

    // Ищем начало ТУ (обычно в конце): " ОЖ0.467.164ТУ", " ТУ 1234-5678" и т.п.
    // Удаляем всё, начиная с " ТУ", " ОЖ", " П0.", " Р0." и т.д.
    var patternsToRemove = new[]
    {
        @"\s+[А-Я]{2}\d+\.\d+\.\d+[А-Я]{0,2}ТУ", // ОЖ0.467.164ТУ
        @"\s+ТУ\s+\d+[-\d]*",                    // ТУ 1234-5678
        @"\s+П\d+\.\d+\.\d+",                    // П0.070.052
        @"\s+Р\d+\.\d+\.\d+",                    // Р0.123.456
        @",\s*ТУ\s+\S+",                         // ,ТУ 1234
        @"/\s*\S*ТУ\S*"                          // /ОЖ0.467.164ТУ
    };

    string cleaned = rawName;
    foreach (var pattern in patternsToRemove)
    {
      cleaned = Regex.Replace(cleaned, pattern, "", RegexOptions.IgnoreCase);
    }

    // Убираем лишние пробелы и запятые в конце
    cleaned = Regex.Replace(cleaned, @"\s*[,;]\s*$", "");
    cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

    // Если ничего не осталось — вернуть оригинал
    return string.IsNullOrEmpty(cleaned) ? rawName : cleaned;
  }
}
