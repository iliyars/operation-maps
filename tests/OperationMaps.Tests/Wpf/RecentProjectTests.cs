using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf;
using OperationMaps.Wpf.Features.Overview;
using OperationMaps.Wpf.Features.Welcome;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Services;
using OperationMaps.Wpf.Shell;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Tests.Wpf;

public sealed class RecentProjectTests : IDisposable
{
  private readonly string _folder = Path.Combine(Path.GetTempPath(), "recent-project-" + Guid.NewGuid().ToString("N"));
  private readonly SqliteConnection _connection = new("Data Source=:memory:");
  private readonly ServiceProvider _provider;
  private readonly Mock<IDialogService> _dialogs = new();
  private readonly UserPreferences _preferences = UserPreferences.CreateInMemory();
  private readonly ProjectStore _store;
  private readonly ShellViewModel _shell;
  private readonly WelcomeViewModel _welcome;
  private string ProjectPath => Path.Combine(_folder, "sample.omaps");

  public RecentProjectTests()
  {
    Directory.CreateDirectory(_folder);
    _connection.Open();
    using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    db.Database.EnsureCreated();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
    services.AddPresentation();
    services.AddSingleton(_preferences);
    services.AddSingleton(_dialogs.Object);
    services.AddSingleton<IDbContextFactory<CatalogDbContext>>(TestCatalogDbContextFactory.From(db));
    _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    _store = _provider.GetRequiredService<ProjectStore>();
    _store.Load("sample", _folder, new ProjectMatchResult { Matched = [], Unresolved = [], Warnings = [] });
    _shell = _provider.GetRequiredService<ShellViewModel>();
    _welcome = _provider.GetRequiredService<WelcomeViewModel>();
  }

  [Fact]
  public async Task Successful_save_is_added_to_recent_list_and_opens_overview_from_there()
  {
    await _shell.SaveProjectCommand.ExecuteAsync(null);
    Assert.Equal(ProjectPath, Assert.Single(_preferences.RecentProjects).FilePath);
    await _welcome.OnNavigatedToAsync();
    Assert.True(_welcome.HasRecentProjects);
    await _welcome.OpenRecentCommand.ExecuteAsync(_welcome.RecentProjects[0]);
    Assert.IsType<ProjectOverviewViewModel>(_provider.GetRequiredService<INavigationService>().CurrentScreen);
    Assert.False(_welcome.RecentProjects[0].OpenFailed);
    Assert.Equal(ProjectPath, _store.ProjectFilePath);
  }

  [Fact]
  public async Task Missing_recent_file_keeps_current_project_and_is_not_removed_from_list()
  {
    _preferences.RememberProject(ProjectPath);
    await _welcome.OnNavigatedToAsync();
    await _welcome.OpenRecentCommand.ExecuteAsync(_welcome.RecentProjects[0]);
    Assert.True(_welcome.RecentProjects[0].OpenFailed);
    Assert.Single(_preferences.RecentProjects);
    Assert.Equal("sample", _store.ProjectName);
    Assert.Null(_store.ProjectFilePath);
    _dialogs.Verify(d => d.ShowError(It.IsAny<string>(), "Открытие проекта"), Times.Once);
  }

  [Fact]
  public async Task Cancelling_unsaved_changes_does_not_label_recent_file_as_unavailable()
  {
    _preferences.RememberProject(ProjectPath);
    _store.MarkDirty();
    _dialogs.Setup(d => d.AskYesNoCancel(It.IsAny<string>(), It.IsAny<string>())).Returns((bool?)null);
    await _welcome.OnNavigatedToAsync();
    await _welcome.OpenRecentCommand.ExecuteAsync(_welcome.RecentProjects[0]);
    Assert.False(_welcome.RecentProjects[0].OpenFailed);
    Assert.True(_store.IsDirty);
    Assert.Null(_store.ProjectFilePath);
    _dialogs.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  public void Dispose()
  {
    _provider.Dispose();
    _connection.Dispose();
    foreach (var file in Directory.GetFiles(_folder)) File.Delete(file);
    Directory.Delete(_folder);
  }
}
