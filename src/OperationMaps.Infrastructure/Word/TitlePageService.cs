using DocumentFormat.OpenXml.Packaging;
using OperationMaps.Application.Services;

namespace OperationMaps.Infrastructure.Word;

/// <summary>
/// Fills the title page template. Placeholders are typed straight into the
/// template as <c>&lt;Tag&gt;</c> text — in the body and in the header frame.
/// </summary>
public sealed class TitlePageService : ITitlePageService
{
  private readonly string _templatePath;

  public TitlePageService(string templatePath)
  {
    _templatePath = templatePath ?? throw new ArgumentNullException(nameof(templatePath));
  }

  public async Task<byte[]> BuildAsync(TitlePageData data, CancellationToken ct = default)
  {
    if (!File.Exists(_templatePath))
      throw new FileNotFoundException("Шаблон титульного листа не найден.", _templatePath);

    using var ms = new MemoryStream();
    await using (var fs = File.OpenRead(_templatePath))
      await fs.CopyToAsync(ms, ct);

    // The package writes its changes into the stream only on Dispose — read
    // the bytes after the using block, never inside it.
    using (var doc = WordprocessingDocument.Open(ms, isEditable: true))
    {
      WordPlaceholderReplacer.ReplaceEverywhere(doc, new Dictionary<string, string>
      {
        ["<DocumentNumber>"] = data.DocumentNumber.Trim(),
        ["<Name>"] = data.Name.Trim(),
        ["<HeadName>"] = data.HeadName.Trim(),
        ["<Occupation>"] = data.HeadOccupation.Trim(),
      });
    }

    return ms.ToArray();
  }
}
