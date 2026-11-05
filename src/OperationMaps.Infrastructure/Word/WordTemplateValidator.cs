using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace OperationMaps.Infrastructure.Word;

/// <summary>Validates physical cells using the same indexing as the form writer.</summary>
public static class WordTemplateValidator
{
  public static IReadOnlyList<string> Validate(WordFormMap map, WordprocessingDocument document)
  {
    var errors = new List<string>();
    if (map.ComponentsPerPage < 1 || map.ComponentSlots.Count != map.ComponentsPerPage)
      errors.Add("Число слотов должно совпадать с положительным числом компонентов на странице.");
    Table table;
    try { table = WordTableHelper.GetTable(document, map.TableIndex); }
    catch (InvalidOperationException ex) { errors.Add(ex.Message); return errors; }

    var occupied = new Dictionary<CellCoord, string>();
    void Check(CellCoord cell, string field, Dictionary<CellCoord, string> targets)
    {
      if (WordTableHelper.TryGetCell(table, cell.Row, cell.Col) is null)
        errors.Add($"{field}: ячейка (строка {cell.Row + 1}, столбец {cell.Col + 1}) отсутствует в шаблоне.");
      if (targets.TryGetValue(cell, out var previous))
        errors.Add($"{field} и {previous}: запись в одну ячейку (строка {cell.Row + 1}, столбец {cell.Col + 1}).");
      else targets.Add(cell, field);
    }
    void Parameter(ParameterCoord coordinate, string field, Dictionary<CellCoord, string> targets)
    {
      Check(new(coordinate.Row, coordinate.NtdCol), field + " по НТД", targets);
      if (coordinate.SchemeCol is { } scheme) Check(new(coordinate.Row, scheme), field + " в схеме", targets);
      if (coordinate.PinsCol is { } pins) Check(new(coordinate.Row, pins), field + " выводы", targets);
    }

    foreach (var (name, cell) in map.OperatingConditionCells)
      Check(cell, $"Условия эксплуатации {name}", occupied);
    for (var index = 0; index < map.ComponentSlots.Count; index++)
    {
      var slot = map.ComponentSlots[index];
      var prefix = $"Слот {index + 1}";
      foreach (var (name, cell) in slot.MetaCells) Check(cell, $"{prefix}, {name}", occupied);
      foreach (var (row, coordinate) in slot.ParameterCells) Parameter(coordinate, $"{prefix}, параметр {row}", occupied);
      if (slot.NoteCell is { } note) Check(note, $"{prefix}, примечание", occupied);
      foreach (var label in slot.Labels) Check(label.Cell, $"{prefix}, подпись «{label.Text}»", occupied);
    }

    // Each optional parameter is inserted into a new row. It may reuse the
    // primary row's columns, but its own slots must not overwrite one another.
    foreach (var group in map.OptionalRows.GroupBy(pair => pair.Key.Split(':')[0]))
    {
      var optionalTargets = new Dictionary<CellCoord, string>();
      if (group.Count() != map.ComponentsPerPage || group.Select(p => p.Value.TemplateRowIndex).Distinct().Count() != 1)
        errors.Add($"Дополнительный параметр {group.Key}: слоты должны использовать одну строку-шаблон.");
      foreach (var (key, row) in group)
      {
        var parts = key.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var index) || index < 0 || index >= map.ComponentSlots.Count)
        { errors.Add($"Некорректный слот дополнительного параметра {key}."); continue; }
        if (!map.ComponentSlots[index].ParameterCells.TryGetValue(row.PrimaryRowNumber.ToString(), out var primary)
            || primary.Row != row.TemplateRowIndex || row.Coord.Row != row.TemplateRowIndex)
          errors.Add($"Дополнительный параметр {key}: строка-шаблон не соответствует основному параметру {row.PrimaryRowNumber}.");
        Parameter(row.Coord, $"Дополнительный параметр {key}", optionalTargets);
      }
    }
    return errors;
  }

  public static void ThrowIfInvalid(WordFormMap map, WordprocessingDocument document)
  {
    var errors = Validate(map, document);
    if (errors.Count > 0)
      throw new InvalidDataException($"Форма {map.FormNumber}: карта не соответствует шаблону Word.\n" + string.Join("\n", errors));
  }
}
