using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Services;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Word;
using Xunit;

namespace OperationMaps.Tests.Word;

/// <summary>The whole printed set as one document: every part keeps its own page format, frame and numbering.</summary>
public class DocumentMergerTests
{
  private static readonly string Res = Path.Combine(AppContext.BaseDirectory, "Word", "Resources");

  private static async Task<IReadOnlyList<byte[]>> BuildSetAsync()
  {
    var loader = new WordFormMapLoader(Res);
    var form = new WordFormData
    {
      FormNumber = "67",
      Components = Enumerable.Range(0, 4).Select(i => new WordComponentData { Name = $"К10-79-{i}" }).ToList(), // 2 sheets
      FirstSheetNumber = 3,
    };

    return
    [
      await new TitlePageService(Path.Combine(Res, "Title", "template.docx"))
          .BuildAsync(new TitlePageData("АБВГ.123456.001", "Блок питания", "Иванов И.И.", "Директор")),
      await new ContentsPageService(Path.Combine(Res, "Content", "template.docx"))
          .BuildAsync(new ContentsPageData("АБВГ.123456.001", [new ContentsEntry("Карта рабочих режимов конденсаторов", "3–4")], TotalSheets: 5)),
      await new WordService(loader).ExportAsync(new WordFormData
      {
        FormNumber = "4", Components = [new WordComponentData { Name = "Конденсатор", ComponentTypeName = "К10-79" }],
        FirstSheetNumber = 3,
      }, loader.GetTemplatePath("4")),
      await new WordService(loader).ExportAsync(form, loader.GetTemplatePath("67")),
      await new RegistrationSheetService(Path.Combine(Res, "RegistrationChanges", "template.docx"))
          .BuildAsync(new RegistrationSheetData("АБВГ.123456.001", "Блок питания", 5, 5)),
    ];
  }

  [Fact]
  public async Task Merged_set_keeps_each_part_as_its_own_section()
  {
    var parts = await BuildSetAsync();

    var merged = WordDocumentMerger.Merge(parts);

    using var doc = WordprocessingDocument.Open(new MemoryStream(merged), false);
    var main = doc.MainDocumentPart!;
    var sections = WordDocumentAssertions.BodyOf(main).Descendants<SectionProperties>().ToList();

    // One section per part (a form may add its own inside, but never fewer).
    Assert.True(sections.Count >= 4, $"sections={sections.Count}");

    // Sheet numbering restarts per part exactly as planned: title 1, contents 2, form 3, registration 5.
    var starts = sections.Select(s => s.GetFirstChild<PageNumberType>()?.Start?.Value).Where(v => v is not null).ToList();
    Assert.Equal([1, 2, 3, 5], starts.Distinct());

    // Page formats survive: A3 landscape parts and the A4 registration sheet at the end.
    Assert.Equal(11906u, sections[^1].GetFirstChild<PageSize>()!.Width!.Value);   // A4 portrait
    Assert.Equal(23814u, sections[0].GetFirstChild<PageSize>()!.Width!.Value);    // A3 landscape

    // Every header/footer reference points at a part that really exists in the merged file.
    foreach (var reference in sections.SelectMany(s => s.Elements<HeaderReference>().Select(r => r.Id!.Value!)
                 .Concat(s.Elements<FooterReference>().Select(r => r.Id!.Value!))))
      Assert.NotNull(main.GetPartById(reference));

    // The frames came along with their content (designation filled in).
    Assert.Contains(main.HeaderParts.Concat<OpenXmlPart>(main.FooterParts),
        p => p.RootElement!.InnerText.Contains("АБВГ.123456.001Д4"));

    // Text of every part is there.
    var text = WordDocumentAssertions.BodyOf(main).InnerText;
    Assert.Contains("Иванов И.И.", text);
    Assert.Contains("Карта рабочих режимов конденсаторов", text);
    Assert.Contains("К10-79-3", text);
    Assert.Contains("Лист регистрации изменений", text);
  }

  [Fact]
  public async Task Form4_heading_keeps_its_own_style_instead_of_the_title_page_heading()
  {
    var parts = await BuildSetAsync();
    using var form = WordprocessingDocument.Open(new MemoryStream(parts[2]), false);
    using var merged = WordprocessingDocument.Open(new MemoryStream(WordDocumentMerger.Merge(parts)), false);
    var sourceMain = form.MainDocumentPart!;
    var targetMain = merged.MainDocumentPart!;
    var sourceHeading = sourceMain.Document!.Descendants<Paragraph>()
        .First(p => p.InnerText.Contains("ФОРМА 4", StringComparison.OrdinalIgnoreCase));
    var targetHeading = targetMain.Document!.Descendants<Paragraph>()
        .First(p => p.InnerText == sourceHeading.InnerText);
    var sourceId = sourceHeading.ParagraphProperties!.ParagraphStyleId!.Val!.Value;
    var targetId = targetHeading.ParagraphProperties!.ParagraphStyleId!.Val!.Value;
    Assert.NotEqual(sourceId, targetId);
    var originalStyle = sourceMain.StyleDefinitionsPart!.Styles!.Elements<Style>().Single(s => s.StyleId == sourceId);
    var importedStyle = targetMain.StyleDefinitionsPart!.Styles!.Elements<Style>().Single(s => s.StyleId == targetId);
    Assert.Equal(originalStyle.StyleParagraphProperties!.OuterXml, importedStyle.StyleParagraphProperties!.OuterXml);
    Assert.Equal(originalStyle.StyleRunProperties!.OuterXml, importedStyle.StyleRunProperties!.OuterXml);
    var baseStyle = targetMain.StyleDefinitionsPart.Styles.Elements<Style>()
        .Single(s => s.StyleId == importedStyle.BasedOn!.Val);
    Assert.Equal("Times New Roman", baseStyle.StyleRunProperties!.GetFirstChild<RunFonts>()!.Ascii!.Value);
  }

  [Fact]
  public async Task Merged_set_is_a_valid_word_document()
  {
    var merged = WordDocumentMerger.Merge(await BuildSetAsync());

    using var doc = WordprocessingDocument.Open(new MemoryStream(merged), false);
    var errors = new OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Office2019).Validate(doc)
        .Select(e => $"{e.Path?.XPath}: {e.Description}")
        .ToList();
    Assert.True(errors.Count == 0, string.Join("\n", errors));
    var visited = new HashSet<OpenXmlPart>();
    var drawingIds = new List<uint>();
    void Inspect(OpenXmlPartContainer container)
    {
      foreach (var relationship in container.Parts)
      {
        var part = relationship.OpenXmlPart;
        if (!visited.Add(part)) continue;
        if (part.RootElement is { } root)
          drawingIds.AddRange(root.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>().Select(d => d.Id!.Value));
        Inspect(part);
      }
    }
    Inspect(doc);
    Assert.NotEmpty(drawingIds);
    Assert.DoesNotContain(0u, drawingIds);
    Assert.Equal(drawingIds.Count, drawingIds.Distinct().Count());
    var names = doc.MainDocumentPart!.StyleDefinitionsPart!.Styles!.Elements<Style>()
        .Select(s => s.StyleName?.Val?.Value).Where(n => n is not null).ToList();
    Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
  }
}
