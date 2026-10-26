namespace OperationMaps.Application.Services;

/// <summary>What goes onto the change registration sheet (Word/Resources/RegistrationChanges/template.docx).</summary>
public sealed record RegistrationSheetData(
    string DocumentNumber,  // <DocumentNumber> or <DecimalNumber>
    string Name,            // <Name>
    int SheetNumber,        // <SheetNumber> — this sheet, the last of the set
    int TotalSheets);       // <SCount>

public interface IRegistrationSheetService
{
  /// <summary>False until the template has been made — the sheet is then only listed in the contents.</summary>
  bool TemplateExists { get; }

  string TemplatePath { get; }

  Task<byte[]> BuildAsync(RegistrationSheetData data, CancellationToken ct = default);
}
