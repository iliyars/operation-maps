namespace OperationMaps.Application.Services;

/// <summary>One form as it will be printed: its full title and how many sheets it takes.</summary>
public sealed record PlannedForm(string Title, int ComponentCount, int ComponentsPerPage);

/// <summary>
/// Works out which sheets each form occupies in the printed set:
/// sheet 1 is the title page, sheet 2 the contents, forms follow in order.
/// A form takes as many sheets as its components need (ComponentsPerPage
/// per sheet — the same rule the Word export uses), and at least one.
/// </summary>
public static class ContentsPlanner
{
  public const int FirstFormSheet = 3;

  public static int SheetsFor(PlannedForm form)
      => form.ComponentCount <= 0 || form.ComponentsPerPage <= 0
          ? 1
          : (int)Math.Ceiling((double)form.ComponentCount / form.ComponentsPerPage);

  public static IReadOnlyList<ContentsEntry> Plan(IEnumerable<PlannedForm> forms, int firstSheet = FirstFormSheet)
  {
    var entries = new List<ContentsEntry>();
    var sheet = firstSheet;

    foreach (var form in forms)
    {
      var count = SheetsFor(form);
      var last = sheet + count - 1;
      entries.Add(new ContentsEntry(form.Title, count == 1 ? $"{sheet}" : $"{sheet}–{last}"));
      sheet = last + 1;
    }

    return entries;
  }
}
