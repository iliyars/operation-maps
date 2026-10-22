using OperationMaps.Application.Models;

namespace OperationMaps.Wpf.Features.Project;

/// <summary>
/// On-disk shape of a <c>.omaps</c> project (JSON).
/// <para>
/// Stores what the user entered, not catalog data: the imported component
/// list (re-matched against the catalog on open, so catalog fixes are
/// picked up), Form 4 notes, and each own form's column layout + "в схеме"
/// values. Parameters are referenced by form number + RowNumber rather than
/// database ids, because every team member has their own catalog.db where
/// ids may differ.
/// </para>
/// </summary>
public sealed class ProjectFile
{
  public const int CurrentVersion = 1;

  public int Version { get; set; } = CurrentVersion;

  public string ProjectName { get; set; } = "";
  public string? DocumentTitle { get; set; }
  public string? DocumentNumber { get; set; }
  public string? DevelopedBy { get; set; }
  public string? CheckedBy { get; set; }
  public string? ApprovedBy { get; set; }

  /// <summary>Title page: head of the organization and their position.</summary>
  public string? HeadName { get; set; }
  public string? HeadOccupation { get; set; }

  /// <summary>Contents stamp: "Н. контр." (Разраб./Пров. are DevelopedBy/CheckedBy above).</summary>
  public string? NormControl { get; set; }

  public OperatingConditions Conditions { get; set; } = new();

  public List<SavedComponent> Components { get; set; } = [];
  public List<SavedForm4Notes> Form4Notes { get; set; } = [];
  public List<SavedOwnForm> OwnForms { get; set; } = [];
}

/// <summary>One line of the imported component list (ПЭ3).</summary>
public sealed class SavedComponent
{
  public int ImportIndex { get; set; }
  public string RawName { get; set; } = "";
  public string DetectedCategory { get; set; } = "";
  public List<string> Positions { get; set; } = [];
  public string? RawPositions { get; set; }
}

/// <summary>Form 4 notes of one component's NTD row, in display order.</summary>
public sealed class SavedForm4Notes
{
  /// <summary>Any position of the component — identifies it after re-import.</summary>
  public string Position { get; set; } = "";
  public int Row { get; set; }
  public List<string> Notes { get; set; } = [];
}

public sealed class SavedOwnForm
{
  public string FormNumber { get; set; } = "";
  public List<SavedColumn> Columns { get; set; } = [];
}

/// <summary>
/// One own-form column. Its positions capture split/merge: a column with
/// R1, R2 out of a component R1–R5 is a split part.
/// </summary>
public sealed class SavedColumn
{
  public List<string> Positions { get; set; } = [];
  public int? LoadFactorBaseRow { get; set; }

  /// <summary>"В схеме" values of regular rows, keyed by RowNumber.</summary>
  public Dictionary<int, string> Values { get; set; } = [];

  /// <summary>"В схеме" values of optional second rows, keyed by the optional parameter's RowNumber.</summary>
  public Dictionary<int, string> OptionalValues { get; set; } = [];

  public List<SavedNote> Notes { get; set; } = [];
}

public sealed class SavedNote
{
  public int Row { get; set; }
  public bool Optional { get; set; }
  public string Text { get; set; } = "";
}
