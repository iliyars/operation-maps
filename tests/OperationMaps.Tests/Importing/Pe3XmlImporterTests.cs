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

    // All components in this fixture have recognizable categories.
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
  public async Task ImportAsync_rejects_an_unrelated_document_instead_of_loading_an_empty_project()
  {
    var importer = CreateImporter();
    // Pure-ASCII content, so plain ASCII bytes are valid cp1251 too — avoids
    // depending on Encoding.RegisterProvider having run before this point.
    var xml = "<?xml version=\"1.0\" encoding=\"windows-1251\"?><AVS></AVS>";
    using var stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(xml));

    var error = await Assert.ThrowsAsync<InvalidDataException>(() => importer.ImportAsync(stream));
    Assert.Contains("RecordsData", error.Message);
  }

  [Theory]
  [InlineData("windows-1251", false)]
  [InlineData("utf-8", false)]
  [InlineData("utf-8", true)]
  [InlineData("utf-16", true)]
  public async Task ImportAsync_honors_declared_encoding_and_bom(string encodingName, bool bom)
  {
    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    var encoding = System.Text.Encoding.GetEncoding(encodingName);
    var xml = $"<?xml version=\"1.0\" encoding=\"{encodingName}\"?><AVS><RecordsData>"
        + "<Record field_5=\"Диод 2Д215Ж ОС\" field_32=\"VD1\"/></RecordsData></AVS>";
    using var stream = new MemoryStream((bom ? encoding.GetPreamble() : []).Concat(encoding.GetBytes(xml)).ToArray());
    var result = await CreateImporter().ImportAsync(stream);
    Assert.Empty(result.Warnings);
    var component = Assert.Single(result.Components);
    Assert.Equal("Диод", component.DetectedCategory);
    Assert.Equal("2Д215Ж ОС", component.RawName);
    Assert.Equal(["VD1"], component.Positions);
    Assert.True(stream.CanRead);
  }

  [Fact]
  public async Task ImportAsync_keeps_unclassified_component_in_original_order()
  {
    using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
        "<AVS><RecordsData><Record field_5=\"UNKNOWN123\" field_32=\"A1\"/>"
        + "<Record field_5=\"Диод 2Д215Ж\" field_32=\"VD1\"/></RecordsData></AVS>"));
    var result = await CreateImporter().ImportAsync(stream);
    Assert.Equal(2, result.ComponentCount);
    Assert.Equal("UNKNOWN123", result.Components[0].RawName);
    Assert.Equal("", result.Components[0].DetectedCategory);
    Assert.Equal(["A1"], result.Components[0].Positions);
    Assert.Equal([0, 1], result.Components.Select(c => c.ImportIndex));
    Assert.Contains(result.Warnings, w => w.Contains("Строка 1") && w.Contains("определить тип"));
  }

  [Fact]
  public async Task ImportAsync_reports_empty_rows_missing_positions_and_duplicate_positions_without_dropping_components()
  {
    using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
        "<AVS><RecordsData><Record field_32=\"R1\"/>"
        + "<Record field_5=\"Диод 2Д215Ж\" field_32=\"VD1, VD1\"/>"
        + "<Record field_5=\"Диод 2Д215Ж\" field_32=\"VD1\"/>"
        + "<Record field_5=\"Диод 2Д215Ж\"/></RecordsData></AVS>"));
    var result = await CreateImporter().ImportAsync(stream);
    Assert.Equal(3, result.ComponentCount);
    Assert.Contains(result.Warnings, w => w.Contains("Строка 1") && w.Contains("пустое наименование"));
    Assert.Contains(result.Warnings, w => w.Contains("Строка 2") && w.Contains("повторяется"));
    Assert.Contains(result.Warnings, w => w.Contains("Строка 3") && w.Contains("повторяется"));
    Assert.Contains(result.Warnings, w => w.Contains("Строка 4") && w.Contains("нет позиционных"));
  }

  [Theory]
  [InlineData("<AVS><RecordsData/><RecordsData/></AVS>")]
  [InlineData("<AVS><RecordsData>")]
  [InlineData("<?xml version=\"1.0\" encoding=\"unknown-encoding\"?><AVS/>")]
  public async Task ImportAsync_rejects_unsupported_structure_or_invalid_xml(string xml)
  {
    using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
    await Assert.ThrowsAsync<InvalidDataException>(() => CreateImporter().ImportAsync(stream));
  }
}
