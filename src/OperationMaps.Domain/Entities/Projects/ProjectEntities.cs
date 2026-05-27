using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Domain.Entities.Users;
using OperationMaps.Domain.Enums;


namespace OperationMaps.Domain.Entities.Projects;

public class Project
{
  public int Id { get; set; }
  public string Name { get; set; } = "";
  public int CreatedById { get; set; }
  public AppUser CreatedBy { get; set; } = null!;
  public DateTime CreatedAt { get; set; }
  public string? SourceFileName { get; set; }

  public List<ProjectComponent> Components { get; set; } = new();
  public List<RegimeGroup> RegimeGroups { get; set; } = new();
}

public class ProjectComponent
{
  public int Id { get; set; }
  public int ProjectId { get; set; }
  public Project Project { get; set; } = null!;

  public string Designation { get; set; } = "";
  public string? RawName { get; set; }
  public int Quantity { get; set; }
  public MatchStatus MatchStatus { get; set; }

  public int? ComponentId { get; set; }
  public Component? Component { get; set; }
  public string? DetectedCategory { get; set; }
}

public class RegimeGroup
{
  public int Id { get; set; }
  public int ProjectId { get; set; }
  public Project Project { get; set; } = null!;
  public int FormId { get; set; }
  public Form Form { get; set; } = null!;

  public string Label { get; set; } = "";

  public int? LoadFactorParameterId { get; set; }
  public FormParameter? LoadFactorParameter { get; set; }
  public string? LoadFactorMin { get; set; }

  public List<RegimeGroupMember> Members { get; set; } = new();
  public List<ParameterCellValue> CellValues { get; set; } = new();
}

public class RegimeGroupMember
{
  public int RegimeGroupId { get; set; }
  public RegimeGroup RegimeGroup { get; set; } = null!;
  public int ProjectComponentId { get; set; }
  public ProjectComponent ProjectComponent { get; set; } = null!;
}

public class ParameterCellValue
{
  public int Id { get; set; }
  public int RegimeGroupId { get; set; }
  public RegimeGroup RegimeGroup { get; set; } = null!;
  public int FormParameterId { get; set; }
  public FormParameter FormParameter { get; set; } = null!;
  public int FormValueColumnId { get; set; }
  public FormValueColumn FormValueColumn { get; set; } = null!;

  public string Value { get; set; } = "";

  public List<ParameterCellNote> Notes { get; set; } = new();
}

/// <summary>
/// Примечание, выбранное пользователем для конкретной ячейки карты.
/// Order определяет порядок: 1 = *, 2 = **, и т.д.
/// </summary>
public class ParameterCellNote
{
  public int Id { get; set; }

  public int ParameterCellValueId { get; set; }
  public ParameterCellValue ParameterCellValue { get; set; } = null!;

  public int NoteId { get; set; }
  public Note Note { get; set; } = null!;

  /// <summary>Порядковый номер примечания в ячейке: 1 = *, 2 = **, ...</summary>
  public int Order { get; set; }
}
