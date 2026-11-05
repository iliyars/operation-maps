using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Services;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Word;
using Xunit;

namespace OperationMaps.Tests.Word;

/// <summary>Sheet numbers run through the whole set: title 1, contents 2, forms from 3, registration last.</summary>
public class SheetNumberingTests
{
  private static readonly string ResourcesDir = Path.Combine(AppContext.BaseDirectory, "Word", "Resources");

  private static int?[] StartsOf(WordprocessingDocument doc)
      => WordDocumentAssertions.BodyOf(doc).Descendants<SectionProperties>()
          .Select(s => s.GetFirstChild<PageNumberType>()?.Start?.Value)
          .ToArray();

  private static void AssertValid(WordprocessingDocument doc)
  {
    var errors = new OpenXmlValidator().Validate(doc)
        .Where(e => e.Path?.XPath?.Contains("sectPr") == true)
        .Select(e => e.Description)
        .ToList();
    Assert.True(errors.Count == 0, string.Join("\n", errors)); // Word calls a misordered sectPr "damaged"
  }

  [Fact]
  public void Plan_knows_where_each_form_starts()
  {
    var plan = ContentsPlanner.Plan(
    [
      new PlannedForm("Форма 4", 5, 4, Key: "4"),
      new PlannedForm("Форма 67", 7, 3, Key: "67"),
    ]);

    Assert.Equal(3, plan.FirstSheetOf("4"));
    Assert.Equal(5, plan.FirstSheetOf("67"));
    Assert.Equal(8, plan.RegistrationSheet);
    Assert.Null(plan.FirstSheetOf("99"));
  }

  [Fact]
  public async Task Exported_form_starts_its_sheet_numbering_where_the_contents_says()
  {
    var loader = new WordFormMapLoader(ResourcesDir);
    var data = new WordFormData
    {
      FormNumber = "67",
      Components = Enumerable.Range(0, 4).Select(i => new WordComponentData { Name = $"C{i}" }).ToList(), // 2 sheets
      FirstSheetNumber = 5,
    };

    var bytes = await new WordService(loader).ExportAsync(data, loader.GetTemplatePath("67"));

    using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
    Assert.All(StartsOf(doc), start => Assert.Equal(5, start));
    AssertValid(doc);
  }

  [Fact]
  public async Task Stand_alone_export_keeps_default_numbering()
  {
    var loader = new WordFormMapLoader(ResourcesDir);
    var bytes = await new WordService(loader).ExportAsync(new WordFormData { FormNumber = "67" }, loader.GetTemplatePath("67"));

    using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
    Assert.All(StartsOf(doc), start => Assert.Null(start));
  }

  [Fact]
  public async Task Registration_sheet_template_is_one_numbered_sheet_with_its_tags_filled()
  {
    var service = new RegistrationSheetService(Path.Combine(ResourcesDir, "RegistrationChanges", "template.docx"));
    Assert.True(service.TemplateExists);

    var bytes = await service.BuildAsync(new RegistrationSheetData("АБВГ.123456.001", "Блок питания", SheetNumber: 12, TotalSheets: 12));

    using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
    var main = doc.MainDocumentPart!;
    Assert.Single(WordDocumentAssertions.BodyOf(main).Descendants<SectionProperties>()); // the first page is gone
    Assert.Equal([12], StartsOf(doc));
    AssertValid(doc);

    var footers = string.Concat(main.FooterParts.Select(f => Assert.IsType<Footer>(f.Footer).InnerText));
    Assert.Contains("АБВГ.123456.001Д4", footers);
    Assert.DoesNotContain("<DecimalNumber>", footers);
  }

  [Fact]
  public async Task Contents_stamp_shows_only_the_sheet_count_and_is_sheet_2()
  {
    var bytes = await new ContentsPageService(Path.Combine(ResourcesDir, "Content", "template.docx"))
        .BuildAsync(new ContentsPageData("АБВГ.123456.001", [new ContentsEntry("Форма 4", "3")], TotalSheets: 10));

    using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
    Assert.Equal([2], StartsOf(doc));
    AssertValid(doc);

    // The "Листов" cell used to carry a PAGE field after <SCount> (it printed "106"-style numbers).
    var countCells = doc.MainDocumentPart!.FooterParts.Concat<OpenXmlPart>(doc.MainDocumentPart.HeaderParts)
        .SelectMany(p => p.RootElement!.Descendants<Paragraph>())
        .Where(p => p.InnerText.Trim() == "10")
        .ToList();
    Assert.NotEmpty(countCells);
    Assert.All(countCells, p => Assert.Empty(p.Descendants<FieldCode>()));
  }
}
