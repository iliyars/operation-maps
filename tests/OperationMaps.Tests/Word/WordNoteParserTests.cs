using OperationMaps.Infrastructure.Word;

namespace OperationMaps.Tests.Word;

public class WordNoteParserTests
{
  [Fact]
  public void Cell_reader_keeps_soft_line_breaks_between_note_definitions()
  {
    var cell = new DocumentFormat.OpenXml.Wordprocessing.TableCell(
      new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
        new DocumentFormat.OpenXml.Wordprocessing.Run(
          new DocumentFormat.OpenXml.Wordprocessing.Text("* Первое"),
          new DocumentFormat.OpenXml.Wordprocessing.Break(),
          new DocumentFormat.OpenXml.Wordprocessing.Text("** Второе"))));
    Assert.Equal("* Первое\n** Второе", WordTableHelper.GetCellText(cell));
  }
  [Fact]
  public void Resolves_exact_markers_multiline_notes_and_numbered_definitions()
  {
    var values = new Dictionary<int, string> { [1] = "10 *", [2] = "20 **", [3] = "30 1)", [4] = "40 ***" };
    var notes = WordNoteParser.Extract("* Первое\nпродолжение\n** Второе\n1. Третье", values);
    Assert.Equal("Первое\nпродолжение", Assert.Single(notes[1]));
    Assert.Equal("Второе", Assert.Single(notes[2]));
    Assert.Equal("Третье", Assert.Single(notes[3]));
    Assert.Equal("10", values[1]);
    Assert.Equal("20", values[2]);
    Assert.Equal("30", values[3]);
    Assert.Equal("40", values[4]);
  }
}
