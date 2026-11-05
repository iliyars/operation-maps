using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Word;

namespace OperationMaps.Tests.Word;

public sealed class WordTemplateValidatorTests
{
  public static IEnumerable<object[]> RealForms => Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "Word", "Resources"), "Form*")
      .Where(path => File.Exists(Path.Combine(path, "map.json")) && File.Exists(Path.Combine(path, "template.docx")))
      .Select(path => new object[] { Path.GetFileName(path)[4..] });

  [Theory]
  [MemberData(nameof(RealForms))]
  public void Real_maps_target_existing_nonoverlapping_physical_cells(string number)
  {
    var loader = new WordFormMapLoader(Path.Combine(AppContext.BaseDirectory, "Word", "Resources"));
    loader.ValidateTemplate(number);
  }

  private static WordFormMap Map(ParameterCoord? parameter = null, CellCoord? note = null,
      IReadOnlyDictionary<string, OptionalRowMap>? optional = null, int table = 0) => new()
  {
    FormNumber = "test", TableIndex = table, ComponentsPerPage = 1,
    ComponentSlots = [new ComponentSlotMap
    {
      MetaCells = new Dictionary<string, CellCoord> { [MetaCellKey.ComponentName] = new(0, 0) },
      ParameterCells = new Dictionary<string, ParameterCoord> { ["1"] = parameter ?? new(1, 0, 1, 2) },
      NoteCell = note ?? new(2, 0),
    }],
    OptionalRows = optional ?? new Dictionary<string, OptionalRowMap>(),
  };

  private static MemoryStream Template()
  {
    var stream = new MemoryStream();
    using (var document = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
    {
      var part = document.AddMainDocumentPart();
      // GridSpan does not create an extra physical cell.
      part.Document = new Document(new Body(new Table(Enumerable.Range(0, 3).Select(_ => new TableRow(
        new TableCell(new TableCellProperties(new GridSpan { Val = 2 }), new Paragraph()),
        new TableCell(new Paragraph()), new TableCell(new Paragraph()))))));
      part.Document.Save();
    }
    stream.Position = 0;
    return stream;
  }

  [Theory]
  [InlineData(-1, 0)]
  [InlineData(3, 0)]
  [InlineData(1, 3)]
  public void Missing_physical_coordinates_are_reported_even_when_grid_span_could_look_valid(int row, int column)
  {
    using var stream = Template();
    using var doc = WordprocessingDocument.Open(stream, false);
    var errors = WordTemplateValidator.Validate(Map(parameter: new(row, column, null)), doc);
    Assert.Contains(errors, e => e.Contains("Слот 1") && e.Contains("отсутствует"));
  }

  [Fact]
  public void Parameter_columns_and_notes_cannot_overwrite_each_other()
  {
    using var stream = Template();
    using var doc = WordprocessingDocument.Open(stream, false);
    var errors = WordTemplateValidator.Validate(Map(parameter: new(1, 0, 0), note: new(1, 0)), doc);
    Assert.Contains(errors, e => e.Contains("в схеме") && e.Contains("по НТД") && e.Contains("одну ячейку"));
    Assert.Contains(errors, e => e.Contains("примечание") && e.Contains("одну ячейку"));
  }

  [Fact]
  public void Separate_component_slots_cannot_share_output_cells()
  {
    var slot = Map().ComponentSlots[0];
    var map = new WordFormMap { FormNumber = "test", ComponentsPerPage = 2, ComponentSlots = [slot, slot] };
    using var stream = Template();
    using var doc = WordprocessingDocument.Open(stream, false);
    Assert.Contains(WordTemplateValidator.Validate(map, doc), e => e.Contains("Слот 1") && e.Contains("Слот 2") && e.Contains("одну ячейку"));
  }

  [Fact]
  public void Optional_row_may_reuse_primary_columns_but_must_clone_the_primary_row()
  {
    using var stream = Template();
    using var doc = WordprocessingDocument.Open(stream, false);
    var optional = new Dictionary<string, OptionalRowMap>
    {
      ["2:0"] = new() { PrimaryRowNumber = 1, TemplateRowIndex = 1, Coord = new(1, 0, 1, 2) },
    };
    Assert.Empty(WordTemplateValidator.Validate(Map(optional: optional), doc));
    optional["2:0"] = new() { PrimaryRowNumber = 1, TemplateRowIndex = 2, Coord = new(2, 0, 1, 2) };
    Assert.Contains(WordTemplateValidator.Validate(Map(optional: optional), doc), e => e.Contains("не соответствует основному параметру"));
  }

  [Fact]
  public void Missing_table_is_reported_as_a_template_error()
  {
    using var stream = Template();
    using var doc = WordprocessingDocument.Open(stream, false);
    Assert.NotEmpty(WordTemplateValidator.Validate(Map(table: 1), doc));
  }

  [Fact]
  public void Writer_rejects_bad_map_before_filling_and_does_not_modify_template()
  {
    using var stream = Template();
    var path = Path.Combine(Path.GetTempPath(), $"template-check-{Guid.NewGuid():N}.docx");
    var bytes = stream.ToArray();
    try
    {
      File.WriteAllBytes(path, bytes);
      var error = Assert.Throws<InvalidDataException>(() => WordFormWriter.Fill(
          new WordFormData { FormNumber = "test" }, Map(parameter: new(999, 0, null)), path));
      Assert.Contains("параметр 1", error.Message);
      Assert.Equal(bytes, File.ReadAllBytes(path));
    }
    finally { File.Delete(path); }
  }
}
