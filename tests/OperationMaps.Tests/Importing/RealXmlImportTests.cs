using System.Text;
using System.Xml.Linq;
using OperationMaps.Infrastructure.Importing;
using OperationMaps.Infrastructure.Services;

namespace OperationMaps.Tests.Importing;

public class RealXmlImportTests
{
  [Theory]
  [InlineData("ВЕИР.436634.710 ПЭ4.PE.XML", 6, 7)]
  [InlineData("ВЕИР.436634.712 ПЭ4.PE.XML", 3, 5)]
  [InlineData("ВЕИР.468364.741 ПЭ3.PE.XML", 157, 456)]
  [InlineData("ВЕИР.468364.742 ПЭ3.PE.XML", 157, 456)]
  [InlineData("ВЕИР.687282.840 ПЭ3.PE.XML", 52, 137)]
  [InlineData("ВЕИР.687282.842 ПЭ3.PE.XML", 23, 69)]
  [InlineData("ВЕИР.687282.845 ПЭ3.PE.XML", 63, 371)]
  [InlineData("ВЕИР.687282.846 ПЭ3.PE.XML", 1, 7)]
  public async Task Import_preserves_source_rows_quantities_and_passport(string filename, int rows, int positions)
  {
    var path = Path.Combine(AppContext.BaseDirectory, "TestData", "Xml", filename);
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    var source = XDocument.Parse(await File.ReadAllTextAsync(path, Encoding.GetEncoding(1251)));
    var records = source.Descendants("RecordsData").Elements("Record")
      .Where(r => (string?)r.Attribute("type_rec") is not ("X" or "R"))
      .Where(r => !string.IsNullOrWhiteSpace((string?)r.Attribute("field_5")))
      .ToArray();
    await using var stream = File.OpenRead(path);
    var result = await new Pe3XmlImporter(new ComponentNameParser()).ImportAsync(stream);

    Assert.Empty(result.Warnings);
    Assert.Equal(rows, result.ComponentCount);
    Assert.Equal(positions, result.PositionCount);
    Assert.Equal(records.Length, result.ComponentCount);
    Assert.Equal(Enumerable.Range(0, rows), result.Components.Select(c => c.ImportIndex));
    for (int i = 0; i < records.Length; i++)
    {
      var record = records[i];
      var component = result.Components[i];
      Assert.Equal(((string?)record.Attribute("field_32"))?.Trim(), component.RawPositions);
      var quantity = (int?)record.Attribute("field_6") ?? 1;
      Assert.True(quantity == component.Quantity,
        $"{filename}: {component.RawPositions}: expected {quantity}, got {component.Quantity}");
      Assert.NotEmpty(component.RawName);
      Assert.NotEmpty(component.DetectedCategory);
      if (((string)record.Attribute("field_5")!).StartsWith("Плата", StringComparison.OrdinalIgnoreCase))
      {
        Assert.Equal("Плата", component.DetectedCategory);
        var designation = (string?)record.Attribute("field_4");
        if (!string.IsNullOrEmpty(designation))
          Assert.Contains(designation, component.RawName);
      }
      Assert.All(component.Positions, p => Assert.Matches(@"^[A-Za-zА-Яа-яЁё]+\d+$", p));
    }
    var allPositions = result.Components.SelectMany(c => c.Positions).ToArray();
    Assert.Equal(allPositions.Length, allPositions.Distinct().Count());
    var passport = source.Descendants("PassportData").Elements("Record").Single();
    Assert.Equal((string?)passport.Attribute("field_2"), result.DocumentTitle);
    Assert.Equal((string?)passport.Attribute("field_9"), result.DevelopedBy);
    Assert.Equal((string?)passport.Attribute("field_16"), result.CheckedBy);
    Assert.Equal((string?)passport.Attribute("field_18"), result.ApprovedBy);
  }
}
