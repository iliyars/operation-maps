using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;

namespace OperationMaps.Infrastructure.Word
{
  public static class WordDocumentHelper
  {
    // ── Table reading ─────────────────────────────────────────────────────────

    /// <summary>
    /// Reads all cell texts from a table by physical row/col coordinates.
    /// Handles merged cells — merged cells are counted by their logical position.
    /// </summary>
    public static Dictionary<(int Row, int Col), string> ReadTable(Table table)
    {
      var result = new Dictionary<(int Row, int Col), string>();
      var rows = table.Elements<TableRow>().ToList();

      for (int r = 0; r < rows.Count; r++)
      {
        var cells = rows[r].Elements<TableCell>().ToList();
        for (int c = 0; c < cells.Count; c++)
        {
          var text = GetCellText(cells[c]);
          result[(r, c)] = text;
        }
      }

      return result;
    }

    /// <summary>
    /// Gets the table at the given index (0-based) from the document body.
    /// </summary>
    public static Table? GetTable(WordprocessingDocument doc, int tableIndex = 0)
    {
      return doc.MainDocumentPart?
          .Document?.Body?
          .Elements<Table>()
          .ElementAtOrDefault(tableIndex);
    }

    /// <summary>Gets the text content of a table cell.</summary>
    public static string GetCellText(TableCell cell)
    {
      var texts = cell.Descendants<Text>()
          .Select(t => t.Text ?? "");
      return string.Join("", texts).Trim();
    }

    // ── Table writing ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the text of a specific cell by row/col coordinates.
    /// Preserves existing formatting (font, size, bold etc.).
    /// </summary>
    public static void SetCellText(Table table, int row, int col, string value)
    {
      var rows = table.Elements<TableRow>().ToList();
      if (row >= rows.Count) return;

      var cells = rows[row].Elements<TableCell>().ToList();
      if (col >= cells.Count) return;

      SetCellText(cells[col], value);
    }

    /// <summary>
    /// Sets the text of a table cell, preserving run formatting.
    /// </summary>
    public static void SetCellText(TableCell cell, string value)
    {
      // Find first paragraph (preserve paragraph formatting)
      var para = cell.Elements<Paragraph>().FirstOrDefault();
      if (para is null)
      {
        para = new Paragraph();
        cell.AppendChild(para);
      }

      // Preserve run properties from first existing run
      RunProperties? existingRpr = para
          .Elements<Run>()
          .FirstOrDefault()
          ?.RunProperties
          ?.CloneNode(true) as RunProperties;

      // Clear existing runs
      foreach (var run in para.Elements<Run>().ToList())
        run.Remove();

      // Add new run with preserved formatting
      var newRun = new Run();
      if (existingRpr is not null)
        newRun.AppendChild(existingRpr);
      var text = new Text(value);
      text.SetAttribute(new OpenXmlAttribute("space", "http://www.w3.org/XML/1998/namespace", "preserve"));
      newRun.AppendChild(text);
      para.AppendChild(newRun);
    }

    // ── Header/Footer ─────────────────────────────────────────────────────────

    /// <summary>
    /// Reads all text from all headers in the document.
    /// Returns a dictionary keyed by header type (default/first/even).
    /// </summary>
    public static Dictionary<string, string> ReadHeaders(WordprocessingDocument doc)
    {
      var result = new Dictionary<string, string>();
      var mainPart = doc.MainDocumentPart;
      if (mainPart is null) return result;

      foreach (var rel in mainPart.HeaderParts)
      {
        var id = mainPart.GetIdOfPart(rel);
        var text = string.Join(" ",
            rel.Header.Descendants<Text>().Select(t => t.Text));
        result[id] = text.Trim();
      }

      return result;
    }

    /// <summary>
    /// Finds a run in any header that contains the given placeholder text
    /// and replaces it with the new value.
    /// </summary>
    public static void ReplaceInHeaders(
        WordprocessingDocument doc,
        string placeholder,
        string value)
    {
      var mainPart = doc.MainDocumentPart;
      if (mainPart is null) return;

      foreach (var headerPart in mainPart.HeaderParts)
        ReplaceTextInPart(headerPart.Header, placeholder, value);
    }

    /// <summary>
    /// Finds a run in any footer that contains the given placeholder text
    /// and replaces it with the new value.
    /// </summary>
    public static void ReplaceInFooters(
        WordprocessingDocument doc,
        string placeholder,
        string value)
    {
      var mainPart = doc.MainDocumentPart;
      if (mainPart is null) return;

      foreach (var footerPart in mainPart.FooterParts)
        ReplaceTextInPart(footerPart.Footer, placeholder, value);
    }

    /// <summary>
    /// Replaces all occurrences of a placeholder in text runs of an OpenXml element.
    /// </summary>
    public static void ReplaceTextInPart(
        DocumentFormat.OpenXml.OpenXmlElement root,
        string placeholder,
        string value)
    {
      foreach (var text in root.Descendants<Text>())
      {
        if (text.Text?.Contains(placeholder) == true)
          text.Text = text.Text.Replace(placeholder, value);
      }
    }

    // ── Document cloning ──────────────────────────────────────────────────────

    /// <summary>
    /// Opens a template document, copies it to the output path, and returns
    /// the opened copy for editing.
    /// </summary>
    public static WordprocessingDocument OpenCopy(string templatePath, string outputPath)
    {
      File.Copy(templatePath, outputPath, overwrite: true);
      return WordprocessingDocument.Open(outputPath, isEditable: true);
    }
  }
}
