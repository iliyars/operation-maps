using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace OperationMaps.Infrastructure.Word;

internal static class WordDocumentStructure
{
  public static Document RequireDocument(WordprocessingDocument document)
    => document.MainDocumentPart?.Document
      ?? throw new InvalidDataException("Документ Word не содержит основной части документа.");

  public static Body RequireBody(WordprocessingDocument document)
    => RequireBody(RequireDocument(document));

  public static Body RequireBody(MainDocumentPart part)
    => RequireBody(part.Document
      ?? throw new InvalidDataException("Документ Word не содержит основной части документа."));

  private static Body RequireBody(Document document)
    => document.Body ?? throw new InvalidDataException("Документ Word не содержит тела документа.");
}
