namespace OperationMaps.Application.Services;

/// <summary>One form as it will be printed: its full title and how many sheets it takes.</summary>
public sealed record PlannedForm(string Title, int ComponentCount, int ComponentsPerPage);

/// <summary>
/// The whole printed set: the contents rows (forms, then the change
/// registration sheet), the total sheet count ("Листов" in the stamp) and
/// the sheet the registration sheet lands on.
/// </summary>
public sealed record ContentsPlan(IReadOnlyList<ContentsEntry> Entries, int TotalSheets, int RegistrationSheet)
{
  public static readonly ContentsPlan Empty = new([], 0, 0);
}

/// <summary>
/// Works out which sheets each part of the printed set occupies:
/// sheet 1 is the title page, sheet 2 the contents, forms follow in order,
/// and the change registration sheet ("Лист регистрации изменений") closes
/// the set. A form takes as many sheets as its components need
/// (ComponentsPerPage per sheet — the same rule the Word export uses), and
/// at least one.
/// </summary>
public static class ContentsPlanner
{
  public const int FirstFormSheet = 3;
  public const string RegistrationSheetTitle = "Лист регистрации изменений";

  public static int SheetsFor(PlannedForm form)
      => form.ComponentCount <= 0 || form.ComponentsPerPage <= 0
          ? 1
          : (int)Math.Ceiling((double)form.ComponentCount / form.ComponentsPerPage);

  public static ContentsPlan Plan(IEnumerable<PlannedForm> forms, int firstSheet = FirstFormSheet)
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

    // The registration sheet is always the set's last sheet.
    entries.Add(new ContentsEntry(RegistrationSheetTitle, $"{sheet}"));
    return new ContentsPlan(entries, TotalSheets: sheet, RegistrationSheet: sheet);
  }
}
