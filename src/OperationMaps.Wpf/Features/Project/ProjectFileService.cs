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
  private readonly CatalogDbContext _db;
  private readonly IComponentMatcher _matcher;
  private readonly OwnFormStateService _ownForms;

  public ProjectFileService(
      ProjectStore store,
      CatalogDbContext db,
      IComponentMatcher matcher,
      OwnFormStateService ownForms)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    _db = db ?? throw new ArgumentNullException(nameof(db));
    _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
    _ownForms = ownForms ?? throw new ArgumentNullException(nameof(ownForms));
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
    var file = Snapshot();

    // Write next to the target first, then swap — a crash mid-write must
    // not leave the user with a truncated project file.
    var tmpPath = path + ".tmp";
    await using (var stream = File.Create(tmpPath))
      await JsonSerializer.SerializeAsync(stream, file, Json, ct);
    File.Move(tmpPath, path, overwrite: true);

    _store.ProjectFilePath = path;
    _store.ProjectName = Path.GetFileNameWithoutExtension(path);
    _store.ProjectFolderPath = Path.GetDirectoryName(path);
    _store.IsDirty = false;
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
        .Select(c => c.Entry.Imported)
        .Select(i => new SavedComponent
        {
          ImportIndex = i.ImportIndex,
          RawName = i.RawName,
          DetectedCategory = i.DetectedCategory,
          Positions = i.Positions.ToList(),
          RawPositions = i.RawPositions,
        })
        .ToList(),
    Form4Notes = _store.Components
        .Where(c => c.Entry.Imported.Positions.Count > 0)
        .SelectMany(c => c.NtdValues
            .Where(row => row.Notes.Count > 0)
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
        Positions = col.Component.Entry.Imported.Positions.ToList(),
        LoadFactorBaseRow = col.LoadFactorBaseParameterId is { } baseId && rowById.TryGetValue(baseId, out var b)
            ? b.RowNumber
            : null,
        // The load-factor result row is calculated, not entered — it is
        // recomputed on open, so there's no point in storing it.
        Values = col.CellValues
            .Where(kv => !string.IsNullOrEmpty(kv.Value)
                      && rowById.TryGetValue(kv.Key, out var p) && !p.IsOptional && !p.IsLoadFactorResult)
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

    if (file is null)
      throw new InvalidDataException("Файл пуст или не является проектом OperationMaps.");
    if (file.Version > ProjectFile.CurrentVersion)
      throw new InvalidDataException(
          "Проект сохранён более новой версией программы — обновите OperationMaps.");

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

    _store.Load(
        Path.GetFileNameWithoutExtension(path),
        Path.GetDirectoryName(path) ?? AppContext.BaseDirectory,
        matchResult,
        importResult);
    _store.Conditions = file.Conditions;
    _store.HeadName = file.HeadName;
    _store.HeadOccupation = file.HeadOccupation;
    _store.NormControl = file.NormControl;

    await RestoreForm4NotesAsync(file.Form4Notes, ct);
    foreach (var saved in file.OwnForms)
      await RestoreOwnFormAsync(saved, ct);

    _store.ProjectFilePath = path;
    _store.IsDirty = false;

    return matchResult;
  }

  private async Task RestoreForm4NotesAsync(IEnumerable<SavedForm4Notes> saved, CancellationToken ct)
  {
    foreach (var entry in saved)
    {
      var component = _store.Components.FirstOrDefault(
          c => c.Entry.Imported.Positions.Contains(entry.Position));
      if (component is null) continue;

      await component.LoadNtdValuesAsync(_db, ct);

      var row = component.NtdValues.FirstOrDefault(r => r.RowNumber == entry.Row);
      if (row is null) continue;

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
  private async Task RestoreOwnFormAsync(SavedOwnForm saved, CancellationToken ct)
  {
    var formId = await _db.Forms
        .Where(f => f.Number == saved.FormNumber)
        .Select(f => (int?)f.Id)
        .FirstOrDefaultAsync(ct);
    if (formId is null) return;

    var state = await _ownForms.GetAsync(formId.Value, ct);
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

      if (savedCol.LoadFactorBaseRow is { } baseRow && primaryByRow.TryGetValue(baseRow, out var baseParam))
        column.LoadFactorBaseParameterId = baseParam.FormParameterId;

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
