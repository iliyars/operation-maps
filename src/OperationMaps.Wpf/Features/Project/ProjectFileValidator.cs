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
