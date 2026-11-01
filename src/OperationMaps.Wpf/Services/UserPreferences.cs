using System.IO;
using System.Text.Json;
using OperationMaps.Wpf.Features.Components;

namespace OperationMaps.Wpf.Services;

/// <summary>
/// Small per-user settings that outlive a project — e.g. the head of the
/// organization for the title page, which rarely changes between projects.
/// Stored in %LocalAppData%\OperationMaps\preferences.json; a missing or
/// broken file just means "no preferences yet".
/// </summary>
public sealed class UserPreferences
{
  private static readonly string FilePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "OperationMaps", "preferences.json");

  private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
  private string? _storagePath = FilePath;
  public const int RecentProjectLimit = 10;
  public List<RecentProject> RecentProjects { get; set; } = [];
  public ComponentFilter ComponentsFilter { get; set; }
  public bool OwnFormOnlyIncomplete { get; set; }
  public double OwnFormListWidth { get; set; } = 260;
  public double ComponentDetailWidth { get; set; } = 320;
  public bool ComponentDetailsHidden { get; set; }

  public static UserPreferences CreateInMemory() => new() { _storagePath = null };

  public void RememberProject(string path)
  {
    path = Path.GetFullPath(path);
    RecentProjects.RemoveAll(p => string.Equals(p.FilePath, path, StringComparison.OrdinalIgnoreCase));
    RecentProjects.Insert(0, new RecentProject { FilePath = path, LastOpened = DateTimeOffset.UtcNow });
    if (RecentProjects.Count > RecentProjectLimit) RecentProjects.RemoveRange(RecentProjectLimit, RecentProjects.Count - RecentProjectLimit);
    Save();
  }

  public void ForgetProject(string path)
  {
    RecentProjects.RemoveAll(p => string.Equals(p.FilePath, path, StringComparison.OrdinalIgnoreCase));
    Save();
  }

  public string? LastHeadName { get; set; }
  public string? LastHeadOccupation { get; set; }

  // Contents stamp: usually the same people from project to project.
  public string? LastDevelopedBy { get; set; }
  public string? LastCheckedBy { get; set; }
  public string? LastNormControl { get; set; }

  public static UserPreferences Load(string? path = null)
  {
    path ??= FilePath;
    try
    {
      var preferences = File.Exists(path)
          ? JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(path)) ?? new()
          : new();
      preferences._storagePath = path;
      preferences.RecentProjects = (preferences.RecentProjects ?? [])
          .Where(p => p is not null && ValidProjectPath(p.FilePath))
          .OrderByDescending(p => p.LastOpened).DistinctBy(p => p.FilePath, StringComparer.OrdinalIgnoreCase)
          .Take(RecentProjectLimit).ToList();
      if (!Enum.IsDefined(preferences.ComponentsFilter)) preferences.ComponentsFilter = ComponentFilter.All;
      preferences.OwnFormListWidth = ValidWidth(preferences.OwnFormListWidth, 260, 180, 520);
      preferences.ComponentDetailWidth = ValidWidth(preferences.ComponentDetailWidth, 320, 240, 600);
      return preferences;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
    {
      return new() { _storagePath = path };
    }
  }

  public void Save()
  {
    if (_storagePath is null) return;
    var temporary = _storagePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
    try
    {
      Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_storagePath))!);
      File.WriteAllText(temporary, JsonSerializer.Serialize(this, Json));
      File.Move(temporary, _storagePath, overwrite: true);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      // Only a convenience — never worth bothering the user about.
      Serilog.Log.Warning(ex, "Could not save user preferences to {Path}", _storagePath);
    }
    finally
    {
      try { if (File.Exists(temporary)) File.Delete(temporary); }
      catch (IOException) { }
      catch (UnauthorizedAccessException) { }
    }
  }

  private static double ValidWidth(double value, double fallback, double min, double max)
      => double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;

  private static bool ValidProjectPath(string path)
  {
    try { return !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path) && Path.GetFullPath(path).Length > 0; }
    catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
  }
}

public sealed class RecentProject
{
  public string FilePath { get; set; } = "";
  public DateTimeOffset LastOpened { get; set; }
}
