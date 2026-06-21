using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace OperationMaps.Infrastructure.Word
{
  /// <summary>
  /// Low-level helper for Word table operations.
  /// All row/column indices are ZERO-BASED throughout this class.
  /// Physical cell index accounts for merged cells (GridSpan) — matches
  /// the coordinate model used in map.json.
  /// </summary>
  public static class WordTableHelper
  {
    // ── Table access ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the table at <paramref name="tableIndex"/> in the document body.
    /// Throws <see cref="InvalidOperationException"/> if the index is out of range.
    /// </summary>
    public static Table GetTable(WordprocessingDocument doc, int tableIndex)
    {
      var body = doc.MainDocumentPart?.Document?.Body
          ?? throw new InvalidOperationException("Document has no body.");

      var tables = body.Elements<Table>().ToList();

      if (tableIndex < 0 || tableIndex >= tables.Count)
        throw new InvalidOperationException(
            $"Table index {tableIndex} is out of range. Document has {tables.Count} table(s).");

      return tables[tableIndex];
    }

    // ── Cell access ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the physical cell at (<paramref name="rowIndex"/>, <paramref name="colIndex"/>).
    /// <para>
    /// "Physical" means each cell is counted once regardless of its GridSpan —
    /// this is how map.json coordinates work.
    /// </para>
    /// Returns <c>null</c> when the row or column index is out of range instead of
    /// throwing, so callers can decide how to handle missing cells gracefully.
    /// </summary>
    public static TableCell? TryGetCell(Table table, int rowIndex, int colIndex)
    {
      var rows = table.Elements<TableRow>().ToList();
      if (rowIndex < 0 || rowIndex >= rows.Count) return null;

      var cells = rows[rowIndex].Elements<TableCell>().ToList();
      if (colIndex < 0 || colIndex >= cells.Count) return null;

      return cells[colIndex];
    }

    /// <summary>
    /// Same as <see cref="TryGetCell"/> but throws when the cell does not exist.
    /// </summary>
    public static TableCell GetCell(Table table, int rowIndex, int colIndex)
        => TryGetCell(table, rowIndex, colIndex)
           ?? throw new InvalidOperationException(
               $"Cell ({rowIndex}, {colIndex}) does not exist in the table.");

    // ── Read ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the concatenated plain text of all paragraphs in the cell.
    /// Paragraphs are joined with a newline; runs within a paragraph are
    /// joined without separator.
    /// </summary>
    public static string GetCellText(TableCell cell)
    {
      var sb = new StringBuilder();
      var paragraphs = cell.Elements<Paragraph>().ToList();

      for (int i = 0; i < paragraphs.Count; i++)
      {
        if (i > 0) sb.Append('\n');
        foreach (var run in paragraphs[i].Elements<Run>())
          sb.Append(run.InnerText);
      }

      return sb.ToString();
    }

    /// <summary>Convenience overload that locates the cell first.</summary>
    public static string GetCellText(Table table, int rowIndex, int colIndex)
        => GetCellText(GetCell(table, rowIndex, colIndex));

    // ── Write ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the text content of <paramref name="cell"/> with <paramref name="text"/>,
    /// preserving the cell's existing paragraph and run properties (font, size,
    /// alignment, shading, borders).
    /// <para>
    /// Multi-line text (containing <c>\n</c>) creates multiple paragraphs, each
    /// inheriting the properties of the first original paragraph.
    /// </para>
    /// </summary>
    public static void SetCellText(TableCell cell, string text)
    {
      // Capture formatting from the first existing paragraph (if any)
      var firstPara = cell.Elements<Paragraph>().FirstOrDefault();
      var paraProps = firstPara?.ParagraphProperties?.CloneNode(true) as ParagraphProperties;
      var runProps = firstPara?.Elements<Run>().FirstOrDefault()
                            ?.RunProperties?.CloneNode(true) as RunProperties;

      // Remove all existing paragraphs
      foreach (var p in cell.Elements<Paragraph>().ToList())
        p.Remove();

      // Split on newline to support multi-line values
      var lines = text.Split('\n');
      foreach (var line in lines)
      {
        var para = new Paragraph();

        if (paraProps is not null)
          para.AppendChild((ParagraphProperties)paraProps.CloneNode(true));

        var run = new Run();
        if (runProps is not null)
          run.AppendChild((RunProperties)runProps.CloneNode(true));

        run.AppendChild(new Text(line) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(run);
        cell.AppendChild(para);
      }
    }

    /// <summary>Convenience overload that locates the cell first.</summary>
    public static void SetCellText(Table table, int rowIndex, int colIndex, string text)
        => SetCellText(GetCell(table, rowIndex, colIndex), text);

    // ── Row cloning (pagination) ──────────────────────────────────────────────

    /// <summary>
    /// Clones an entire block of rows and inserts the clone immediately after
    /// the original block. Used to add pages to a form.
    /// <para>
    /// <paramref name="firstRowIndex"/> and <paramref name="lastRowIndex"/> are
    /// the inclusive zero-based indices of the page block to duplicate.
    /// </para>
    /// </summary>
    public static void CloneRowBlock(Table table, int firstRowIndex, int lastRowIndex)
    {
      var rows = table.Elements<TableRow>().ToList();

      if (firstRowIndex < 0 || lastRowIndex >= rows.Count || firstRowIndex > lastRowIndex)
        throw new ArgumentOutOfRangeException(
            $"Row range [{firstRowIndex}..{lastRowIndex}] is invalid for a table with {rows.Count} rows.");

      // Clone the block
      var clones = rows[firstRowIndex..(lastRowIndex + 1)]
          .Select(r => (TableRow)r.CloneNode(deep: true))
          .ToList();

      // Clear text from cloned cells so the page starts empty
      foreach (var row in clones)
        foreach (var cell in row.Elements<TableCell>())
          ClearCellText(cell);

      // Insert after the last row of the original block
      var anchor = rows[lastRowIndex];
      foreach (var clone in clones)
        anchor.InsertAfterSelf(clone);
    }

    // ── Table cloning (pagination for full-table forms) ───────────────────────

    /// <summary>
    /// Removes all empty paragraphs that appear between <paramref name="table"/>
    /// and the next table or <see cref="SectionProperties"/> in the document body.
    /// Templates often contain trailing paragraphs that cause a blank page.
    /// </summary>
    public static void RemoveTrailingParagraphsAfterTable(
        WordprocessingDocument doc, Table table)
    {
      var body = doc.MainDocumentPart!.Document.Body!;

      var toRemove = new List<Paragraph>();
      bool pastTable = false;

      foreach (var child in body.ChildElements)
      {
        if (!pastTable)
        {
          if (child == table) pastTable = true;
          continue;
        }

        // Stop at next table or sectPr
        if (child is Table || child is SectionProperties)
          break;

        if (child is Paragraph p)
          toRemove.Add(p);
      }

      foreach (var p in toRemove)
        p.Remove();
    }

    /// <summary>
    /// Clones the entire <paramref name="table"/> and inserts the clone
    /// after it in the document body, preceded by a page break paragraph.
    /// All cell text in the clone is cleared so the new page starts empty.
    /// </summary>
    /// <returns>The cloned (empty) table, ready to be filled.</returns>
    public static Table CloneTable(WordprocessingDocument doc, Table table)
    {
      var body = doc.MainDocumentPart!.Document.Body
          ?? throw new InvalidOperationException("Document has no body.");

      var clone = (Table)table.CloneNode(deep: true);

      var pageBreak = new Paragraph(new Run(new Break { Type = BreakValues.Page }));

      // sectPr must always be the last element in body.
      // Insert pageBreak and clone before sectPr (or at end if no sectPr).
      var sectPr = body.Elements<SectionProperties>().LastOrDefault();

      if (sectPr is not null)
      {
        sectPr.InsertBeforeSelf(pageBreak);
        sectPr.InsertBeforeSelf(clone);
      }
      else
      {
        body.AppendChild(pageBreak);
        body.AppendChild(clone);
      }

      return clone;
    }

    // ── Row insertion (dynamic forms) ─────────────────────────────────────────

    /// <summary>
    /// Inserts a new row immediately before <paramref name="rowIndex"/>,
    /// copying the structure (cell count, widths, shading) of the row at
    /// <paramref name="templateRowIndex"/>. The new row's cells are empty.
    /// <para>
    /// Use this for forms where an optional row may appear depending on
    /// whether the component has data for it.
    /// </para>
    /// </summary>
    public static void InsertRowBefore(
        Table table,
        int rowIndex,
        int templateRowIndex)
    {
      var rows = table.Elements<TableRow>().ToList();

      if (rowIndex < 0 || rowIndex > rows.Count)
        throw new ArgumentOutOfRangeException(nameof(rowIndex));
      if (templateRowIndex < 0 || templateRowIndex >= rows.Count)
        throw new ArgumentOutOfRangeException(nameof(templateRowIndex));

      var newRow = (TableRow)rows[templateRowIndex].CloneNode(deep: true);

      // Clear all cell text in the new row
      foreach (var cell in newRow.Elements<TableCell>())
        ClearCellText(cell);

      if (rowIndex == rows.Count)
        table.AppendChild(newRow);
      else
        rows[rowIndex].InsertBeforeSelf(newRow);
    }

    /// <summary>
    /// Inserts a new row immediately after <paramref name="rowIndex"/>,
    /// copying the structure (cell count, widths, shading, vMerge/gridSpan)
    /// of the row at <paramref name="templateRowIndex"/>. The new row's
    /// cells are empty.
    /// <para>
    /// Used for Form 64's optional second supply-voltage row: the template
    /// row to clone IS the primary parameter's row (it has the matching
    /// column layout), and the clone is inserted right after it so the
    /// "по НТД"/"в схеме" pairs read top-to-bottom in the right order.
    /// </para>
    /// <returns>The newly inserted (empty) row.</returns>
    /// </summary>
    public static TableRow InsertRowAfter(
        Table table,
        int rowIndex,
        int templateRowIndex)
    {
      var rows = table.Elements<TableRow>().ToList();

      if (rowIndex < 0 || rowIndex >= rows.Count)
        throw new ArgumentOutOfRangeException(nameof(rowIndex));
      if (templateRowIndex < 0 || templateRowIndex >= rows.Count)
        throw new ArgumentOutOfRangeException(nameof(templateRowIndex));

      var newRow = (TableRow)rows[templateRowIndex].CloneNode(deep: true);

      // Clear all cell text in the new row
      foreach (var cell in newRow.Elements<TableCell>())
        ClearCellText(cell);

      rows[rowIndex].InsertAfterSelf(newRow);
      return newRow;
    }

    // ── Header / footer text replacement ─────────────────────────────────────

    /// <summary>
    /// Replaces all occurrences of each key in <paramref name="replacements"/>
    /// with the corresponding value, searching across all headers and footers
    /// of the document.
    /// <para>
    /// This handles the case where Word splits a placeholder like
    /// <c>{{sheet}}</c> across multiple runs. The method merges adjacent runs
    /// in the same paragraph before scanning.
    /// </para>
    /// </summary>
    public static void ReplaceInHeadersAndFooters(
        WordprocessingDocument doc,
        IReadOnlyDictionary<string, string> replacements)
    {
      var part = doc.MainDocumentPart
          ?? throw new InvalidOperationException("No MainDocumentPart.");

      var targets = new List<OpenXmlPart>();
      targets.AddRange(part.HeaderParts);
      targets.AddRange(part.FooterParts);

      foreach (var target in targets)
        ReplaceInPart(target, replacements);
    }

    /// <summary>
    /// Same as <see cref="ReplaceInHeadersAndFooters"/> but operates on the
    /// main document body (useful for cover-page placeholders).
    /// </summary>
    public static void ReplaceInBody(
        WordprocessingDocument doc,
        IReadOnlyDictionary<string, string> replacements)
    {
      var part = doc.MainDocumentPart
          ?? throw new InvalidOperationException("No MainDocumentPart.");

      ReplaceInPart(part, replacements);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void ClearCellText(TableCell cell)
    {
      var firstPara = cell.Elements<Paragraph>().FirstOrDefault();
      var paraProps = firstPara?.ParagraphProperties?.CloneNode(true) as ParagraphProperties;

      foreach (var p in cell.Elements<Paragraph>().ToList())
        p.Remove();

      var para = new Paragraph();
      if (paraProps is not null)
        para.AppendChild(paraProps);
      cell.AppendChild(para);
    }

    private static void ReplaceInPart(
        OpenXmlPart part,
        IReadOnlyDictionary<string, string> replacements)
    {
      var rootElement = part.RootElement;
      if (rootElement is null) return;

      foreach (var para in rootElement.Descendants<Paragraph>())
      {
        // Merge all runs in the paragraph into a single run before replacing.
        // Word often splits placeholder text (e.g. "{{" and "sheet" and "}}")
        // into separate runs when the user has typed or formatted mid-token.
        MergeRunsInParagraph(para);

        foreach (var run in para.Elements<Run>())
        {
          var textEl = run.Elements<Text>().FirstOrDefault();
          if (textEl is null) continue;

          var value = textEl.InnerText;
          foreach (var (key, replacement) in replacements)
            value = value.Replace(key, replacement);

          if (value != textEl.InnerText)
            textEl.Text = value;
        }
      }
    }

    /// <summary>
    /// Merges all <see cref="Run"/> elements in a paragraph into one,
    /// preserving the run properties of the first run.
    /// Only runs containing <see cref="Text"/> elements are merged;
    /// runs with special content (images, breaks) are left untouched.
    /// </summary>
    private static void MergeRunsInParagraph(Paragraph para)
    {
      var runs = para.Elements<Run>()
          .Where(r => r.Elements<Text>().Any())
          .ToList();

      if (runs.Count <= 1) return;

      var merged = new StringBuilder();
      foreach (var r in runs)
        merged.Append(r.InnerText);

      // Keep the first run, update its text
      var first = runs[0];
      var firstText = first.Elements<Text>().First();
      firstText.Text = merged.ToString();
      firstText.Space = SpaceProcessingModeValues.Preserve;

      // Remove the rest
      for (int i = 1; i < runs.Count; i++)
        runs[i].Remove();
    }
  }
}
