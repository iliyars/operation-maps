using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

namespace OperationMaps.Infrastructure.Word;

/// <summary>Converts known unit-bearing template tokens to their Transitional numeric equivalents.</summary>
public static class WordDocumentCompatibility
{
  private const string WordNs = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
  private const string DrawingNs = "http://schemas.openxmlformats.org/drawingml/2006/main";
  private const string RelativeDrawingNs = "http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing";

  public static void Normalize(WordprocessingDocument document)
  {
    var visited = new HashSet<OpenXmlPart>();
    var roots = new List<OpenXmlPartRootElement>();
    void Visit(OpenXmlPartContainer container)
    {
      foreach (var relationship in container.Parts)
      {
        var part = relationship.OpenXmlPart;
        if (!visited.Add(part)) continue;
        if (part.RootElement is { } root)
        {
          roots.Add(root);
          foreach (var node in root.Descendants().Prepend(root).ToArray()) NormalizeElement(node);
        }
        Visit(part);
      }
    }
    Visit(document);
    // Word requires drawing object ids to be unique across the document,
    // including headers copied from other templates. Schema validation alone
    // does not detect these collisions across package parts.
    var drawings = roots.SelectMany(r => r.Descendants()).Where(e => e.LocalName == "docPr"
        && e.NamespaceUri == "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing").ToList();
    static uint? DrawingId(OpenXmlElement drawing) => uint.TryParse(drawing.GetAttribute("id", "").Value,
        NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;
    var reserved = drawings.Select(DrawingId).Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
    var used = new HashSet<uint>();
    uint next = 1;
    foreach (var drawing in drawings)
    {
      if (DrawingId(drawing) is { } id && id != 0 && used.Add(id)) continue;
      // Avoid assigning an id that occurs later in the package.
      while (reserved.Contains(next) || used.Contains(next)) next++;
      drawing.SetAttribute(new OpenXmlAttribute("id", "", next.ToString(CultureInfo.InvariantCulture)));
      used.Add(next);
    }
    foreach (var root in roots) root.Save();
  }

  private static void NormalizeElement(OpenXmlElement node)
  {
    foreach (var attribute in node.GetAttributes().ToArray())
    {
      string? replacement = null;
      if (node.NamespaceUri == WordNs && attribute.NamespaceUri == WordNs)
      {
        var twips = node.LocalName switch
        {
          "pgMar" => attribute.LocalName is "top" or "bottom" or "left" or "right" or "header" or "footer" or "gutter",
          "pgSz" => attribute.LocalName is "w" or "h",
          "tcW" or "tblW" or "tblInd" or "wBefore" or "wAfter" => attribute.LocalName == "w",
          "top" or "bottom" or "left" or "right" or "start" or "end" => attribute.LocalName == "w"
              && node.Parent?.LocalName is "tcMar" or "tblCellMar",
          "tab" => attribute.LocalName == "pos",
          "defaultTabStop" => attribute.LocalName == "val",
          _ => false,
        };
        if (twips) replacement = ConvertUnit(attribute.Value, "pt", 20);
        if (node.LocalName == "lvlJc" && attribute.LocalName == "val")
          replacement = attribute.Value switch { "start" => "left", "end" => "right", _ => replacement };
      }
      if (node.NamespaceUri == DrawingNs && attribute.NamespaceUri.Length == 0)
      {
        var percentage = (node.LocalName == "gs" && attribute.LocalName == "pos")
            || (node.LocalName == "miter" && attribute.LocalName == "lim")
            || (node.LocalName is "satMod" or "lumMod" or "tint" or "shade" or "alpha" && attribute.LocalName == "val");
        if (percentage) replacement = ConvertUnit(attribute.Value, "%", 1000);
      }
      if (replacement is not null)
        node.SetAttribute(new OpenXmlAttribute(attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, replacement));
    }
    if (node.NamespaceUri == RelativeDrawingNs && node.LocalName is "pctWidth" or "pctHeight"
        && node is OpenXmlLeafTextElement text && ConvertUnit(text.Text, "%", 1000) is { } percentageValue)
      text.Text = percentageValue;
  }

  private static string? ConvertUnit(string? value, string suffix, decimal factor)
  {
    if (value is null || !value.EndsWith(suffix, StringComparison.Ordinal)) return null;
    if (!decimal.TryParse(value.AsSpan(0, value.Length - suffix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
      throw new InvalidDataException($"Некорректный размер в шаблоне Word: {value}");
    return decimal.Round(number * factor, 0, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
  }
}
