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
  public void Forms_follow_title_and_contents_and_the_registration_sheet_closes_the_set()
  {
    var plan = ContentsPlanner.Plan(
    [
      new PlannedForm("Форма 4", ComponentCount: 5, ComponentsPerPage: 4),   // 2 sheets
      new PlannedForm("Форма 67", ComponentCount: 3, ComponentsPerPage: 3),  // exactly 1
      new PlannedForm("Форма 68", ComponentCount: 0, ComponentsPerPage: 3),  // empty still prints 1
      new PlannedForm("Форма 77", ComponentCount: 7, ComponentsPerPage: 3),  // 3 sheets
    ]);

    Assert.Equal(["3–4", "5", "6", "7–9", "10"], plan.Entries.Select(e => e.Sheets));
    Assert.Equal(ContentsPlanner.RegistrationSheetTitle, plan.Entries[^1].Name);
    Assert.Equal(10, plan.RegistrationSheet);
    Assert.Equal(10, plan.TotalSheets); // title + contents + 7 form sheets + registration
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
        ],
        Name: "Блок питания БП-12",
        Creator: "Иванов",
        Checker: "Петров",
        NormControl: "Сидоров",
        TotalSheets: 6));

    using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
    var main = doc.MainDocumentPart!;
    var rows = main.Document.Body!.Elements<Table>().First().Elements<TableRow>().ToList();
    string[] Cells(TableRow r) => r.Elements<TableCell>().Select(c => c.InnerText.Trim()).ToArray();

    var header = rows.FindIndex(r => r.InnerText.Contains("Наименование документа"));
    Assert.Equal("АБВГ.123456.001Д4", Cells(rows[header + 1])[0]);

    var entryRows = rows.Skip(header + 2).Select(Cells).ToList();
    Assert.Equal(3, entryRows.Count); // the template's sample rows are gone
    Assert.Equal(["Карта рабочих режимов конденсаторов", "4–5"], entryRows[1]);

    var allText = main.Document.InnerText
        + string.Concat(main.HeaderParts.Select(h => h.Header.InnerText))
        + string.Concat(main.FooterParts.Select(f => f.Footer.InnerText));
    foreach (var tag in new[] { "<DocumentNumber>", "<Name>", "<Creator>", "<Checker>", "<NControl>", "<SCount>" })
      Assert.DoesNotContain(tag, allText);
    foreach (var value in new[] { "Блок питания БП-12", "Иванов", "Петров", "Сидоров" })
      Assert.Contains(value, allText);
    Assert.DoesNotContain("электрических соединителей", allText);
    Assert.DoesNotContain("ВЕИР.687282.846", allText);
    Assert.Contains(main.HeaderParts, h => h.Header.InnerText.Contains("АБВГ.123456.001"));
  }

  [Fact]
  public async Task Registration_sheet_fills_its_tags_once_the_template_exists()
  {
    var folder = Path.Combine(Path.GetTempPath(), $"reg-{Guid.NewGuid():N}");
    Directory.CreateDirectory(folder);
    var templatePath = Path.Combine(folder, "template.docx");
    try
    {
      var service = new RegistrationSheetService(templatePath);
      Assert.False(service.TemplateExists);

      using (var doc = WordprocessingDocument.Create(templatePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
      {
        doc.AddMainDocumentPart().Document = new Document(new Body(
            new Paragraph(new Run(new Text("<DocumentNumber>Д4 — <Name>"))),
            new Paragraph(new Run(new Text("Лист <SheetNumber> из <SCount>")))));
      }

      Assert.True(service.TemplateExists);
      var bytes = await service.BuildAsync(new RegistrationSheetData("АБВГ.123456.001", "Блок питания", 10, 10));

      using var result = WordprocessingDocument.Open(new MemoryStream(bytes), false);
      var text = string.Join("|", result.MainDocumentPart!.Document.Body!.Elements<Paragraph>().Select(p => p.InnerText));
      Assert.Equal("АБВГ.123456.001Д4 — Блок питания|Лист 10 из 10", text);
    }
    finally
    {
      Directory.Delete(folder, recursive: true);
    }
  }
}
