using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Services;

namespace OperationMaps.Tests.Wpf;

public sealed class UserPreferencesTests : IDisposable
{
  private readonly string _folder = Path.Combine(Path.GetTempPath(), "workspace-preferences-" + Guid.NewGuid().ToString("N"));
  private string Settings => Path.Combine(_folder, "preferences.json");

  [Fact]
  public void Recent_projects_are_deduplicated_bounded_and_persisted()
  {
    var preferences = UserPreferences.Load(Settings);
    for (var index = 0; index < 12; index++) preferences.RememberProject(Path.Combine(_folder, $"project-{index}.omaps"));
    var newest = preferences.RecentProjects[0].FilePath;
    preferences.RememberProject(newest.ToUpperInvariant());
    var reopened = UserPreferences.Load(Settings);
    Assert.Equal(UserPreferences.RecentProjectLimit, reopened.RecentProjects.Count);
    Assert.Equal(newest.ToUpperInvariant(), reopened.RecentProjects[0].FilePath);
    Assert.Single(reopened.RecentProjects, p => p.FilePath.Equals(newest, StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void Workspace_preferences_survive_restart()
  {
    var preferences = UserPreferences.Load(Settings);
    preferences.ComponentsFilter = ComponentFilter.Unresolved;
    preferences.OwnFormOnlyIncomplete = true;
    preferences.OwnFormListWidth = 410;
    preferences.ComponentDetailWidth = 470;
    preferences.ComponentDetailsHidden = true;
    preferences.Save();
    var reopened = UserPreferences.Load(Settings);
    Assert.Equal(ComponentFilter.Unresolved, reopened.ComponentsFilter);
    Assert.True(reopened.OwnFormOnlyIncomplete);
    Assert.True(reopened.ComponentDetailsHidden);
    Assert.Equal(410, reopened.OwnFormListWidth);
    Assert.Equal(470, reopened.ComponentDetailWidth);
  }

  [Theory]
  [InlineData("{\"LastHeadName\":\"Иванов\"}")]
  [InlineData("{\"RecentProjects\":null,\"ComponentsFilter\":999,\"OwnFormListWidth\":-5,\"ComponentDetailWidth\":5000}")]
  [InlineData("not json")]
  public async Task Older_or_damaged_settings_load_with_usable_defaults(string contents)
  {
    Directory.CreateDirectory(_folder);
    await File.WriteAllTextAsync(Settings, contents);
    var preferences = UserPreferences.Load(Settings);
    Assert.Empty(preferences.RecentProjects);
    Assert.Equal(ComponentFilter.All, preferences.ComponentsFilter);
    Assert.InRange(preferences.OwnFormListWidth, 180, 520);
    Assert.InRange(preferences.ComponentDetailWidth, 240, 600);
  }

  [Fact]
  public async Task Forgetting_a_project_does_not_delete_its_file()
  {
    Directory.CreateDirectory(_folder);
    var project = Path.Combine(_folder, "keep.omaps");
    await File.WriteAllTextAsync(project, "project data");
    var preferences = UserPreferences.Load(Settings);
    preferences.RememberProject(project);
    preferences.ForgetProject(project);
    Assert.Empty(UserPreferences.Load(Settings).RecentProjects);
    Assert.Equal("project data", await File.ReadAllTextAsync(project));
  }

  public void Dispose()
  {
    if (!Directory.Exists(_folder)) return;
    foreach (var file in Directory.GetFiles(_folder)) File.Delete(file);
    Directory.Delete(_folder);
  }
}
