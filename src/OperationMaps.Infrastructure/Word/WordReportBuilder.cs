using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Application.Services;
using OperationMaps.Application.Word;

namespace OperationMaps.Infrastructure.Word
{
  /// <summary>
  /// Assembles the final report document:
  /// Cover page + all form sections merged into one .docx file.
  /// <para>
  /// Each form is exported independently to a <see cref="MemoryStream"/>,
  /// then its table(s) and paragraphs are appended to the cover document body.
  /// A page-break paragraph is inserted between sections.
  /// </para>
  /// </summary>
  public sealed class WordReportBuilder
  {
    private readonly IWordService _wordService;

    public WordReportBuilder(IWordService wordService)
    {
      _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the complete report document.
    /// </summary>
    /// <param name="coverTemplatePath">
    /// Absolute path to the cover page template.
    /// All form sections are appended after its body content.
    /// </param>
    /// <param name="forms">
    /// Ordered list of (formData, templatePath) pairs.
    /// Forms are appended in the order provided.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Raw bytes of the assembled .docx file.</returns>
    public async Task<byte[]> BuildAsync(
        string coverTemplatePath,
        IReadOnlyList<(WordFormData Data, string TemplatePath)> forms,
        CancellationToken ct = default)
    {
      // ── 1. Load cover into working stream ─────────────────────────────────
      var ms = new MemoryStream();
      await using (var fs = File.OpenRead(coverTemplatePath))
        await fs.CopyToAsync(ms, ct);
      ms.Position = 0;

      using var report = WordprocessingDocument.Open(ms, isEditable: true);
      var reportBody = report.MainDocumentPart!.Document.Body!;

      // ── 2. Export each form and merge into report body ─────────────────────
      foreach (var (data, templatePath) in forms)
      {
        ct.ThrowIfCancellationRequested();

        // Page break before each new form section
        reportBody.AppendChild(MakePageBreak());

        var formBytes = await _wordService.ExportAsync(data, templatePath, ct);
        MergeBodyContent(formBytes, reportBody);
      }

      // ── 3. Save ───────────────────────────────────────────────────────────
      report.MainDocumentPart!.Document.Save();
      ms.Position = 0;
      return ms.ToArray();
    }

    // ── Merge helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Copies all top-level body elements (paragraphs, tables, SDTs) from
    /// <paramref name="sourceBytes"/> into <paramref name="targetBody"/>,
    /// skipping the final empty paragraph that Word always appends.
    /// </summary>
    private static void MergeBodyContent(byte[] sourceBytes, Body targetBody)
    {
      using var srcMs = new MemoryStream(sourceBytes);
      using var srcDoc = WordprocessingDocument.Open(srcMs, isEditable: false);
      var srcBody = srcDoc.MainDocumentPart?.Document?.Body;

      if (srcBody is null) return;

      var elements = srcBody.ChildElements
          .Where(e => e is not SectionProperties)   // skip <sectPr>
          .ToList();

      // Skip the last element if it's a lone empty paragraph (Word trailer)
      if (elements.Count > 0
          && elements[^1] is Paragraph p
          && string.IsNullOrWhiteSpace(p.InnerText))
      {
        elements = elements[..^1];
      }

      foreach (var element in elements)
        targetBody.AppendChild(element.CloneNode(deep: true));
    }

    /// <summary>
    /// Creates a paragraph that forces a page break before the next element.
    /// Uses a run with <see cref="Break"/> of type <see cref="BreakValues.Page"/>
    /// which is the most reliable way to insert a hard page break in OpenXml.
    /// </summary>
    private static Paragraph MakePageBreak()
    {
      return new Paragraph(
          new Run(
              new Break { Type = BreakValues.Page }));
    }
  }
}
