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
