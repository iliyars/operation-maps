using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace OperationMaps.Infrastructure.Word;

/// <summary>
/// Glues several finished .docx documents into one, each kept as its own
/// section(s): page size and orientation, the frame in its headers/footers
/// and its page-number start all travel with it.
/// <para>
/// Needed for the printed set: the title page and the forms are A3 with
/// different frames, the change registration sheet is A4. Copying only the
/// body (as <see cref="WordReportBuilder"/> does) would give every page the
/// first document's page setup and frame.
/// </para>
/// <para>
/// Mechanics: the target's last section is closed with a section-break
/// paragraph, the next document's body (including its own section
/// properties) is appended, and every relationship it refers to — header and
/// footer parts with their images, pictures in the body, hyperlinks — is
/// copied into the target with re-mapped ids. Each source's styles are
/// imported under unique ids, so matching style names cannot replace formatting.
/// </para>
/// </summary>
public static class WordDocumentMerger
{
  private const string RelationshipsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

  public static byte[] Merge(IReadOnlyList<byte[]> documents)
  {
    if (documents.Count == 0)
      throw new ArgumentException("Nothing to merge.", nameof(documents));

    using var ms = new MemoryStream();
    ms.Write(documents[0]);
    ms.Position = 0;

    // The package writes its changes into the stream only on Dispose.
    using (var target = WordprocessingDocument.Open(ms, isEditable: true))
    {
      var targetDocument = WordDocumentStructure.RequireDocument(target);
      var targetBody = WordDocumentStructure.RequireBody(target);
      var targetMain = target.MainDocumentPart
          ?? throw new InvalidDataException("Документ Word не содержит основной части документа.");

      foreach (var bytes in documents.Skip(1))
      {
        using var sourceStream = new MemoryStream(bytes);
        using var source = WordprocessingDocument.Open(sourceStream, isEditable: false);
        WordDocumentStructure.RequireBody(source);
        var sourceMain = source.MainDocumentPart
            ?? throw new InvalidDataException("Документ Word не содержит основной части документа.");
        Append(targetMain, targetBody, sourceMain);
      }

      WordDocumentCompatibility.Normalize(target);
      targetDocument.Save();
    }

    return ms.ToArray();
  }

  private static void Append(MainDocumentPart targetMain, Body targetBody, MainDocumentPart sourceMain)
  {
    CloseLastSection(targetBody);
    var styleMap = CopyStyles(targetMain, sourceMain);

    var copiedParts = new Dictionary<OpenXmlPart, string>();
    var idMap = new Dictionary<string, string>();

    // Everything, the final sectPr included — it becomes the target's new last section.
    foreach (var element in WordDocumentStructure.RequireBody(sourceMain).ChildElements)
    {
      var clone = element.CloneNode(deep: true);
      RemapStyles(clone, styleMap);
      RemapRelationships(clone, sourceMain, targetMain, copiedParts, idMap);
      targetBody.AppendChild(clone);
    }
    foreach (var part in copiedParts.Keys)
    {
      var copied = targetMain.GetPartById(copiedParts[part]);
      RemapPartStyles(copied, styleMap, new HashSet<OpenXmlPart>());
    }
  }

  /// <summary>
  /// Moves the body-level sectPr into a section-break paragraph, so the
  /// following content starts a new section (on a new page). The paragraph
  /// is shrunk to ~1 pt: after a page-filling table, a normal-height empty
  /// line would spill onto an extra blank page.
  /// </summary>
  private static void CloseLastSection(Body body)
  {
    var section = body.Elements<SectionProperties>().LastOrDefault();
    if (section is null) return;

    section.Remove();
    body.AppendChild(new Paragraph(
        new ParagraphProperties(
            new SpacingBetweenLines { Before = "0", After = "0", Line = "20", LineRule = LineSpacingRuleValues.Exact },
            new ParagraphMarkRunProperties(new FontSize { Val = "2" }),
            section)));
  }

  private static void RemapRelationships(
      OpenXmlElement root,
      MainDocumentPart sourceMain,
      MainDocumentPart targetMain,
      Dictionary<OpenXmlPart, string> copiedParts,
      Dictionary<string, string> idMap)
  {
    foreach (var node in root.Descendants().Prepend(root).ToList())
    {
      foreach (var attribute in node.GetAttributes().Where(a => a.NamespaceUri == RelationshipsNs).ToList())
      {
        if (string.IsNullOrEmpty(attribute.Value)) continue;

        if (!idMap.TryGetValue(attribute.Value!, out var newId))
        {
          newId = CopyRelationship(attribute.Value!, sourceMain, targetMain, copiedParts);
          idMap[attribute.Value!] = newId;
        }

        node.SetAttribute(new OpenXmlAttribute(attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, newId));
      }
    }
  }

  private static string CopyRelationship(
      string sourceId, MainDocumentPart sourceMain, MainDocumentPart targetMain, Dictionary<OpenXmlPart, string> copiedParts)
  {
    var pair = sourceMain.Parts.FirstOrDefault(p => p.RelationshipId == sourceId);
    if (pair.OpenXmlPart is { } part)
    {
      if (copiedParts.TryGetValue(part, out var existing)) return existing;

      // Across packages AddPart deep-copies the part with its own child
      // parts (a header keeps its images and drawings).
      var copy = targetMain.AddPart(part);
      var id = targetMain.GetIdOfPart(copy);
      copiedParts[part] = id;
      return id;
    }

    var hyperlink = sourceMain.HyperlinkRelationships.FirstOrDefault(r => r.Id == sourceId);
    if (hyperlink is not null)
      return targetMain.AddHyperlinkRelationship(hyperlink.Uri, hyperlink.IsExternal).Id;

    var external = sourceMain.ExternalRelationships.FirstOrDefault(r => r.Id == sourceId);
    if (external is not null)
      return targetMain.AddExternalRelationship(external.RelationshipType, external.Uri).Id;

    return sourceId; // unknown — leave as is rather than fail the whole report
  }

  private static void RemapPartStyles(OpenXmlPart part, IReadOnlyDictionary<string, string> map, HashSet<OpenXmlPart> visited)
  {
    if (!visited.Add(part)) return;
    if (part.RootElement is { } root)
    {
      RemapStyles(root, map);
      root.Save();
    }
    foreach (var child in part.Parts) RemapPartStyles(child.OpenXmlPart, map, visited);
  }

  private static void RemapStyles(OpenXmlElement root, IReadOnlyDictionary<string, string> map)
  {
    foreach (var node in root.Descendants().Prepend(root))
    {
      if (node is ParagraphStyleId or RunStyle or TableStyle or BasedOn or NextParagraphStyle or LinkedStyle)
      {
        var attr = node.GetAttributes().FirstOrDefault(a => a.LocalName == "val");
        if (attr.Value is { } id && map.TryGetValue(id, out var replacement))
          node.SetAttribute(new OpenXmlAttribute(attr.Prefix, attr.LocalName, attr.NamespaceUri, replacement));
      }
    }
  }

  private static Dictionary<string, string> CopyStyles(MainDocumentPart targetMain, MainDocumentPart sourceMain)
  {
    var sourceStyles = sourceMain.StyleDefinitionsPart?.Styles;
    var map = new Dictionary<string, string>();
    if (sourceStyles is null) return map;

    var targetStylesPart = targetMain.StyleDefinitionsPart ?? targetMain.AddNewPart<StyleDefinitionsPart>();
    targetStylesPart.Styles ??= new Styles();
    var targetStyles = targetStylesPart.Styles;

    var known = targetStyles.Elements<Style>().Select(s => s.StyleId?.Value).ToHashSet();
    // Style ids (Heading2, Normal, Header...) are local to a template.
    // Import the complete inheritance graph under unique ids, even when names match.
    foreach (var style in sourceStyles.Elements<Style>())
      if (style.StyleId?.Value is { } id)
      {
        var candidate = $"Merged_{known.Count}_{id}";
        while (!known.Add(candidate)) candidate += "_";
        map.Add(id, candidate);
      }

    var defaults = sourceStyles.GetFirstChild<DocDefaults>();
    foreach (var style in sourceStyles.Elements<Style>())
    {
      if (style.StyleId?.Value is not { } id) continue;
      var clone = (Style)style.CloneNode(true);
      clone.StyleId = map[id];
      // Word also identifies styles by their display name. A unique id alone
      // still leaves conflicting built-in names such as "heading 2".
      clone.StyleName = new StyleName { Val = map[id] };
      clone.RemoveAllChildren<Aliases>();
      clone.Default = null;
      RemapStyles(clone, map);
      // Root styles must inherit the source's document defaults, not the title page's.
      if (clone.BasedOn is null)
      {
        var defaultRun = defaults?.GetFirstChild<RunPropertiesDefault>()?.RunPropertiesBaseStyle;
        if (defaultRun is not null)
        {
          clone.StyleRunProperties ??= new StyleRunProperties();
          foreach (var property in defaultRun.ChildElements)
            if (!clone.StyleRunProperties.ChildElements.Any(p => p.LocalName == property.LocalName))
              clone.StyleRunProperties.AddChild(property.CloneNode(true), true);
        }
      }
      targetStyles.AppendChild(clone);
    }

    targetStyles.Save();
    return map;
  }
}
