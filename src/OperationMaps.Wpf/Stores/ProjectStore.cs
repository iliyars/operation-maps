using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Application.Importing;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.Components.Commands;
using OperationMaps.Wpf.Infrastructure.Commands;
using System.Collections.ObjectModel;
using System.IO;

namespace OperationMaps.Wpf.Stores;

/// <summary>
/// Singleton store that holds the current project state.
/// All ViewModels that need project data read from here.
/// Survives navigation — created once, lives for the app lifetime.
/// </summary>
public sealed partial class ProjectStore : ObservableObject
{

  // ---- Project metadat -------------------

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HasProject))]
  private string? _projectName;

  /// <summary>
  /// Absolute path to the project folder (the folder containing the .omaps file).
  /// All generated Word documents are saved relative to this path.
  /// Null when no project is open.
  /// </summary>
  [ObservableProperty]
  private string? _projectFolderPath;

  public bool HasProject => ProjectName is not null;

  /// <summary>
  /// Absolute path to the Forms subfolder: <c>{ProjectFolderPath}/Forms</c>.
  /// Created on first export if it does not exist.
  /// Returns null when no project is open.
  /// </summary>
  public string? FormsFolder => ProjectFolderPath is null
      ? null
      : Path.Combine(ProjectFolderPath, "Forms");

  /// <summary>
  /// Returns the output path for a specific form document,
  /// e.g. <c>{ProjectFolderPath}/Forms/Form4.docx</c>.
  /// Returns null when no project is open.
  /// </summary>
  public string? GetFormDocumentPath(string formNumber)
      => FormsFolder is null
          ? null
          : Path.Combine(FormsFolder, $"Form{formNumber}.docx");

  /// <summary>
  /// Returns the output path for the final report document,
  /// e.g. <c>{ProjectFolderPath}/Forms/Report.docx</c>.
  /// </summary>
  public string? ReportDocumentPath => FormsFolder is null
      ? null
      : Path.Combine(FormsFolder, "Report.docx");

  // ----- Components --------------------

  public ObservableCollection<ProjectComponentVm> Components { get; } = [];

  public ProjectMatchResult? MatchResult { get; private set; }


  // ---- Undo/Redo ----------------------

  public UndoRedoStack History { get; } = new();


  // ---- Load ----------------------------

  public void Load(string projectName, string projectFolderPath, ProjectMatchResult matchResult)
  {
    ProjectName = projectName;
    ProjectFolderPath = projectFolderPath;
    MatchResult = matchResult;

    Components.Clear();
    History.Clear();

    foreach (var entry in matchResult.Matched.Concat(matchResult.Unresolved))
      Components.Add(new ProjectComponentVm(entry));
  }

  public void Clear()
  {
    ProjectName = null;
    ProjectFolderPath = null;
    MatchResult = null;
    Components.Clear();
    History.Clear();
  }
}
