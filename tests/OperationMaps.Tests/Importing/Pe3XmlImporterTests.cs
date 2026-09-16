using OperationMaps.Infrastructure.Importing;
using OperationMaps.Infrastructure.Services;
using Xunit;

namespace OperationMaps.Tests.Importing;

public class Pe3XmlImporterTests
{
  private static Pe3XmlImporter CreateImporter() => new(new ComponentNameParser());

  [Fact]
  public void CanImport_accepts_only_xml_files()
  {
    var importer = CreateImporter();

    Assert.True(importer.CanImport(@"C:\data\sample.xml"));
    Assert.True(importer.CanImport(@"C:\data\sample.XML"));
    Assert.False(importer.CanImport(@"C:\data\sample.txt"));
  }

  [Fact]
  public async Task ImportAsync_parses_real_pe3_export()
  {
    var importer = CreateImporter();
    var path = Path.Combine(AppContext.BaseDirectory, "TestData", "sample_pe3_1.XML");

    await using var stream = File.OpenRead(path);
    var result = await importer.ImportAsync(stream);

    // 40 <Record> entries in RecordsData, one of which is a type_rec="R"
    // footnote (a note attached to designator "|", not a real component).
    Assert.Equal(39, result.Components.Count);
    Assert.False(string.IsNullOrWhiteSpace(result.DocumentNumber));

    // Import order must be preserved — the UI relies on ImportIndex to show
    // components in the same order as the source document.
    Assert.Equal(Enumerable.Range(0, 39), result.Components.Select(c => c.ImportIndex));

    // Every parsed component must have a detected category — the importer
    // is expected to skip/warn on anything the name parser can't classify
    // rather than let it through with an empty category.
    Assert.All(result.Components, c => Assert.False(string.IsNullOrEmpty(c.DetectedCategory)));

    // Range expansion ("C26 - C54") must flow through end-to-end.
    var wideRangeComponent = result.Components.Single(c => c.RawPositions == "C26 - C54");
    Assert.Equal(29, wideRangeComponent.Positions.Count);
    Assert.Equal("C26", wideRangeComponent.Positions[0]);
    Assert.Equal("C54", wideRangeComponent.Positions[^1]);

    // Double-comma-space edge case ("R18 , R19") must also expand correctly.
    var spacedComponent = result.Components.Single(c => c.RawPositions == "R18 , R19");
    Assert.Equal(new[] { "R18", "R19" }, spacedComponent.Positions);
  }

  [Fact]
  public async Task ImportAsync_warns_when_records_data_is_missing()
  {
    var importer = CreateImporter();
    // Pure-ASCII content, so plain ASCII bytes are valid cp1251 too — avoids
    // depending on Encoding.RegisterProvider having run before this point.
    var xml = "<?xml version=\"1.0\" encoding=\"windows-1251\"?><AVS></AVS>";
    using var stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(xml));

    var result = await importer.ImportAsync(stream);

    Assert.Empty(result.Components);
    Assert.Contains(result.Warnings, w => w.Contains("RecordsData"));
  }
}
