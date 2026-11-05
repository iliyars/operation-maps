using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Word;

namespace OperationMaps.Tests.Word;

public class EmptyComponentSlotTests
{
  [Theory]
  [InlineData("3")]
  [InlineData("4")]
  [InlineData("55")]
  [InlineData("56")]
  [InlineData("58")]
  [InlineData("60")]
  [InlineData("64")]
  [InlineData("67")]
  [InlineData("68")]
  [InlineData("69")]
  [InlineData("75")]
  [InlineData("77")]
  [InlineData("83")]
  [InlineData("86")]
  public async Task Unused_slots_are_empty_on_every_page_and_only_occupied_slots_get_labels(string number)
  {
    var loader = new WordFormMapLoader(Path.Combine(AppContext.BaseDirectory, "Word", "Resources"));
    var map = loader.Load(number);
    var template = loader.GetTemplatePath(number);
    using (var doc = WordprocessingDocument.Open(template, false))
    {
      var table = WordTableHelper.GetTable(doc, map.TableIndex);
      foreach (var slot in map.ComponentSlots)
        foreach (var cell in SlotCells(slot))
          Assert.True(string.IsNullOrWhiteSpace(WordTableHelper.GetCellText(table, cell.Row, cell.Col)), $"Template {number}: {cell}");
    }
    foreach (int count in new[] { 0, 1, map.ComponentsPerPage - 1, map.ComponentsPerPage + 1 }.Distinct())
    {
      var components = Enumerable.Range(0, count).Select(i => new WordComponentData
      {
        Name = $"Part {i}", Positions = $"X{i}", Quantity = "1", DecimalNumber = $"TEST.123.{i}",
        NtdValues = Enumerable.Range(1, 30).ToDictionary(n => n, n => $"N{i}-{n}"),
        SchemeValues = Enumerable.Range(1, 30).ToDictionary(n => n, n => $"S{i}-{n}")
      }).ToArray();
      var bytes = await new WordService(loader).ExportAsync(new WordFormData { FormNumber = number, Components = components }, template);
      using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
      var tables = WordDocumentAssertions.BodyOf(doc).Elements<Table>().ToArray();
      Assert.Equal(Math.Max(1, (count + map.ComponentsPerPage - 1) / map.ComponentsPerPage), tables.Length);
      for (int page = 0; page < tables.Length; page++)
        for (int index = 0; index < map.ComponentSlots.Count; index++)
        {
          var slot = map.ComponentSlots[index];
          var occupied = page * map.ComponentsPerPage + index < count;
          if (occupied)
          {
            foreach (var label in slot.Labels)
              Assert.Equal(label.Text, WordTableHelper.GetCellText(tables[page], label.Cell.Row, label.Cell.Col));
          }
          else
            foreach (var cell in SlotCells(slot))
              Assert.True(string.IsNullOrWhiteSpace(WordTableHelper.GetCellText(tables[page], cell.Row, cell.Col)),
                $"Form {number}, count {count}, page {page}, slot {index}: {cell}");
        }
    }
  }

  private static IEnumerable<CellCoord> SlotCells(ComponentSlotMap slot)
  {
    foreach (var cell in slot.MetaCells.Values) yield return cell;
    foreach (var label in slot.Labels) yield return label.Cell;
    if (slot.NoteCell is { } note) yield return note;
    foreach (var parameter in slot.ParameterCells.Values)
    {
      yield return new(parameter.Row, parameter.NtdCol);
      if (parameter.SchemeCol is { } scheme) yield return new(parameter.Row, scheme);
      if (parameter.PinsCol is { } pins) yield return new(parameter.Row, pins);
    }
  }
}
