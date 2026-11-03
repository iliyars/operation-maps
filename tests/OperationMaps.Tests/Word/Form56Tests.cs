using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Word;

namespace OperationMaps.Tests.Word;

public class Form56Tests
{
  [Fact]
  public async Task Export_places_every_parameter_in_its_physical_cell_and_round_trips()
  {
    var loader = new WordFormMapLoader(Path.Combine(AppContext.BaseDirectory, "Word", "Resources"));
    var components = Enumerable.Range(0, 4).Select(i => new WordComponentData
    {
      Name = $"Стабилитрон-{i}", Positions = $"VD{i + 1}", Note = $"Примечание {i}",
      NtdValues = Enumerable.Range(1, 11).ToDictionary(n => n, n => $"НТД-{i}-{n}"),
      SchemeValues = Enumerable.Range(1, 11).ToDictionary(n => n, n => $"СХЕМА-{i}-{n}")
    }).ToArray();
    var service = new WordService(loader);
    var bytes = await service.ExportAsync(new WordFormData
    { FormNumber = "56", DocumentDesignation = "ТЕСТ.123Д4", FirstSheetNumber = 9, Components = components }, loader.GetTemplatePath("56"));
    using (var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false))
    {
      var part = doc.MainDocumentPart!;
      var tables = part.Document.Body!.Elements<Table>().ToArray();
      Assert.Equal(2, tables.Length);
      Assert.Contains("полупроводниковых стабилитронов и стабисторов", tables[0].InnerText);
      Assert.DoesNotContain("варикапов", tables[0].InnerText);
      for (int i = 0; i < 4; i++)
      {
        var table = tables[i / 3];
        var slot = i % 3;
        Assert.Equal(components[i].Positions, WordTableHelper.GetCellText(table, 1, slot + 1));
        Assert.Equal(components[i].Name, WordTableHelper.GetCellText(table, 2, slot + 1));
        Assert.Equal(components[i].Note, WordTableHelper.GetCellText(table, 15, slot + 2));
        for (int n = 1; n <= 11; n++)
        {
          int schemeColumn = (n <= 3 ? 3 : 2) + slot * 2;
          Assert.Equal($"СХЕМА-{i}-{n}", WordTableHelper.GetCellText(table, n + 3, schemeColumn));
          Assert.Equal($"НТД-{i}-{n}", WordTableHelper.GetCellText(table, n + 3, schemeColumn + 1));
        }
      }
      Assert.Equal("1", WordTableHelper.GetCellText(tables[0], 4, 2));
      Assert.Equal("4", WordTableHelper.GetCellText(tables[0], 7, 1));
      Assert.Equal("в схеме", WordTableHelper.GetCellText(tables[0], 3, 1));
      Assert.Equal("по НТД", WordTableHelper.GetCellText(tables[0], 3, 2));
      Assert.Equal(9, part.Document.Body.Elements<SectionProperties>().Single().GetFirstChild<PageNumberType>()!.Start!.Value);
      var header = string.Join("", part.HeaderParts.Select(h => h.Header.InnerText));
      Assert.Contains("ТЕСТ.123Д4", header);
      Assert.DoesNotContain("{{designation}}", header);
    }
    var preview = Path.Combine(AppContext.BaseDirectory, "Form56.preview.docx");
    await File.WriteAllBytesAsync(preview, bytes);
    var read = await service.ImportAsync("56", preview);
    Assert.Equal(4, read.Components.Count);
    for (int i = 0; i < 4; i++)
    {
      Assert.Equal(components[i].Name, read.Components[i].Name);
      Assert.Equal(components[i].Note, read.Components[i].Note);
      foreach (var n in Enumerable.Range(1, 11))
      {
        Assert.Equal(components[i].NtdValues[n], read.Components[i].NtdValues[n]);
        Assert.Equal(components[i].SchemeValues[n], read.Components[i].SchemeValues[n]);
      }
    }
  }
}
