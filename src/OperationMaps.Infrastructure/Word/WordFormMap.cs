using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OperationMaps.Infrastructure.Word
{
  /// <summary>
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
    /// How many component slots fit on one page (one table copy).
    /// Used to calculate total pages and to map component index → slot index.
    /// </summary>
    public int ComponentsPerPage { get; init; }

    /// <summary>
    /// Ordered list of component slots for ONE page/table.
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
    /// Coordinates of the operating conditions cells (колонка "В аппаратуре" Формы 4).
    /// Key: property name from <see cref="OperatingConditions"/> (camelCase).
    /// One cell per parameter — same coordinates on every page.
    /// </summary>
    public IReadOnlyDictionary<string, CellCoord> OperatingConditionCells { get; init; }
        = new Dictionary<string, CellCoord>();

    /// <summary>
    /// Optional: zero-based row index BEFORE which a dynamic row is inserted.
    /// Null means no dynamic row for this form.
    /// </summary>
    public int? OptionalRowInsertIndex { get; init; }

    /// <summary>
    /// Zero-based index of the row to clone as template when inserting a dynamic row.
    /// Only relevant when <see cref="OptionalRowInsertIndex"/> is set.
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
    /// Key: <c>FormParameter.RowNumber</c> as a string (e.g. "1", "12").
    /// </summary>
    public IReadOnlyDictionary<string, ParameterCoord> ParameterCells { get; init; }
        = new Dictionary<string, ParameterCoord>();

    /// <summary>
    /// Coordinate of the note cell for this slot. Null if the form has no note cell.
    /// </summary>
    public CellCoord? NoteCell { get; init; }
  }

  /// <summary>Zero-based row/column pair.</summary>
  public readonly record struct CellCoord(int Row, int Col);

  /// <summary>
  /// Coordinate for a parameter cell that may have separate columns
  /// for "по НТД" and "в схеме". If <see cref="SchemeCol"/> is null,
  /// only NTD column is filled (backward compatible with Form 4).
  /// </summary>
  public readonly record struct ParameterCoord(int Row, int NtdCol, int? SchemeCol);

  /// <summary>Well-known keys used in <see cref="ComponentSlotMap.MetaCells"/>.</summary>
  public static class MetaCellKey
  {
    public const string ComponentName = "componentName";
    public const string ComponentType = "componentType";
    public const string Quantity = "quantity";
    public const string PositionsNumber = "positionsNumber";
  }
}
