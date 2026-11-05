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

  public Task<byte[]> BuildAsync(RegistrationSheetData data, CancellationToken ct = default)
      => Task.Run(() => BuildCoreAsync(data, ct), ct);

  private async Task<byte[]> BuildCoreAsync(RegistrationSheetData data, CancellationToken ct)
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
        ["<DecimalNumber>"] = data.DocumentNumber.Trim(), // the name used in the actual template
        ["<Name>"] = data.Name.Trim(),
        ["<SheetNumber>"] = data.SheetNumber.ToString(),
        ["<SCount>"] = data.TotalSheets.ToString(),
      });

      // The frame's "Лист" is a PAGE field — make it print the set-wide number.
      WordPageNumbering.StartAt(doc, data.SheetNumber);
    }

    ct.ThrowIfCancellationRequested();
    return ms.ToArray();
  }
}
