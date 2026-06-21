using OperationMaps.Domain.Enums;

namespace OperationMaps.Domain.Entities.Forms;

public class Form
{
  public int Id { get; set; }
  public string Number { get; set; } = "";
  public string Title { get; set; } = "";
  public bool IsUniversal { get; set; }
  public ColumnGrouping ColumnGrouping { get; set; }
  public int? DefaultLoadFactorParameterId { get; set; }
  public FormParameter? DefaultLoadFactorParameter { get; set; }
  public List<FormSection> Sections { get; set; } = new();
  public List<FormParameter> Parameters { get; set; } = new();
  public List<FormValueColumn> ValueColumns { get; set; } = new();
}

public class FormSection
{
  public int Id { get; set; }
  public int FormId { get; set; }
  public Form Form { get; set; } = null!;
  public string Title { get; set; } = "";
  public int Order { get; set; }
  public List<FormParameter> Parameters { get; set; } = new();
}

public class FormParameter
{
  public int Id { get; set; }
  public int FormId { get; set; }
  public Form Form { get; set; } = null!;
  public int? SectionId { get; set; }
  public FormSection? Section { get; set; }
  public int RowNumber { get; set; }
  public string Name { get; set; } = "";
  public string? Unit { get; set; }
  public bool CanBeLoadFactorBase { get; set; }
  public int Order { get; set; }
  public string? Formula { get; set; }

  /// <summary>
  /// Whether this parameter must be filled by the user.
  /// Used to calculate completion status of a form column.
  /// </summary>
  public bool IsRequired { get; set; }

  /// <summary>
  /// True for parameters that represent an optional second value of another
  /// parameter — e.g. a second supply voltage in Form 64. The Word template
  /// does not have a physical row for this by default: the export service
  /// inserts one dynamically (cloning the primary parameter's row) only for
  /// components that actually have a value for this parameter.
  /// </summary>
  public bool IsOptional { get; set; }

  /// <summary>
  /// When <see cref="IsOptional"/> is true, the RowNumber of the "primary"
  /// parameter this one extends (e.g. the optional second supply voltage
  /// points to the main "напряжение питания" RowNumber). Null otherwise.
  /// </summary>
  public int? OptionalForRowNumber { get; set; }
}

public class FormValueColumn
{
  public int Id { get; set; }
  public int FormId { get; set; }
  public Form Form { get; set; } = null!;
  public string Key { get; set; } = "";      // "scheme" | "ntd" | "pins"
  public string Title { get; set; } = "";
  public ColumnSource Source { get; set; }
  public int Order { get; set; }
}
