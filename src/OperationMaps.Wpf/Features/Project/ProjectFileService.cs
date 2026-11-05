using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Wpf.Features.Project;

/// <summary>
/// Saves the open project to a <c>.omaps</c> file and opens it back into
/// <see cref="ProjectStore"/>. See <see cref="ProjectFile"/> for what is
/// stored and why.
/// </summary>
public sealed class ProjectFileService
{
  public const string Extension = ".omaps";
  public const string FileFilter = "Проект OperationMaps (*.omaps)|*.omaps";

  private static readonly JsonSerializerOptions Json = new()
  {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  private readonly ProjectStore _store;
  private readonly IDbContextFactory<CatalogDbContext> _dbFactory;
  private readonly IComponentMatcher _matcher;
  private readonly IProjectFileWriter _writer;
  private readonly SemaphoreSlim _operations = new(1, 1);

  public ProjectFileService(
      ProjectStore store,
      IDbContextFactory<CatalogDbContext> dbFactory,
      IComponentMatcher matcher,
      IProjectFileWriter writer)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
    _writer = writer ?? throw new ArgumentNullException(nameof(writer));
  }

  /// <summary>
  /// Where "Сохранить" writes a project that has never been saved: next to
  /// the imported XML, named after it (the Forms/ export folder is there too).
  /// </summary>
  public string? DefaultSavePath => _store.ProjectFolderPath is null || _store.ProjectName is null
      ? null
      : Path.Combine(_store.ProjectFolderPath, _store.ProjectName + Extension);

  // ── Save ──────────────────────────────────────────────────────────────────

  public async Task SaveAsync(string path, CancellationToken ct = default)
  {
    await _operations.WaitAsync(ct);
    try
    {
      path = Path.GetFullPath(path);
      var generation = _store.Generation;
      var revision = _store.Revision;
      // Serialize before yielding: even mutable conditions belong to this exact revision.
      var contents = JsonSerializer.SerializeToUtf8Bytes(Snapshot(), Json);
      await _writer.WriteAsync(path, contents, ct);

      if (_store.Generation != generation) return;
      _store.ProjectFilePath = path;
      _store.ProjectName = Path.GetFileNameWithoutExtension(path);
      _store.ProjectFolderPath = Path.GetDirectoryName(path);
      if (_store.Revision == revision) _store.IsDirty = false;
    }
    finally
    {
      _operations.Release();
    }
  }

  internal ProjectFile Snapshot() => new()
  {
    ProjectName = _store.ProjectName ?? "",
    DocumentTitle = _store.DocumentTitle,
    DocumentNumber = _store.DocumentNumber,
    DevelopedBy = _store.DevelopedBy,
    CheckedBy = _store.CheckedBy,
    ApprovedBy = _store.ApprovedBy,
    HeadName = _store.HeadName,
    HeadOccupation = _store.HeadOccupation,
    NormControl = _store.NormControl,
    Conditions = _store.Conditions,
    Components = _store.Components
        .Select(c => new SavedComponent
        {
          ImportIndex = c.Entry.Imported.ImportIndex,
          RawName = c.Entry.Imported.RawName,
          DetectedCategory = c.Entry.Imported.DetectedCategory,
          Positions = c.Entry.Imported.Positions.ToList(),
          RawPositions = c.Entry.Imported.RawPositions,
          Regime = c.ActiveRegime,
        })
        .ToList(),
    Form4Notes = _store.Components
        .Where(c => c.Entry.Imported.Positions.Count > 0)
        .SelectMany(c => c.NtdValues
            .Select(row => new SavedForm4Notes
            {
              Position = c.Entry.Imported.Positions[0],
              Row = row.RowNumber,
              Notes = row.Notes.OrderBy(n => n.Order).Select(n => n.NoteText).ToList(),
            }))
        .ToList(),
    OwnForms = _store.OwnForms.Values
        .OrderBy(s => s.FormNumber)
        .Select(SnapshotOwnForm)
        .ToList(),
  };

  private static SavedOwnForm SnapshotOwnForm(OwnFormState state)
  {
    var rowById = state.Parameters.ToDictionary(p => p.FormParameterId);

    return new SavedOwnForm
    {
      FormNumber = state.FormNumber,
      Columns = state.Columns.Select(col => new SavedColumn
      {
        HasRegimeOverride = col.HasRegimeOverride,
        Regime = col.HasRegimeOverride ? col.Component.ActiveRegime : null,
        Positions = col.Component.Entry.Imported.Positions.ToList(),
        LoadFactorBaseRow = col.LoadFactorBaseParameterId is { } baseId && rowById.TryGetValue(baseId, out var b)
            ? b.RowNumber
            : null,
        // Formulas and load factor are recomputed on open from the input rows.
        Values = col.CellValues
            .Where(kv => !string.IsNullOrEmpty(kv.Value)
                      && rowById.TryGetValue(kv.Key, out var p) && !p.IsOptional && !p.IsLoadFactorResult && p.Formula is null)
            .ToDictionary(kv => rowById[kv.Key].RowNumber, kv => kv.Value),
        OptionalValues = col.OptionalCellValues
            .Where(kv => !string.IsNullOrEmpty(kv.Value) && rowById.ContainsKey(kv.Key))
            .ToDictionary(kv => rowById[kv.Key].RowNumber, kv => kv.Value),
        Notes = col.Notes
            .Where(kv => rowById.ContainsKey(kv.Key))
            .SelectMany(kv => kv.Value.Select(n => (Param: rowById[kv.Key], Note: n)))
            .OrderBy(x => x.Note.Order)
            .Select(x => new SavedNote { Row = x.Param.RowNumber, Optional = x.Param.IsOptional, Text = x.Note.NoteText })
            .ToList(),
      }).ToList(),
    };
  }

  // ── Open ──────────────────────────────────────────────────────────────────

  /// <summary>
  /// Loads <paramref name="path"/> into <see cref="ProjectStore"/>, re-matching
  /// its components against the current catalog and restoring the user's
  /// edits on top. Throws <see cref="InvalidDataException"/> for files that
  /// aren't valid projects.
  /// </summary>
  public async Task<ProjectMatchResult> OpenAsync(string path, CancellationToken ct = default)
  {
    await _operations.WaitAsync(ct);
    try { return await OpenCoreAsync(Path.GetFullPath(path), ct); }
    finally { _operations.Release(); }
  }

  private async Task<ProjectMatchResult> OpenCoreAsync(string path, CancellationToken ct)
  {
    var generation = _store.Generation;
    var revision = _store.Revision;
    ProjectFile? file;
    try
    {
      await using var stream = File.OpenRead(path);
      file = await JsonSerializer.DeserializeAsync<ProjectFile>(stream, Json, ct);
    }
    catch (JsonException ex)
    {
      throw new InvalidDataException("Файл повреждён или не является проектом OperationMaps.", ex);
    }

    ProjectFileValidator.Validate(file);

    return await RestoreCoreAsync(file, Path.GetFileNameWithoutExtension(path),
        Path.GetDirectoryName(path) ?? AppContext.BaseDirectory, path, generation, revision, ct);
  }

  public async Task SaveRecoveryAsync(string path, CancellationToken ct = default)
  {
    await _operations.WaitAsync(ct);
    try
    {
      var recovery = new ProjectRecoveryFile(Snapshot(), _store.ProjectFolderPath!, _store.ProjectFilePath, DateTimeOffset.UtcNow);
      var bytes = JsonSerializer.SerializeToUtf8Bytes(recovery, Json);
      await _writer.WriteAsync(path, bytes, ct);
    }
    finally { _operations.Release(); }
  }

  public async Task<ProjectMatchResult> OpenRecoveryAsync(string path, CancellationToken ct = default)
  {
    await _operations.WaitAsync(ct);
    try
    {
      var generation = _store.Generation;
      var revision = _store.Revision;
      ProjectRecoveryFile? recovery;
      try
      {
        await using var stream = File.OpenRead(path);
        recovery = await JsonSerializer.DeserializeAsync<ProjectRecoveryFile>(stream, Json, ct);
      }
      catch (JsonException ex) { throw new InvalidDataException("Аварийная копия повреждена.", ex); }
      if (recovery is null || string.IsNullOrWhiteSpace(recovery.ProjectFolderPath))
        throw new InvalidDataException("Аварийная копия не содержит папку проекта.");
      ProjectFileValidator.Validate(recovery.Project);
      var result = await RestoreCoreAsync(recovery.Project, recovery.Project.ProjectName,
          recovery.ProjectFolderPath, recovery.ProjectFilePath, generation, revision, ct);
      _store.MarkDirty();
      return result;
    }
    finally { _operations.Release(); }
  }

  private async Task<ProjectMatchResult> RestoreCoreAsync(ProjectFile file, string name,
      string folder, string? sourcePath, long generation, long revision, CancellationToken ct)
  {
    var importResult = new ImportResult
    {
      Components = file.Components
          .Select(c => new ImportedComponent
          {
            ImportIndex = c.ImportIndex,
            RawName = c.RawName,
            DetectedCategory = c.DetectedCategory,
            Positions = c.Positions,
            RawPositions = c.RawPositions,
          })
          .ToList(),
      Warnings = [],
      DocumentTitle = file.DocumentTitle,
      DocumentNumber = file.DocumentNumber,
      DevelopedBy = file.DevelopedBy,
      CheckedBy = file.CheckedBy,
      ApprovedBy = file.ApprovedBy,
    };

    var matchResult = await _matcher.MatchAllAsync(importResult.Components, ct);

    var prepared = new ProjectStore();
    var ownForms = new OwnFormStateService(prepared, _dbFactory);
    prepared.Load(
        name,
        folder,
        matchResult,
        importResult);
    prepared.Conditions = file.Conditions;
    prepared.HeadName = file.HeadName;
    prepared.HeadOccupation = file.HeadOccupation;
    prepared.NormControl = file.NormControl;
    foreach (var component in prepared.Components)
    {
      var savedComponent = file.Components.FirstOrDefault(c => c.Positions.Intersect(component.Entry.Imported.Positions).Any())
        ?? file.Components.FirstOrDefault(c => c.ImportIndex == component.Entry.Imported.ImportIndex && c.RawName == component.Entry.Imported.RawName);
      component.SelectRegime(savedComponent?.Regime);
    }

    await RestoreForm4NotesAsync(prepared, file.Form4Notes, ct);
    foreach (var saved in file.OwnForms)
      await RestoreOwnFormAsync(ownForms, saved, file.Version, ct);

    prepared.ProjectFilePath = sourcePath;
    prepared.IsDirty = false;

    ct.ThrowIfCancellationRequested();
    if (_store.Generation != generation || _store.Revision != revision)
      throw new IOException("Текущий проект изменился во время открытия файла. Повторите открытие после сохранения изменений.");
    _store.ReplaceWith(prepared);

    return matchResult;
  }

  private async Task RestoreForm4NotesAsync(ProjectStore store, IEnumerable<SavedForm4Notes> saved, CancellationToken ct)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    foreach (var entry in saved)
    {
      var component = store.Components.FirstOrDefault(
          c => c.Entry.Imported.Positions.Contains(entry.Position));
      if (component is null) continue;

      await component.LoadNtdValuesAsync(db, ct);

      var row = component.NtdValues.FirstOrDefault(r => r.RowNumber == entry.Row);
      if (row is null) continue;

      row.Notes.Clear();
      foreach (var text in entry.Notes)
        row.AddNote(text);
    }
  }

  /// <summary>
  /// Rebuilds a form's column layout from the saved one. Columns are matched
  /// by position designators: each saved column takes its positions from
  /// whichever fresh column now holds them (that's where the catalog NTD
  /// values come from). Positions no longer in the project are dropped;
  /// positions not covered by any saved column (components added to the
  /// project since) keep their default column.
  /// </summary>
  private async Task RestoreOwnFormAsync(OwnFormStateService ownForms, SavedOwnForm saved, int version, CancellationToken ct)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    var formId = await db.Forms
        .Where(f => f.Number == saved.FormNumber)
        .Select(f => (int?)f.Id)
        .FirstOrDefaultAsync(ct);
    if (formId is null) return;

    var state = await ownForms.GetAsync(formId.Value, ct);
    if (state is null) return;

    var primaryByRow = state.Parameters
        .Where(p => !p.IsOptional)
        .GroupBy(p => p.RowNumber)
        .ToDictionary(g => g.Key, g => g.First());
    var optionalByRow = state.Parameters
        .Where(p => p.IsOptional)
        .GroupBy(p => p.RowNumber)
        .ToDictionary(g => g.Key, g => g.First());

    var defaults = state.Columns.ToList();
    var regimeParameters = saved.Columns.Any(c => c.HasRegimeOverride)
        ? await db.FormParameters.Where(p => p.FormId == formId.Value).ToListAsync(ct) : [];
    var columnByPosition = new Dictionary<string, FormColumnVm>();
    foreach (var col in defaults)
      foreach (var pos in col.Component.Entry.Imported.Positions)
        columnByPosition.TryAdd(pos, col);

    var taken = new HashSet<string>();
    var result = new List<FormColumnVm>();

    foreach (var savedCol in saved.Columns)
    {
      var positions = savedCol.Positions
          .Where(p => columnByPosition.ContainsKey(p) && taken.Add(p))
          .ToList();
      if (positions.Count == 0) continue;

      var column = columnByPosition[positions[0]].CloneWithPositions(positions);
      if (savedCol.HasRegimeOverride)
      {
        column.SelectRegimeOverride(savedCol.Regime);
        column.RefreshRegime(state.FormNumber, regimeParameters);
      }
      foreach (var notes in column.Notes.Values) notes.Clear();

      if (savedCol.LoadFactorBaseRow is { } baseRow && primaryByRow.TryGetValue(baseRow, out var baseParam))
        column.LoadFactorBaseParameterId = baseParam.FormParameterId;
      else if (version >= 4 && savedCol.LoadFactorBaseRow is null)
        column.LoadFactorBaseParameterId = null;

      foreach (var (row, value) in savedCol.Values)
        if (primaryByRow.TryGetValue(row, out var p))
          column.CellValues[p.FormParameterId] = value;

      foreach (var (row, value) in savedCol.OptionalValues)
        if (optionalByRow.TryGetValue(row, out var p))
          column.OptionalCellValues[p.FormParameterId] = value;

      // Notes are numbered (*, **, ...) in the order their parameters were
      // first touched; the detail panel touches every row top-down, so
      // pre-create the per-row lists in that order to reproduce numbering.
      foreach (var p in state.Parameters)
        column.GetNotes(p.FormParameterId);

      foreach (var note in savedCol.Notes)
      {
        var byRow = note.Optional ? optionalByRow : primaryByRow;
        if (byRow.TryGetValue(note.Row, out var p))
          column.AddNote(p.FormParameterId, note.Text);
      }
      column.OrderNoteParameters(savedCol.Notes.Select(note =>
          (note.Optional ? optionalByRow : primaryByRow).GetValueOrDefault(note.Row)?.FormParameterId ?? -1));

      result.Add(column);
    }

    foreach (var col in defaults)
    {
      var leftover = col.Component.Entry.Imported.Positions.Where(p => !taken.Contains(p)).ToList();
      if (leftover.Count == col.Component.Entry.Imported.Positions.Count)
        result.Add(col);
      else if (leftover.Count > 0)
        result.Add(col.CloneWithPositions(leftover));
    }

    state.ReplaceColumns(result);
  }
}
