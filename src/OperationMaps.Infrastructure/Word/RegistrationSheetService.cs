using DocumentFormat.OpenXml.Packaging;
using OperationMaps.Application.Services;

namespace OperationMaps.Infrastructure.Word;

/// <summary>Fills the change registration sheet ("Лист регистрации изменений") template.</summary>
public sealed class RegistrationSheetService : IRegistrationSheetService
{
  public RegistrationSheetService(string templatePath)
  {
    TemplatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
  }

  public string TemplatePath { get; }

  public bool TemplateExists => File.Exists(TemplatePath);

  public async Task<byte[]> BuildAsync(RegistrationSheetData data, CancellationToken ct = default)
  {
    if (!TemplateExists)
      throw new FileNotFoundException("Шаблон листа регистрации изменений не найден.", TemplatePath);

    using var ms = new MemoryStream();
    await using (var fs = File.OpenRead(TemplatePath))
      await fs.CopyToAsync(ms, ct);

    // The package writes its changes into the stream only on Dispose.
    using (var doc = WordprocessingDocument.Open(ms, isEditable: true))
    {
      WordPlaceholderReplacer.ReplaceEverywhere(doc, new Dictionary<string, string>
      {
        ["<DocumentNumber>"] = data.DocumentNumber.Trim(),
        ["<Name>"] = data.Name.Trim(),
        ["<SheetNumber>"] = data.SheetNumber.ToString(),
        ["<SCount>"] = data.TotalSheets.ToString(),
      });
    }

    return ms.ToArray();
  }
}
