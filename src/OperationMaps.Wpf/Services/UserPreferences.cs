using System.IO;
using System.Text.Json;

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

  public string? LastHeadName { get; set; }
  public string? LastHeadOccupation { get; set; }

  public static UserPreferences Load()
  {
    try
    {
      return File.Exists(FilePath)
          ? JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(FilePath)) ?? new()
          : new();
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
    {
      return new();
    }
  }

  public void Save()
  {
    try
    {
      Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
      File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      // Only a convenience — never worth bothering the user about.
      Serilog.Log.Warning(ex, "Could not save user preferences to {Path}", FilePath);
    }
  }
}
