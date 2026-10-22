using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Services;
using OperationMaps.Infrastructure.Word;
using Xunit;

namespace OperationMaps.Tests.Word;

public class ContentsPageTests
{
  private static readonly string TemplatePath =
      Path.Combine(AppContext.BaseDirectory, "Word", "Resources", "Content", "template.docx");

  [Fact]
  public void Forms_follow_title_and_contents_with_sheet_ranges()
  {
    var entries = ContentsPlanner.Plan(
    [
      new PlannedForm("Форма 4", ComponentCount: 5, ComponentsPerPage: 4),   // 2 sheets
      new PlannedForm("Форма 67", ComponentCount: 3, ComponentsPerPage: 3),  // exactly 1
      new PlannedForm("Форма 68", ComponentCount: 0, ComponentsPerPage: 3),  // empty still prints 1
      new PlannedForm("Форма 77", ComponentCount: 7, ComponentsPerPage: 3),  // 3 sheets
    ]);

    Assert.Equal(["3–4", "5", "6", "7–9"], entries.Select(e => e.Sheets));
  }

  [Fact]
  public async Task Real_template_gets_designation_and_one_row_per_form()
  {
    var bytes = await new ContentsPageService(TemplatePath).BuildAsync(new ContentsPageData(
        "АБВГ.123456.001",
        [
          new ContentsEntry("Карта оценки номенклатуры ЭРИ", "3"),
          new ContentsEntry("Карта рабочих режимов конденсаторов", "4–5"),
          new ContentsEntry("Карта рабочих режимов резисторов", "6"),
        ]));

    using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
    var main = doc.MainDocumentPart!;
    var rows = main.Document.Body!.Elements<Table>().First().Elements<TableRow>().ToList();
    string[] Cells(TableRow r) => r.Elements<TableCell>().Select(c => c.InnerText.Trim()).ToArray();

    var header = rows.FindIndex(r => r.InnerText.Contains("Наименование документа"));
    Assert.Equal("АБВГ.123456.001Д4", Cells(rows[header + 1])[0]);

    var entryRows = rows.Skip(header + 2).Select(Cells).ToList();
    Assert.Equal(3, entryRows.Count); // the template's sample rows are gone
    Assert.Equal(["Карта рабочих режимов конденсаторов", "4–5"], entryRows[1]);

    var allText = main.Document.InnerText + string.Concat(main.HeaderParts.Select(h => h.Header.InnerText));
    Assert.DoesNotContain("<DocumentNumber>", allText);
    Assert.DoesNotContain("электрических соединителей", allText);
    Assert.DoesNotContain("ВЕИР.687282.846", allText);
    Assert.Contains(main.HeaderParts, h => h.Header.InnerText.Contains("АБВГ.123456.001"));
  }
}
