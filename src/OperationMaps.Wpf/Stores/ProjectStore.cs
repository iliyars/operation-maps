using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Application.Importing;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.Components.Commands;
using OperationMaps.Wpf.Infrastructure.Commands;
using System.Collections.ObjectModel;

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

  public bool HasProject => ProjectName is not null;

  // ----- Components --------------------

  public ObservableCollection<ProjectComponentVm> Components { get; } = [];

  public ProjectMatchResult? MatchResult { get; private set; }


  // ---- Undo/Redo ----------------------

  public UndoRedoStack History { get; } = new();


  // ---- Load ----------------------------

  public void Load(string projectName, ProjectMatchResult matchResult)
  {
    ProjectName = projectName;
    MatchResult = matchResult;

    Components.Clear();
    History.Clear();

    foreach (var entry in matchResult.Matched.Concat(matchResult.Unresolved))
      Components.Add(new ProjectComponentVm(entry));
  }

  public void Clear()
  {
    ProjectName = null;
    MatchResult = null;
    Components.Clear();
    History.Clear();
  }
}
