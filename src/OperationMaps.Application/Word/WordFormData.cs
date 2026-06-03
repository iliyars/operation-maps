using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OperationMaps.Application.Word
{
  public sealed class WordFormData
  {
    public string FormNumber { get; init; } = "";
    public string DocumentDesignation { get; init; } = "";
    public int SheetNumber { get; init; } = 1;
    public int TotalSheets { get; init; } = 1;

    // ── Table cells ───────────────────────────────────────────────────────────

    /// <summary>
    /// All cell values keyed by (row, col) — zero-based coordinates.
    /// Matches the physical table structure in the .docx template.
    /// </summary>
    public Dictionary<(int Row, int Col), string> Cells { get; init; } = new();

    // ── Header/footer fields ──────────────────────────────────────────────────

    /// <summary>
    /// Key-value pairs for header/footer placeholders.
    /// Keys match bookmark names or content control tags in the template.
    /// </summary>
    public Dictionary<string, string> HeaderFields { get; init; } = new();

    // ── Helpers ───────────────────────────────────────────────────────────────

    public string GetCell(int row, int col)
        => Cells.TryGetValue((row, col), out var v) ? v : "";

    public void SetCell(int row, int col, string value)
        => Cells[(row, col)] = value;
  }
}
