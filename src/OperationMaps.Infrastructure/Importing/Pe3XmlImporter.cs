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

  public async Task<ImportResult> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(stream);
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    XDocument doc;
    using (var reader = new StreamReader(stream, Encoding.GetEncoding(1251),
                                        detectEncodingFromByteOrderMarks: false,
                                        leaveOpen: true))
    {
      doc = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
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
      developedBy = (string?)passportData.Attribute("field_16");
      checkedBy = (string?)passportData.Attribute("field_17");
      approvedBy = (string?)passportData.Attribute("field_18");
    }

    var recordsData = doc.Descendants("RecordsData").FirstOrDefault();
    if (recordsData is null)
    {
      warnings.Add("Не найден узел <RecordsData> — файл не похож на перечень ПЭ3.");
      return BuildResult(components, warnings,
                           docTitle, docNumber, developedBy, checkedBy, approvedBy);
    }

    int importIndex = 0;
    foreach (var rec in recordsData.Elements("Record"))
    {
      cancellationToken.ThrowIfCancellationRequested();

      var typeRec = (string?)rec.Attribute("type_rec");

      // Skip section headings and footnotes
      if (string.Equals(typeRec, "X", StringComparison.OrdinalIgnoreCase)) continue;
      if (string.Equals(typeRec, "R", StringComparison.OrdinalIgnoreCase)) continue;

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
      catch (Exception ex)
      {
        warnings.Add($"Ошибка парсинга: «{rawName}» - {ex.Message}.");
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
        ImportIndex = importIndex++,
        RawName = parsed.Name,
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
