using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace OperationMaps.Wpf.Features.Project;

internal static class ProjectFileValidator
{
  public static void Validate([NotNull] ProjectFile? file)
  {
    if (file is null) Fail("Файл пуст.");
    if (file.Version > ProjectFile.CurrentVersion)
      Fail("Проект сохранён более новой версией программы — обновите OperationMaps.");
    if (file.Version < 1) Fail("Неподдерживаемая версия проекта.");
    if (file.Conditions is null || file.Components is null || file.Form4Notes is null || file.OwnForms is null)
      Fail("Отсутствуют данные проекта.");

    var positions = new HashSet<string>(StringComparer.Ordinal);
    foreach (var component in file.Components)
    {
      if (component is null || string.IsNullOrWhiteSpace(component.RawName)
          || component.DetectedCategory is null || component.Positions is null)
        Fail("Некорректные данные компонента.");
      if (component.Regime is { } regime && (string.IsNullOrWhiteSpace(regime.Name)
          || string.IsNullOrWhiteSpace(regime.Condition) || regime.Values is null
          || regime.Values.Any(v => v is null || string.IsNullOrWhiteSpace(v.FormNumber) || v.RowNumber < 1
            || string.IsNullOrWhiteSpace(v.Value) || v.Value.Contains('*'))
          || regime.Values.Select(v => (v.FormNumber, v.RowNumber)).Distinct().Count() != regime.Values.Count))
        Fail("Некорректный режим компонента.");
      if (component.Regime is { } selection)
      {
        if (selection.Conditions is null || selection.Conditions.Any(c => c is null || c.Conditions is null
          || c.Conditions.Count > 0 || string.IsNullOrWhiteSpace(c.Name) || string.IsNullOrWhiteSpace(c.Condition)
          || c.Values is null || c.Values.Count == 0 || c.Values.Any(v => v is null
            || string.IsNullOrWhiteSpace(v.FormNumber) || v.RowNumber < 1 || string.IsNullOrWhiteSpace(v.Value) || v.Value.Contains('*'))))
          Fail("Некорректные условия компонента.");
        if (selection.Conditions.Count > 0)
        {
          OperationMaps.Application.Models.ComponentRegimeSelection combined;
          try { combined = OperationMaps.Application.Models.ComponentRegimeSelection.Combine(selection.Conditions)!; }
          catch (InvalidOperationException) { Fail("Противоречащие условия компонента."); throw; }
          if (!combined.Values.OrderBy(v => v.FormNumber).ThenBy(v => v.RowNumber)
            .SequenceEqual(selection.Values.OrderBy(v => v.FormNumber).ThenBy(v => v.RowNumber)))
            Fail("Значения компонента не соответствуют выбранным условиям.");
        }
      }
      foreach (var position in component.Positions)
        if (string.IsNullOrWhiteSpace(position) || !positions.Add(position))
          Fail("Пустое или повторяющееся позиционное обозначение компонента.");
    }

    var noteRows = new HashSet<(string, int)>();
    foreach (var note in file.Form4Notes)
    {
      if (note is null || string.IsNullOrWhiteSpace(note.Position) || note.Row < 1
          || note.Notes is null || note.Notes.Any(t => t is null)
          || !noteRows.Add((note.Position, note.Row)))
        Fail("Некорректные примечания к Форме 4.");
    }

    var forms = new HashSet<string>(StringComparer.Ordinal);
    foreach (var form in file.OwnForms)
    {
      if (form is null || string.IsNullOrWhiteSpace(form.FormNumber)
          || !forms.Add(form.FormNumber) || form.Columns is null)
        Fail("Некорректная или повторяющаяся форма.");
      var formPositions = new HashSet<string>(StringComparer.Ordinal);
      foreach (var column in form.Columns)
      {
        if (column is null || column.Positions is null || column.Values is null
            || column.OptionalValues is null || column.Notes is null || column.LoadFactorBaseRow is < 1)
          Fail("Некорректные данные столбца формы.");
        foreach (var position in column.Positions)
          if (string.IsNullOrWhiteSpace(position) || !formPositions.Add(position))
            Fail("Пустое или повторяющееся позиционное обозначение в форме.");
        if (column.Values.Concat(column.OptionalValues).Any(v => v.Key < 1 || v.Value is null)
            || column.Notes.Any(n => n is null || n.Row < 1 || n.Text is null))
          Fail("Некорректные значения или примечания в форме.");
      }
    }
  }

  [DoesNotReturn]
  private static void Fail(string message)
      => throw new InvalidDataException("Файл не является корректным проектом OperationMaps. " + message);
}
