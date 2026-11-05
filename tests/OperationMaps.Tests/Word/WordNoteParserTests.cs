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
    Assert.Equal("40 ***", values[4]);
  }

  [Theory]
  [InlineData("* Первое\n* Другое", "10 *", "несколько раз")]
  [InlineData("* Первое", "10 **", "однозначного")]
  [InlineData("", "10 *", "однозначного")]
  [InlineData("1. Первое", "10 2)", "однозначного")]
  public void Ambiguous_or_unknown_reference_keeps_original_value_and_reports_error(string text, string value, string expected)
  {
    var values = new Dictionary<int, string> { [1] = value };
    var errors = new List<string>();
    Assert.Empty(WordNoteParser.Extract(text, values, errors));
    Assert.Equal(value, values[1]);
    Assert.Contains(errors, e => e.Contains(expected));
  }

  [Fact]
  public void Partial_success_does_not_hide_an_unused_note_or_remove_part_of_a_bad_value()
  {
    var values = new Dictionary<int, string> { [1] = "10 *", [2] = "20 * ***" };
    var errors = new List<string>();
    var notes = WordNoteParser.Extract("* Первое\n** Непривязанное", values, errors);
    Assert.Equal("10", values[1]);
    Assert.Equal("Первое", Assert.Single(notes[1]));
    Assert.Equal("20 * ***", values[2]);
    Assert.False(notes.ContainsKey(2));
    Assert.Contains(errors, e => e.Contains("**") && e.Contains("не связано"));
    Assert.Contains(errors, e => e.Contains("Строка 2") && e.Contains("***"));
  }

  [Theory]
  [InlineData("0,5 (13)")]
  [InlineData("0,5 ( 13)")]
  [InlineData("(-60 … +60)")]
  [InlineData("(-60 … +85)")]
  [InlineData("(допуск (нижний) +5)")]
  public void Parenthesized_numbers_are_not_treated_as_footnotes(string value)
  {
    var values = new Dictionary<int, string> { [1] = value };
    var errors = new List<string>();
    Assert.Empty(WordNoteParser.Extract("", values, errors));
    Assert.Empty(errors);
    Assert.Equal(value, values[1]);
  }

  [Fact]
  public void Parenthesized_star_reference_links_to_the_matching_definition()
  {
    var values = new Dictionary<int, string> { [1] = "150000 *)" };
    var errors = new List<string>();
    var notes = WordNoteParser.Extract("*) Используется в облегчённом режиме", values, errors);
    Assert.Empty(errors);
    Assert.Equal("150000", values[1]);
    Assert.Equal("Используется в облегчённом режиме", Assert.Single(notes[1]));
  }

  [Fact]
  public void Unmarked_note_text_is_reported_instead_of_silently_discarded()
  {
    var values = new Dictionary<int, string> { [1] = "150000" };
    var errors = new List<string>();
    Assert.Empty(WordNoteParser.Extract("Используется в облегчённом режиме", values, errors));
    Assert.Contains(errors, e => e.Contains("без определённого маркера"));
    Assert.Equal("150000", values[1]);
  }
}
