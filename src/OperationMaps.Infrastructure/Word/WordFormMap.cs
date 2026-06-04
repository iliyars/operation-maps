using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OperationMaps.Infrastructure.Word
{
  // <summary>
  /// In-memory representation of a form's <c>map.json</c> file.
  /// Describes where each piece of data lives in the Word table.
  /// All row/column values are zero-based (loader converts from 1-based JSON).
  /// </summary>
  public sealed class WordFormMap
  {
    /// <summary>Form number as a string, e.g. "4", "67", "68".</summary>
    public required string FormNumber { get; init; }

    /// <summary>Zero-based index of the target table in the document body.</summary>
    public int TableIndex { get; init; }

    /// <summary>
    /// How many component slots fit on one page of the template.
    /// Used by the export service to calculate how many times to clone the page block.
    /// </summary>
    public int ComponentsPerPage { get; init; }

    /// <summary>
    /// Zero-based row index of the first data row on a page (inclusive).
    /// Together with <see cref="PageLastRow"/> this defines the block that
    /// <see cref="WordTableHelper.CloneRowBlock"/> duplicates for each extra page.
    /// Defaults to 0 when not specified in the JSON (clone from the very first row).
    /// </summary>
    public int PageFirstRow { get; init; }

    /// <summary>
    /// Zero-based row index of the last data row on a page (inclusive).
    /// Must be set explicitly in <c>map.json</c> — the loader will throw if missing.
    /// </summary>
    public int PageLastRow { get; init; }

    /// <summary>
    /// Ordered list of component slots for ONE page.
    /// Count must equal <see cref="ComponentsPerPage"/>.
    /// </summary>
    public IReadOnlyList<ComponentSlotMap> ComponentSlots { get; init; } = [];

    /// <summary>
    /// Header/footer placeholder replacements.
    /// Key: placeholder string in the template (e.g. <c>{{sheet}}</c>).
    /// Value: field name in <c>WordFormData</c> (e.g. <c>sheetNumber</c>).
    /// </summary>
    public IReadOnlyDictionary<string, string> HeaderReplacements { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Optional: if this form has a dynamically inserted row, this value
    /// is the zero-based row index BEFORE which the new row is inserted.
    /// Null means no dynamic row for this form.
    /// </summary>
    public int? OptionalRowInsertIndex { get; init; }

    /// <summary>
    /// Zero-based index of the template row to clone when inserting
    /// a dynamic row. Only relevant when <see cref="OptionalRowInsertIndex"/> is set.
    /// </summary>
    public int? OptionalRowTemplateIndex { get; init; }
  }

  /// <summary>
  /// Describes the cell coordinates for one component slot on a page.
  /// All coordinates are zero-based.
  /// </summary>
  public sealed class ComponentSlotMap
  {
    /// <summary>
    /// Coordinates of the fixed meta-cells (component name, designation, quantity).
    /// Keys match <see cref="MetaCellKey"/> constants.
    /// </summary>
    public IReadOnlyDictionary<string, CellCoord> MetaCells { get; init; }
        = new Dictionary<string, CellCoord>();

    /// <summary>
    /// Coordinates of parameter value cells.
    /// Key: <see cref="FormParameter.RowNumber"/> as a string (e.g. "1", "12").
    /// </summary>
    public IReadOnlyDictionary<string, CellCoord> ParameterCells { get; init; }
        = new Dictionary<string, CellCoord>();

    /// <summary>
    /// Coordinate of the note cell for this slot. Null if the form has no note cell.
    /// </summary>
    public CellCoord? NoteCell { get; init; }
  }

  /// <summary>Zero-based row/column pair.</summary>
  public readonly record struct CellCoord(int Row, int Col);

  /// <summary>Well-known keys used in <see cref="ComponentSlotMap.MetaCells"/>.</summary>
  public static class MetaCellKey
  {
    public const string ComponentName = "componentName";
    public const string Designation = "designation";
    public const string Quantity = "quantity";
  }


}
