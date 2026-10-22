using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Models;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.Components.Commands;
using OperationMaps.Wpf.Features.OwnForm;
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

  // ── Project metadata ──────────────────────────────────────────────────────

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HasProject))]
  private string? _projectName;

  /// <summary>Название изделия из XML (field_2).</summary>
  [ObservableProperty] private string? _documentTitle;

  /// <summary>Децимальный номер из XML (field_3) — используется в колонтитуле.</summary>
  [ObservableProperty] private string? _documentNumber;

  /// <summary>Разработал (field_16).</summary>
  [ObservableProperty] private string? _developedBy;

  /// <summary>Проверил (field_17).</summary>
  [ObservableProperty] private string? _checkedBy;

  /// <summary>Утвердил (field_18).</summary>
  [ObservableProperty] private string? _approvedBy;

  /// <summary>Руководитель организации для титульного листа: "Фамилия И.О." (вводит пользователь).</summary>
  [ObservableProperty] private string? _headName;

  /// <summary>Должность руководителя для титульного листа (вводит пользователь).</summary>
  [ObservableProperty] private string? _headOccupation;

  /// <summary>Нормоконтроль — штамп содержания (вводит пользователь).</summary>
  [ObservableProperty] private string? _normControl;

  /// <summary>
  /// Absolute path to the project folder (the folder containing the .omaps file).
  /// All generated Word documents are saved relative to this path.
  /// Null when no project is open.
  /// </summary>
  [ObservableProperty]
  private string? _projectFolderPath;

  public bool HasProject => ProjectName is not null;

  /// <summary>
  /// Path of the .omaps file this project was opened from / last saved to.
  /// Null for a project freshly imported from XML and not yet saved.
  /// </summary>
  [ObservableProperty] private string? _projectFilePath;

  /// <summary>True when the project has edits not yet written to <see cref="ProjectFilePath"/>.</summary>
  [ObservableProperty] private bool _isDirty;

  public void MarkDirty() => IsDirty = true;

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

  /// <summary>
  /// Path to the operating conditions JSON file: <c>{ProjectFolderPath}/conditions.json</c>.
  /// </summary>
  public string? ConditionsFilePath => ProjectFolderPath is null
      ? null
      : Path.Combine(ProjectFolderPath, "conditions.json");

  /// <summary>Current operating conditions for this project.</summary>
  [ObservableProperty] private OperatingConditions _conditions = new();

  // ── Components ────────────────────────────────────────────────────────────

  public ObservableCollection<ProjectComponentVm> Components { get; } = [];

  public ProjectStore()
  {
    // Form 4 notes live on the components themselves
    Components.CollectionChanged += (_, e) =>
    {
      if (e.OldItems is not null)
        foreach (ProjectComponentVm c in e.OldItems) c.Edited -= MarkDirty;
      if (e.NewItems is not null)
        foreach (ProjectComponentVm c in e.NewItems) c.Edited += MarkDirty;
    };
  }

  public ProjectMatchResult? MatchResult { get; private set; }

  // ── Own-form tables ───────────────────────────────────────────────────────

  /// <summary>
  /// Per-form table state (values, notes, splits/merges, undo history),
  /// keyed by FormId. Filled lazily by <see cref="OwnFormStateService"/> and
  /// kept for the project's lifetime, so edits survive navigation.
  /// </summary>
  public IReadOnlyDictionary<int, OwnFormState> OwnForms => _ownForms;
  private readonly Dictionary<int, OwnFormState> _ownForms = [];

  public void AddOwnForm(OwnFormState state)
  {
    _ownForms[state.FormId] = state;
    state.Edited += MarkDirty;
  }

  // ── Undo / Redo ───────────────────────────────────────────────────────────

  public UndoRedoStack History { get; } = new();

  // ── Load / Clear ──────────────────────────────────────────────────────────

  /// <summary>
  /// Loads a project from an XML import result.
  /// </summary>
  /// <param name="projectName">Display name (file name without extension).</param>
  /// <param name="projectFolderPath">
  /// Absolute path to the folder that contains (or will contain) the .omaps file.
  /// All Word exports go into <c>projectFolderPath/Forms/</c>.
  /// </param>
  /// <param name="matchResult">Component matching result.</param>
  public void Load(string projectName, string projectFolderPath, ProjectMatchResult matchResult, ImportResult? importResult = null)
  {
    ProjectName = projectName;
    ProjectFolderPath = projectFolderPath;
    ProjectFilePath = null;
    MatchResult = matchResult;
    DocumentTitle = importResult?.DocumentTitle;
    DocumentNumber = importResult?.DocumentNumber;
    DevelopedBy = importResult?.DevelopedBy;
    CheckedBy = importResult?.CheckedBy;
    ApprovedBy = importResult?.ApprovedBy;
    HeadName = null;
    HeadOccupation = null;
    NormControl = null;

    Components.Clear();
    ClearOwnForms();
    History.Clear();
    IsDirty = false;

    // Preserve original XML order by sorting on import index
    foreach (var entry in matchResult.Matched
                 .Concat(matchResult.Unresolved)
                 .OrderBy(e => e.Imported.ImportIndex))
      Components.Add(new ProjectComponentVm(entry));
  }

  private void ClearOwnForms()
  {
    foreach (var state in _ownForms.Values)
      state.Edited -= MarkDirty;
    _ownForms.Clear();
  }

  public void Clear()
  {
    ProjectFilePath = null;
    ProjectName = null;
    ProjectFolderPath = null;
    MatchResult = null;
    DocumentTitle = null;
    DocumentNumber = null;
    DevelopedBy = null;
    CheckedBy = null;
    ApprovedBy = null;
    HeadName = null;
    HeadOccupation = null;
    NormControl = null;
    Conditions = new();
    Components.Clear();
    ClearOwnForms();
    History.Clear();
    IsDirty = false;
  }
}
