namespace OperationMaps.Application.Services;

/// <summary>What goes onto the title page (Word/Resources/Title/template.docx).</summary>
public sealed record TitlePageData(
    string DocumentNumber,   // <DocumentNumber> — from the XML (field_3), editable
    string Name,             // <Name> — product name from the XML (field_2), editable
    string HeadName,         // <HeadName> — head of the organization, "Фамилия И.О."
    string HeadOccupation);  // <Occupation> — their position

public interface ITitlePageService
{
  /// <summary>The template with every placeholder filled in, as .docx bytes.</summary>
  Task<byte[]> BuildAsync(TitlePageData data, CancellationToken ct = default);
}
