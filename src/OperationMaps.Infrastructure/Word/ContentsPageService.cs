using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Services;

namespace OperationMaps.Infrastructure.Word;

/// <summary>
/// Fills the contents ("Содержание", Form 2А) template.
/// <para>
/// The template's table holds sample rows: header rows up to "Наименование
/// документа | лист", then the document designation row, then sample
/// entries. The designation row gets the real number; the sample entries are
/// replaced by one row per form, each a copy of the first sample entry so it
/// keeps the template's fonts and alignment.
/// </para>
/// </summary>
public sealed class ContentsPageService : IContentsPageService
{
  private const string HeaderMarker = "Наименование документа";

  /// <summary>Document code appended to the designation, as on the title page frame ("…Д4").</summary>
  public const string DocumentCode = "Д4";

  private readonly string _templatePath;

  public ContentsPageService(string templatePath)
  {
    _templatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
  }

  public async Task<byte[]> BuildAsync(ContentsPageData data, CancellationToken ct = default)
  {
    if (!File.Exists(_templatePath))
      throw new FileNotFoundException("Шаблон содержания не найден.", _templatePath);

    using var ms = new MemoryStream();
    await using (var fs = File.OpenRead(_templatePath))
      await fs.CopyToAsync(ms, ct);

    // The package writes its changes into the stream only on Dispose.
    using (var doc = WordprocessingDocument.Open(ms, isEditable: true))
    {
      var number = data.DocumentNumber.Trim();

      WordPlaceholderReplacer.ReplaceEverywhere(doc, new Dictionary<string, string>
      {
        ["<DocumentNumber>"] = number,
      });

      FillTable(doc.MainDocumentPart!.Document.Body!.Elements<Table>().First(), number, data.Entries);
      doc.MainDocumentPart.Document.Save();
    }

    return ms.ToArray();
  }

  private static void FillTable(Table table, string documentNumber, IReadOnlyList<ContentsEntry> entries)
  {
    var rows = table.Elements<TableRow>().ToList();

    var headerIndex = rows.FindIndex(r => r.InnerText.Contains(HeaderMarker, StringComparison.OrdinalIgnoreCase));
    if (headerIndex < 0 || headerIndex + 2 >= rows.Count)
      throw new InvalidDataException(
          $"В шаблоне содержания не найдена таблица вида «{HeaderMarker} | лист» с примером строк.");

    var designationRow = rows[headerIndex + 1];
    var entryTemplate = (TableRow)rows[headerIndex + 2].CloneNode(deep: true);

    WordTableHelper.SetCellText(designationRow.Elements<TableCell>().First(), documentNumber + DocumentCode);

    // Drop the template's sample entries…
    foreach (var row in rows.Skip(headerIndex + 2))
      row.Remove();

    // …and add the real ones in their place.
    foreach (var entry in entries)
    {
      var row = (TableRow)entryTemplate.CloneNode(deep: true);
      var cells = row.Elements<TableCell>().ToList();
      WordTableHelper.SetCellText(cells[0], entry.Name);
      WordTableHelper.SetCellText(cells[^1], entry.Sheets);
      table.AppendChild(row);
    }
  }
}
