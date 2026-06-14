using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OperationMaps.Application.Models;

namespace OperationMaps.Application.Word
{
  public sealed class WordFormData
  {
    /// <summary>Form number as a string, e.g. "4", "67", "68".</summary>
    public required string FormNumber { get; init; }

    /// <summary>
    /// Document designation (обозначение документа).
    /// Written into the header via the <c>{{designation}}</c> placeholder.
    /// </summary>
    public string DocumentDesignation { get; init; } = "";

    /// <summary>
    /// Field values substituted into header/footer placeholders.
    /// Keys must match the values in <c>map.json → headerReplacements</c>.
    /// Standard keys: <c>sheetNumber</c>, <c>totalSheets</c>, <c>designation</c>.
    /// </summary>
    public IReadOnlyDictionary<string, string> HeaderFields { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// Ordered list of components to fill into the form.
    /// The export service paginates this list automatically using
    /// <c>componentsPerPage</c> from the map.
    /// </summary>
    public IReadOnlyList<WordComponentData> Components { get; init; } = [];

    /// <summary>
    /// Operating conditions for the "В аппаратуре" column of Form 4.
    /// Null for forms that do not have this column.
    /// </summary>
    public OperatingConditions? OperatingConditions { get; init; }


    /// <summary>
    /// Whether this form contains a dynamically inserted optional row.
    /// When true, the export service calls InsertRowBefore for each component
    /// that has <see cref="WordComponentData.HasOptionalRow"/> set.
    /// </summary>
    public bool HasOptionalRows { get; init; }
  }

  /// <summary>Data for one component slot in the form.</summary>
  public sealed class WordComponentData
  {
    /// <summary>Component name / family name.</summary>
    public string Name { get; init; } = "";

    /// <summary>Designation string, e.g. "С1, С2–С5".</summary>
    public string Positions { get; init; } = "";

    public string ComponentTypeName { get; init; } = "";

    /// <summary>Total quantity across all positions.</summary>
    public string Quantity { get; init; } = "";

    /// <summary>
    /// NTD values from the catalog keyed by <c>FormParameter.RowNumber</c>.
    /// Used by Form 4 (read-only NTD columns).
    /// Empty for forms that only have scheme values.
    /// </summary>
    public IReadOnlyDictionary<int, string> NtdValues { get; init; }
        = new Dictionary<int, string>();

    /// <summary>
    /// User-entered "in-circuit" values keyed by <c>FormParameter.RowNumber</c>.
    /// Used by Form 67, 68, and other own-forms.
    /// Empty for Form 4.
    /// </summary>
    public IReadOnlyDictionary<int, string> SchemeValues { get; init; }
        = new Dictionary<int, string>();

    /// <summary>
    /// Formatted note string for the note cell, e.g. "* Обеспечивается конструкцией".
    /// Already concatenated and ordered — the export service writes it as-is.
    /// </summary>
    public string Note { get; init; } = "";

    /// <summary>
    /// When true, the export service inserts an optional row for this component
    /// before filling the slot. Only relevant for the one dynamic form.
    /// </summary>
    public bool HasOptionalRow { get; init; }

    /// <summary>
    /// Value for the optional row cell (when <see cref="HasOptionalRow"/> is true).
    /// </summary>
    public string OptionalRowValue { get; init; } = "";
  }
}
