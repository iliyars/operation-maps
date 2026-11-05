using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using OperationMaps.Infrastructure.Word;

namespace OperationMaps.Tests.Word;

public sealed class WordDocumentCompatibilityTests
{
  [Fact]
  public void Drawing_ids_are_unique_across_body_and_headers_and_stable_on_second_pass()
  {
    using var stream = new MemoryStream();
    using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
    var main = document.AddMainDocumentPart();
    var bodyDrawing = new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 1, Name = "Body" };
    var headerDrawing = new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 1, Name = "Header" };
    main.Document = new Document(new Body(new Paragraph(new Run(new Drawing(
        new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(bodyDrawing))))));
    var header = main.AddNewPart<HeaderPart>();
    header.Header = new Header(new Paragraph(new Run(new Drawing(
        new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(headerDrawing)))));
    WordDocumentCompatibility.Normalize(document);
    Assert.NotEqual(bodyDrawing.Id!.Value, headerDrawing.Id!.Value);
    var assigned = headerDrawing.Id.Value;
    WordDocumentCompatibility.Normalize(document);
    Assert.Equal(assigned, headerDrawing.Id.Value);
    Assert.Equal(1u, bodyDrawing.Id.Value);
  }

  [Theory]
  [MemberData(nameof(WordTemplateValidatorTests.RealForms), MemberType = typeof(WordTemplateValidatorTests))]
  public void Real_templates_are_valid_after_normalization_and_keep_their_text(string number)
  {
    var path = Path.Combine(AppContext.BaseDirectory, "Word", "Resources", $"Form{number}", "template.docx");
    using var stream = new MemoryStream();
    stream.Write(File.ReadAllBytes(path));
    stream.Position = 0;
    using var document = WordprocessingDocument.Open(stream, true);
    var main = document.MainDocumentPart!;
    var text = main.Document!.InnerText;

    WordDocumentCompatibility.Normalize(document);

    Assert.Equal(text, main.Document.InnerText);
    var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).ToList();
    Assert.True(errors.Count == 0, string.Join("\n", errors.Select(e => e.Description)));
    var normalized = main.Document.OuterXml;
    WordDocumentCompatibility.Normalize(document);
    Assert.Equal(normalized, main.Document.OuterXml);
  }

  [Fact]
  public void Point_sizes_keep_their_physical_dimensions_and_document_text_is_untouched()
  {
    using var stream = new MemoryStream();
    using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
    var main = document.AddMainDocumentPart();
    var margins = new PageMargin();
    margins.SetAttribute(new OpenXmlAttribute("w", "top", margins.NamespaceUri, "-17pt"));
    margins.SetAttribute(new OpenXmlAttribute("w", "left", margins.NamespaceUri, "56.70pt"));
    main.Document = new Document(new Body(new Paragraph(new Run(new Text("Размер 56.70pt; нагрузка 63%"))),
        new SectionProperties(margins)));

    WordDocumentCompatibility.Normalize(document);

    Assert.Equal(-340, margins.Top!.Value);
    Assert.Equal(1134u, margins.Left!.Value);
    Assert.Equal("Размер 56.70pt; нагрузка 63%", main.Document.InnerText);
  }
}
