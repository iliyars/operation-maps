namespace OperationMaps.Application.Services;

/// <summary>One line of the contents: a form's full title and the sheets it occupies ("3" or "4–5").</summary>
public sealed record ContentsEntry(string Name, string Sheets);

/// <summary>What goes into the contents (Word/Resources/Content/template.docx).</summary>
public sealed record ContentsPageData(string DocumentNumber, IReadOnlyList<ContentsEntry> Entries);

public interface IContentsPageService
{
  /// <summary>The contents template filled in, as .docx bytes.</summary>
  Task<byte[]> BuildAsync(ContentsPageData data, CancellationToken ct = default);
}
