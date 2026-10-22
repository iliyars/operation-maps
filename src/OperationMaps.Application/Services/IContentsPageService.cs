namespace OperationMaps.Application.Services;

/// <summary>One line of the contents: a form's full title and the sheets it occupies ("3" or "4–5").</summary>
public sealed record ContentsEntry(string Name, string Sheets);

/// <summary>
/// What goes into the contents (Word/Resources/Content/template.docx): the
/// table, and the main stamp of the set — designation and product name from
/// the XML, the people typed in by the user, the total sheet count.
/// </summary>
public sealed record ContentsPageData(
    string DocumentNumber,               // <DocumentNumber>
    IReadOnlyList<ContentsEntry> Entries,
    string Name = "",                    // <Name>
    string Creator = "",                 // <Creator>  — Разраб.
    string Checker = "",                 // <Checker>  — Пров.
    string NormControl = "",             // <NControl> — Н. контр.
    int TotalSheets = 0);                // <SCount>   — Листов

public interface IContentsPageService
{
  /// <summary>The contents template filled in, as .docx bytes.</summary>
  Task<byte[]> BuildAsync(ContentsPageData data, CancellationToken ct = default);
}
