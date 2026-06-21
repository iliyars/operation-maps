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

    /// <summary>
    /// Per-slot configuration for a dynamically-inserted optional parameter row
    /// (e.g. the second supply voltage in Form 64). Key: FormParameter.RowNumber
    /// of the OPTIONAL parameter (as a string, e.g. "1.5" or a dedicated id),
    /// matching <see cref="ComponentSlotMap.OptionalRowSlots"/> keys.
    /// Null/empty for forms without this feature.
    /// </summary>
    public IReadOnlyDictionary<string, OptionalRowMap> OptionalRows { get; init; }
        = new Dictionary<string, OptionalRowMap>();
  }

  /// <summary>
  /// Describes how to insert and fill an optional parameter row for one
  /// component slot. Coordinates are relative to the slot's own column
  /// position — same NtdCol/SchemeCol as the primary row, but the row
  /// itself does not exist in the template and must be cloned in.
  /// </summary>
  public sealed class OptionalRowMap
  {
    /// <summary>
    /// FormParameter.RowNumber of the PRIMARY parameter this optional row
    /// extends (e.g. 1 for "напряжение питания"). The cloned row is inserted
    /// immediately after the primary row's current position.
    /// </summary>
    public required int PrimaryRowNumber { get; init; }

    /// <summary>
    /// Zero-based row index of the template row to clone, AS IT EXISTS in the
    /// original (un-cloned) table — i.e. the primary parameter's row, since
    /// the optional row has the same column layout.
    /// </summary>
    public required int TemplateRowIndex { get; init; }

    /// <summary>
    /// Column coordinates for this slot's optional row cells. Same column
    /// indices as the primary row's NtdCol/SchemeCol — only the row index
    /// changes (computed at runtime as TemplateRowIndex + 1 after insertion).
    /// </summary>
    public required ParameterCoord Coord { get; init; }
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
