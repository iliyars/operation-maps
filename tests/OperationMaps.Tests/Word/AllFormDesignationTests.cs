using System.IO.Compression;
using System.Xml.Linq;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Word;

namespace OperationMaps.Tests.Word;

public sealed class AllFormDesignationTests
{
  [Theory]
  [MemberData(nameof(WordTemplateValidatorTests.RealForms), MemberType = typeof(WordTemplateValidatorTests))]
  public async Task Every_header_and_footer_replaces_split_designations_with_D4(string number)
  {
    var loader = new WordFormMapLoader(Path.Combine(AppContext.BaseDirectory, "Word", "Resources"));
    var bytes = await new WordService(loader).ExportAsync(new WordFormData
    {
      FormNumber = number, DocumentDesignation = "ТЕСТ.123&456",
      Components = [new WordComponentData { Name = "Компонент" }],
    }, loader.GetTemplatePath(number));
    using var zip = new ZipArchive(new MemoryStream(bytes));
    var headers = zip.Entries.Where(e => (e.FullName.StartsWith("word/header") || e.FullName.StartsWith("word/footer"))
        && e.FullName.EndsWith(".xml")).ToList();
    Assert.NotEmpty(headers);
    var allText = new List<string>();
    foreach (var entry in headers)
    {
      using var stream = entry.Open();
      var xml = XDocument.Load(stream);
      var text = string.Concat(xml.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value));
      Assert.DoesNotContain("{{designation}}", text);
      Assert.DoesNotContain("Д4Д4", text);
      allText.Add(text);
    }
    Assert.Contains("ТЕСТ.123&456Д4", string.Concat(allText));
  }
}
