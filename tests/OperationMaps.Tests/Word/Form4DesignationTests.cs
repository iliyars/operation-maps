using DocumentFormat.OpenXml.Packaging;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Word;

namespace OperationMaps.Tests.Word;

public sealed class Form4DesignationTests
{
  [Theory]
  [InlineData("АБВГ.123456.001", "АБВГ.123456.001Д4")]
  [InlineData("АБВГ.123456.001Д4", "АБВГ.123456.001Д4")]
  [InlineData(" АБВГ.123456.001 ", "АБВГ.123456.001Д4")]
  public async Task Form4_header_uses_document_designation_with_one_D4_suffix(string designation, string expected)
  {
    var loader = new WordFormMapLoader(Path.Combine(AppContext.BaseDirectory, "Word", "Resources"));
    var bytes = await new WordService(loader).ExportAsync(new WordFormData
    {
      FormNumber = "4", DocumentDesignation = designation,
      Components = [new WordComponentData { Name = "Компонент" }],
    }, loader.GetTemplatePath("4"));
    using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
    var text = string.Join("", doc.MainDocumentPart!.HeaderParts.Concat<OpenXmlPart>(doc.MainDocumentPart.FooterParts)
        .Select(p => p.RootElement!.InnerText));
    Assert.Contains(expected, text);
    Assert.DoesNotContain(expected + "Д4", text);
    Assert.DoesNotContain("{{designation}}", text);
  }
}
