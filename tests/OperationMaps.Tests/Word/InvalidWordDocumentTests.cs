using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Infrastructure.Word;

namespace OperationMaps.Tests.Word;

public sealed class InvalidWordDocumentTests
{
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void Merge_rejects_missing_body_in_first_or_appended_document(bool appended)
  {
    var invalid = CreateDocument(hasBody: false);
    var documents = appended ? new[] { CreateDocument(hasBody: true), invalid } : new[] { invalid };
    var error = Assert.Throws<InvalidDataException>(() => WordDocumentMerger.Merge(documents));
    Assert.Contains("тела документа", error.Message);
  }

  [Fact]
  public void Form_detection_rejects_missing_body_with_a_data_error()
  {
    var path = Path.Combine(Path.GetTempPath(), $"invalid-word-{Guid.NewGuid():N}.docx");
    try
    {
      File.WriteAllBytes(path, CreateDocument(hasBody: false));
      var error = Assert.Throws<InvalidDataException>(() => WordFormReader.DetectFormNumbers(path));
      Assert.Contains("тела документа", error.Message);
    }
    finally { File.Delete(path); }
  }

  [Fact]
  public void Table_operations_reject_a_missing_main_document_part()
  {
    using var stream = new MemoryStream();
    using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
    var error = Assert.Throws<InvalidDataException>(() => WordTableHelper.CloneTable(doc, new Table()));
    Assert.Contains("основной части", error.Message);
  }

  private static byte[] CreateDocument(bool hasBody)
  {
    using var stream = new MemoryStream();
    using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
      document.AddMainDocumentPart().Document = hasBody ? new Document(new Body()) : new Document();
    return stream.ToArray();
  }
}
