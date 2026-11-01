using System.IO;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Stores;
using Serilog;

namespace OperationMaps.Wpf.Features.Project;

public sealed record ProjectRecoveryFile(ProjectFile Project, string ProjectFolderPath,
    string? ProjectFilePath, DateTimeOffset SavedAt);

public sealed class ProjectRecoveryOptions
{
  public string Folder { get; init; } = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OperationMaps", "Recovery");
}

/// <summary>One recovery file and exclusive lease per application instance.</summary>
public sealed class ProjectRecoveryService(
    ProjectStore store, ProjectFileService projects,
    ProjectRecoveryOptions options, IDialogService dialogs) : IDisposable
{
  private readonly SemaphoreSlim _gate = new(1, 1);
  private readonly string _path = Path.Combine(options.Folder, Guid.NewGuid().ToString("N") + ".recovery");
  private FileStream? _lease;
  private long _generation = -1;
  private long _revision = -1;
  private bool _stopped;
  private bool _warned;

  public async Task CheckpointAsync(CancellationToken ct = default)
  {
    if (!await _gate.WaitAsync(0, ct)) return;
    try
    {
      if (_stopped) return;
      if (!store.HasProject || !store.IsDirty)
      {
        if (File.Exists(_path)) File.Delete(_path);
        _generation = _revision = -1;
        return;
      }
      if (_generation == store.Generation && _revision == store.Revision) return;
      Directory.CreateDirectory(options.Folder);
      _lease ??= File.Open(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
      var generation = store.Generation;
      var revision = store.Revision;
      await projects.SaveRecoveryAsync(_path, ct);
      _generation = generation;
      _revision = revision;
    }
    finally { _gate.Release(); }
  }

  public async Task<bool> OfferRestoreAsync(CancellationToken ct = default)
  {
    if (!Directory.Exists(options.Folder)) return false;
    foreach (var path in Directory.GetFiles(options.Folder, "*.recovery").OrderByDescending(File.GetLastWriteTimeUtc))
    {
      FileStream lease;
      try { lease = File.Open(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
      catch (IOException) { continue; } // This session is still running.
      using (lease)
      {
        var answer = dialogs.AskYesNoCancel(
            $"Найдена аварийная копия от {File.GetLastWriteTime(path):g}.\n\n" +
            "Восстановить несохранённый проект?\nДа — восстановить; Нет — удалить копию; Отмена — оставить на следующий запуск.",
            "Восстановление проекта");
        if (answer is null) return false;
        if (answer == false) { File.Delete(path); continue; }
        var restored = false;
        try
        {
          await projects.OpenRecoveryAsync(path, ct);
          restored = true;
          // Keep a copy in this session before releasing the old recovery file.
          await CheckpointAsync(ct);
          File.Delete(path);
          return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
          dialogs.ShowError("Не удалось восстановить проект. Копия сохранена.\n\n" + ex.Message, "Восстановление проекта");
          return restored;
        }
      }
    }
    return false;
  }

  public async Task TickAsync()
  {
    try { await CheckpointAsync(); }
    catch (Exception ex)
    {
      Log.Warning(ex, "Could not write project recovery file");
      if (!_warned)
      {
        _warned = true;
        dialogs.ShowWarning("Не удалось записать аварийную копию. Сохраняйте проект вручную.\n\n" + ex.Message, "Автовосстановление");
      }
    }
  }

  public async Task CompleteAsync()
  {
    await _gate.WaitAsync();
    try
    {
      _stopped = true;
      File.Delete(_path);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    { Log.Warning(ex, "Could not remove completed recovery file"); }
    finally { _gate.Release(); }
  }

  public void Dispose()
  {
    _lease?.Dispose();
    try { File.Delete(_path + ".lock"); }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }
  }
}
