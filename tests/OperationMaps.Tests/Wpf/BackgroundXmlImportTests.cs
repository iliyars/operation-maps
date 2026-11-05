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
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Tests.Wpf;

public sealed class BackgroundXmlImportTests
{
  [Theory]
  [InlineData(false, false)]
  [InlineData(false, true)]
  [InlineData(true, false)]
  [InlineData(true, true)]
  public async Task Cancellation_during_reading_or_matching_preserves_project(bool blockMatching, bool leave)
  {
    using var fixture = new Fixture(blockMatching);
    var task = fixture.Vm.OpenXmlCommand.ExecuteAsync(null);
    try
    {
      await fixture.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
      Assert.False(task.IsCompleted);
      Assert.True(fixture.Vm.IsBusy);
      Assert.Null(fixture.WorkerContext);
      if (leave) await fixture.Vm.OnNavigatedFromAsync();
      else fixture.Vm.CancelOperationCommand.Execute(null);
      await task.WaitAsync(TimeSpan.FromSeconds(10));
      Assert.False(fixture.Vm.IsBusy);
      Assert.Equal("Original", fixture.Store.ProjectName);
      fixture.Navigation.Verify(n => n.NavigateAsync<ProjectOverviewViewModel>(It.IsAny<object?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
      fixture.Dialogs.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }
    finally { fixture.Vm.CancelOperationCommand.Execute(null); await task; }
  }

  [Fact]
  public async Task Successful_background_import_loads_project_and_navigates()
  {
    using var fixture = new Fixture(null);
    await fixture.Vm.OpenXmlCommand.ExecuteAsync(null);
    Assert.False(fixture.Vm.IsBusy);
    Assert.Equal(Path.GetFileNameWithoutExtension(fixture.Path), fixture.Store.ProjectName);
    fixture.Navigation.Verify(n => n.NavigateAsync<ProjectOverviewViewModel>(null, false, It.IsAny<CancellationToken>()), Times.Once);
    fixture.Dialogs.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
  }

  [Fact]
  public async Task Import_warnings_are_shown_after_successful_import()
  {
    using var fixture = new Fixture(null, ["Строка 2: повторяется позиция VD1"]);
    await fixture.Vm.OpenXmlCommand.ExecuteAsync(null);
    fixture.Dialogs.Verify(d => d.ShowWarning(
        It.Is<string>(message => message.Contains("Предупреждений: 1") && message.Contains("VD1")),
        "Результат импорта XML"), Times.Once);
    fixture.Dialogs.Verify(d => d.ShowError(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    Assert.NotEqual("Original", fixture.Store.ProjectName);
  }

  [Fact]
  public async Task Invalid_xml_keeps_current_project_and_reports_error()
  {
    using var fixture = new Fixture(null, importError: new InvalidDataException("Не найден RecordsData"));
    await fixture.Vm.OpenXmlCommand.ExecuteAsync(null);
    Assert.Equal("Original", fixture.Store.ProjectName);
    Assert.False(fixture.Vm.IsBusy);
    fixture.Dialogs.Verify(d => d.ShowError(It.Is<string>(message => message.Contains("RecordsData")),
        It.IsAny<string?>()), Times.Once);
    fixture.Navigation.Verify(n => n.NavigateAsync<ProjectOverviewViewModel>(It.IsAny<object?>(),
        It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  private sealed class Fixture : IDisposable
  {
    public string Path { get; } = System.IO.Path.GetTempFileName();
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public SynchronizationContext? WorkerContext { get; private set; }
    public Mock<INavigationService> Navigation { get; } = new();
    public Mock<IDialogService> Dialogs { get; } = new();
    public ProjectStore Store { get; }
    public WelcomeViewModel Vm { get; }
    private readonly ServiceProvider _provider;

    public Fixture(bool? blockMatching, IReadOnlyList<string>? warnings = null, Exception? importError = null)
    {
      var picker = new Mock<IFilePicker>();
      picker.Setup(p => p.PickAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Path);
      var importer = new Mock<IComponentListImporter>();
      importer.Setup(i => i.CanImport(Path)).Returns(true);
      importer.Setup(i => i.ImportAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
        .Returns((Stream _, CancellationToken ct) =>
        {
          if (blockMatching == false) Block(ct);
          if (importError is not null) throw importError;
          return Task.FromResult(new ImportResult { Components = [], Warnings = warnings ?? [] });
        });
      var matcher = new Mock<IComponentMatcher>();
      matcher.Setup(m => m.MatchAllAsync(It.IsAny<IReadOnlyList<ImportedComponent>>(), It.IsAny<CancellationToken>()))
        .Returns((IReadOnlyList<ImportedComponent> _, CancellationToken ct) =>
        {
          if (blockMatching == true) Block(ct);
          return Task.FromResult(new ProjectMatchResult { Matched = [], Unresolved = [], Warnings = [] });
        });
      var services = new ServiceCollection();
      services.AddLogging();
      services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
      services.AddPresentation();
      services.AddSingleton(UserPreferences.CreateInMemory());
      services.AddSingleton(picker.Object);
      services.AddSingleton(importer.Object);
      services.AddSingleton(matcher.Object);
      services.AddSingleton(Navigation.Object);
      services.AddSingleton(Dialogs.Object);
      _provider = services.BuildServiceProvider();
      Store = _provider.GetRequiredService<ProjectStore>();
      Store.Load("Original", System.IO.Path.GetTempPath(), new ProjectMatchResult { Matched = [], Unresolved = [], Warnings = [] });
      Vm = _provider.GetRequiredService<WelcomeViewModel>();
    }

    private void Block(CancellationToken ct)
    {
      WorkerContext = SynchronizationContext.Current;
      Started.TrySetResult();
      ct.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));
      ct.ThrowIfCancellationRequested();
    }

    public void Dispose() { _provider.Dispose(); File.Delete(Path); }
  }
}
