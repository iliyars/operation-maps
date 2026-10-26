using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace OperationMaps.Infrastructure.Word;

/// <summary>
/// Sheet numbers through the whole printed set (title = 1, contents = 2,
/// forms from 3, the registration sheet last).
/// <para>
/// The frames print their "Лист" number with Word's PAGE field, which counts
/// from 1 in every separate file. Telling each document's sections to start
/// numbering at its first sheet makes that same field print the set-wide
/// number on every sheet — including the 2nd, 3rd… sheet of a multi-sheet
/// form — with no change to the templates.
/// </para>
/// </summary>
public static class WordPageNumbering
{
  public static void StartAt(WordprocessingDocument doc, int firstSheet)
  {
    var body = doc.MainDocumentPart?.Document.Body;
    if (body is null || firstSheet < 1) return;

    foreach (var section in body.Descendants<SectionProperties>())
    {
      var numbering = section.GetFirstChild<PageNumberType>();
      if (numbering is null)
      {
        numbering = new PageNumberType();
        // Schema order inside sectPr: header/footer refs, footnotePr, endnotePr,
        // type, pgSz, pgMar, paperSrc, pgBorders, lnNumType — then pgNumType.
        // Word rejects the file as damaged if it's out of order.
        var anchor = section.Elements().LastOrDefault(e =>
            e is HeaderReference or FooterReference or FootnoteProperties or EndnoteProperties
              or SectionType or PageSize or PageMargin or PaperSource or PageBorders or LineNumberType);
        if (anchor is null) section.PrependChild(numbering);
        else anchor.InsertAfterSelf(numbering);
      }
      numbering.Start = firstSheet;
    }
  }
}
