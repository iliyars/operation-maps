using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Services;
using OperationMaps.Infrastructure.Word;
using Xunit;

namespace OperationMaps.Tests.Word;

public class TitlePageTests
{
  private static readonly string TemplatePath =
      Path.Combine(AppContext.BaseDirectory, "Word", "Resources", "Title", "template.docx");

  [Fact]
  public void Placeholder_split_over_runs_is_replaced_keeping_the_surrounding_formatting()
  {
    // As Word stores a typed "<HeadName>Д4": "<" | "HeadName" | ">Д4", with "Д4" bold.
    var bold = new RunProperties(new Bold());
    var paragraph = new Paragraph(
        new Run(new Text("Директор: ")),
        new Run(new Text("<")),
        new Run(new Text("HeadName")),
        new Run((RunProperties)bold.CloneNode(true), new Text(">")),
        new Run((RunProperties)bold.CloneNode(true), new Text("Д4")));

    WordPlaceholderReplacer.ReplaceInParagraph(paragraph, new Dictionary<string, string> { ["<HeadName>"] = "Иванов И.И." });

    Assert.Equal("Директор: Иванов И.И.Д4", paragraph.InnerText);
    var runs = paragraph.Elements<Run>().ToList();
    Assert.Equal(5, runs.Count);                       // nothing merged
    Assert.Equal("Директор: ", runs[0].InnerText);     // untouched
    Assert.Equal("Д4", runs[4].InnerText);             // still its own bold run
    Assert.NotNull(runs[4].RunProperties?.Bold);
  }

  [Fact]
  public void Value_containing_the_placeholder_does_not_loop()
  {
    var paragraph = new Paragraph(new Run(new Text("<Name> и <Name>")));

    WordPlaceholderReplacer.ReplaceInParagraph(paragraph, new Dictionary<string, string> { ["<Name>"] = "<Name>!" });

    Assert.Equal("<Name>! и <Name>!", paragraph.InnerText);
  }

  [Fact]
  public async Task Real_template_has_no_placeholder_left_after_filling()
  {
    var service = new TitlePageService(TemplatePath);

    var bytes = await service.BuildAsync(new TitlePageData(
        "АБВГ.123456.001", "Блок питания БП-12", "Иванов И.И.", "Генеральный директор"));

    using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
    var main = doc.MainDocumentPart!;
    var allText = string.Join("\n",
        new[] { main.Document.InnerText }
            .Concat(main.HeaderParts.Select(h => h.Header.InnerText))
            .Concat(main.FooterParts.Select(f => f.Footer.InnerText)));

    foreach (var tag in new[] { "<DocumentNumber>", "<Name>", "<HeadName>", "<Occupation>" })
      Assert.DoesNotContain(tag, allText);

    Assert.Contains("АБВГ.123456.001Д4", main.Document.InnerText);
    Assert.Contains("Блок питания БП-12", main.Document.InnerText);
    Assert.Contains("Иванов И.И.", main.Document.InnerText);
    Assert.Contains("Генеральный директор", main.Document.InnerText);
    Assert.Contains(main.HeaderParts, h => h.Header.InnerText.Contains("АБВГ.123456.001"));
  }
}
