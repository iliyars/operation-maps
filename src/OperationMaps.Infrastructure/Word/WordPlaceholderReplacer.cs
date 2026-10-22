using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace OperationMaps.Infrastructure.Word;

/// <summary>
/// Replaces placeholders like <c>&lt;HeadName&gt;</c> in the body, headers
/// and footers of a document, keeping the formatting around them.
/// <para>
/// Word routinely splits a typed placeholder over several runs
/// (<c>&lt;</c> | <c>HeadName</c> | <c>&gt;Д4</c>). Unlike
/// <see cref="WordTableHelper.ReplaceInBody"/>, which merges every run of a
/// paragraph into one (and so flattens any mixed formatting), this rewrites
/// only the text elements the placeholder actually spans: the replacement
/// takes the formatting of the run where the placeholder starts, and
/// everything outside the placeholder stays exactly as it was.
/// </para>
/// </summary>
public static class WordPlaceholderReplacer
{
  public static void ReplaceEverywhere(WordprocessingDocument doc, IReadOnlyDictionary<string, string> replacements)
  {
    var main = doc.MainDocumentPart ?? throw new InvalidOperationException("No MainDocumentPart.");

    var roots = new List<OpenXmlElement?> { main.Document };
    roots.AddRange(main.HeaderParts.Select(h => (OpenXmlElement?)h.Header));
    roots.AddRange(main.FooterParts.Select(f => (OpenXmlElement?)f.Footer));

    foreach (var root in roots.OfType<OpenXmlElement>())
      foreach (var paragraph in root.Descendants<Paragraph>().ToList())
        ReplaceInParagraph(paragraph, replacements);
  }

  public static void ReplaceInParagraph(Paragraph paragraph, IReadOnlyDictionary<string, string> replacements)
  {
    foreach (var (placeholder, value) in replacements)
    {
      if (placeholder.Length == 0) continue;

      // Each pass replaces one occurrence; the search resumes after the
      // inserted value, so a value containing the placeholder can't loop.
      var searchFrom = 0;
      while (true)
      {
        var texts = paragraph.Descendants<Text>().ToList();
        var full = new StringBuilder();
        var starts = new List<int>(texts.Count);
        foreach (var t in texts)
        {
          starts.Add(full.Length);
          full.Append(t.Text);
        }

        if (searchFrom > full.Length) break;
        var index = full.ToString().IndexOf(placeholder, searchFrom, StringComparison.Ordinal);
        if (index < 0) break;
        searchFrom = index + value.Length;

        var end = index + placeholder.Length; // exclusive
        var first = true;

        for (var i = 0; i < texts.Count; i++)
        {
          var tStart = starts[i];
          var tEnd = tStart + texts[i].Text.Length;
          if (tEnd <= index || tStart >= end) continue; // not touched by the placeholder

          var text = texts[i].Text;
          var before = index > tStart ? text[..(index - tStart)] : "";
          var after = end < tEnd ? text[(end - tStart)..] : "";

          texts[i].Text = first ? before + value + after : before + after;
          texts[i].Space = SpaceProcessingModeValues.Preserve;
          first = false;
        }
      }
    }
  }
}
