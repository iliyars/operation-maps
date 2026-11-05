using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace OperationMaps.Tests.Word;

internal static class WordDocumentAssertions
{
  public static Document DocumentOf(WordprocessingDocument document)
    => DocumentOf(Assert.IsType<MainDocumentPart>(document.MainDocumentPart));

  public static Document DocumentOf(MainDocumentPart? part)
  {
    Assert.NotNull(part);
    return Assert.IsType<Document>(part.Document);
  }
  public static Body BodyOf(WordprocessingDocument document) => Assert.IsType<Body>(DocumentOf(document).Body);
  public static Body BodyOf(MainDocumentPart part) => Assert.IsType<Body>(DocumentOf(part).Body);
}
