using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Importing;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Infrastructure.Word;

namespace OperationMaps.Tests.Word;

public class Form3Tests
{
  private static readonly string Resources = Path.Combine(AppContext.BaseDirectory, "Word", "Resources");

  [Fact]
  public async Task Real_xml_boards_fill_separate_name_number_and_quantity_columns()
  {
    var imported = new List<ImportedComponent>();
    foreach (var path in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "TestData", "Xml"), "*.XML"))
    {
      await using var stream = File.OpenRead(path);
      imported.AddRange((await new Pe3XmlImporter(new ComponentNameParser()).ImportAsync(stream)).Components);
    }
    var data = Form3DataBuilder.Build(imported, "ПРОЕКТ.123Д4");
    Assert.Equal(4, data.Components.Count);
    Assert.Equal(new[] { "ВЕИР.687282.840", "ВЕИР.687282.842", "ВЕИР.687282.855", "ВЕИР.687282.856" },
      data.Components.Select(c => c.DecimalNumber).Order());
    Assert.Contains(data.Components, c => c.Name == "Плата модуля питания драйвера OY");
    Assert.All(data.Components, c => Assert.Equal("1", c.Quantity));

    var loader = new WordFormMapLoader(Resources);
    var bytes = await new WordService(loader).ExportAsync(data, loader.GetTemplatePath("3"));
    using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
    var table = Assert.Single(WordDocumentAssertions.BodyOf(doc).Elements<Table>());
    for (int i = 0; i < 4; i++)
    {
      Assert.Equal(data.Components[i].Name, WordTableHelper.GetCellText(table, i + 2, 0));
      Assert.Equal(data.Components[i].DecimalNumber, WordTableHelper.GetCellText(table, i + 2, 1));
      Assert.Equal("1", WordTableHelper.GetCellText(table, i + 2, 2));
    }
    Assert.Empty(WordTableHelper.GetCellText(table, 6, 0));
    Assert.DoesNotContain("Лист регистрации изменений", WordDocumentAssertions.DocumentOf(doc).InnerText);
    var section = Assert.Single(WordDocumentAssertions.BodyOf(doc).Descendants<SectionProperties>());
    Assert.Equal(PageOrientationValues.Landscape, section.GetFirstChild<PageSize>()!.Orient!.Value);
    Assert.NotEmpty(section.Elements<HeaderReference>());
    Assert.NotEmpty(section.Elements<FooterReference>());
    Assert.Contains("ПРОЕКТ.123Д4", string.Join("", Assert.IsType<MainDocumentPart>(doc.MainDocumentPart)
        .HeaderParts.Select(h => Assert.IsType<Header>(h.Header).InnerText)));
  }

  [Fact]
  public async Task Overflow_fills_both_blocks_and_adds_another_sheet_with_numbering()
  {
    var loader = new WordFormMapLoader(Resources);
    var data = new WordFormData
    {
      FormNumber = "3", FirstSheetNumber = 3,
      Components = Enumerable.Range(1, 43).Select(i => new WordComponentData
      { Name = $"Плата {i}", DecimalNumber = $"ВЕИР.123456.{i:D3}", Quantity = "2" }).ToArray()
    };
    var bytes = await new WordService(loader).ExportAsync(data, loader.GetTemplatePath("3"));
    using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
    var tables = WordDocumentAssertions.BodyOf(doc).Elements<Table>().ToArray();
    Assert.Equal(2, tables.Length);
    Assert.Equal("Плата 21", WordTableHelper.GetCellText(tables[0], 22, 0));
    Assert.Equal("Плата 22", WordTableHelper.GetCellText(tables[0], 2, 4));
    Assert.Equal("Плата 42", WordTableHelper.GetCellText(tables[0], 22, 4));
    Assert.Equal("Плата 43", WordTableHelper.GetCellText(tables[1], 2, 0));
    Assert.Empty(WordTableHelper.GetCellText(tables[1], 3, 0));
    Assert.Equal(3, WordDocumentAssertions.BodyOf(doc).Elements<SectionProperties>().Single().GetFirstChild<PageNumberType>()!.Start!.Value);
  }
}
