using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.Components.WordImport;
using OperationMaps.Wpf.Infrastructure.Navigation;

namespace OperationMaps.Tests.Wpf;

public sealed class BackgroundWordImportTests : IDisposable
{
  private readonly SqliteConnection _connection = new("DataSource=:memory:");
  private readonly CatalogDbContext _db;
  private readonly BlockingImporter _importer = new();
  private readonly WordImportViewModel _vm;

  public BackgroundWordImportTests()
  {
    _connection.Open();
    _db = new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    _db.Database.EnsureCreated();
    _vm = new(_importer, TestCatalogDbContextFactory.From(_db), Mock.Of<INavigationService>());
  }

  [Fact]
  public async Task Slow_synchronous_scan_does_not_block_screen_and_can_be_cancelled()
  {
    _importer.BlockScan = true;
    var loading = _vm.OnNavigatedToAsync("large.docx");
    try
    {
      await _importer.ScanStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.False(loading.IsCompleted);
      Assert.True(_vm.IsBusy);
      Assert.True(_vm.CancelOperationCommand.CanExecute(null));
      Assert.Null(_importer.ScanContext);
      _vm.CancelOperationCommand.Execute(null);
      await loading.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.False(_vm.IsBusy);
      Assert.Null(_vm.ErrorMessage);
      Assert.Empty(_vm.Rows);
      Assert.Contains("отменено", _vm.ResultSummary);
    }
    finally { _vm.CancelOperationCommand.Execute(null); _importer.ScanGate.Set(); await loading; }
  }

  [Fact]
  public async Task Cancelling_import_keeps_completed_components_and_does_not_mark_cancelled_row_as_error()
  {
    await _vm.OnNavigatedToAsync("two.docx");
    Assert.Equal(2, _vm.Rows.Count);
    _importer.BlockImportNumber = 2;
    var saving = _vm.ImportSelectedCommand.ExecuteAsync(null);
    try
    {
      await _importer.ImportBlocked.Task.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.True(_vm.IsBusy);
      Assert.Null(_importer.ImportContext);
      Assert.True(_vm.Rows[0].IsImported);
      Assert.False(_vm.Rows[1].IsImported);
      _vm.CancelOperationCommand.Execute(null);
      await saving.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.False(_vm.IsBusy);
      Assert.Null(_vm.Rows[1].ImportError);
      Assert.Contains("Сохранено компонентов: 1", _vm.ResultSummary);
      Assert.Equal(2, _importer.ImportCalls);
      Assert.True(_vm.CanImportSelected);
    }
    finally { _vm.CancelOperationCommand.Execute(null); _importer.ImportGate.Set(); await saving; }
  }

  [Fact]
  public async Task Navigating_away_cancels_reading_and_does_not_publish_rows_later()
  {
    _importer.BlockScan = true;
    var loading = _vm.OnNavigatedToAsync("large.docx");
    try
    {
      await _importer.ScanStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
      await _vm.OnNavigatedFromAsync();
      await loading.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.Empty(_vm.Rows);
      Assert.False(_vm.IsBusy);
      Assert.Null(_vm.ErrorMessage);
    }
    finally { _importer.ScanGate.Set(); await loading; }
  }

  [Fact]
  public async Task Navigation_publishes_loading_screen_before_initialization_finishes()
  {
    var services = new ServiceCollection();
    services.AddSingleton<DelayedScreen>();
    using var provider = services.BuildServiceProvider();
    using var navigation = new NavigationService(provider);
    IScreen? published = null;
    navigation.CurrentScreenChanged += (_, args) => published = navigation.CurrentScreen;
    var navigating = navigation.NavigateAsync<DelayedScreen>();
    var screen = provider.GetRequiredService<DelayedScreen>();
    Assert.Same(screen, published);
    Assert.False(navigating.IsCompleted);
    screen.Ready.SetResult();
    await navigating;
  }

  public void Dispose()
  {
    _vm.OnNavigatedFromAsync().GetAwaiter().GetResult();
    _importer.ScanGate.Dispose();
    _importer.ImportGate.Dispose();
    _db.Dispose();
    _connection.Dispose();
  }

  private sealed class DelayedScreen : IScreen, INavigatedTo
  {
    public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default) => Ready.Task;
  }

  private sealed class BlockingImporter : IWordCatalogImportService
  {
    public bool BlockScan { get; set; }
    public int BlockImportNumber { get; set; }
    public int ImportCalls { get; private set; }
    public SynchronizationContext? ScanContext { get; private set; }
    public SynchronizationContext? ImportContext { get; private set; }
    public TaskCompletionSource ScanStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ImportBlocked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public ManualResetEventSlim ScanGate { get; } = new(false);
    public ManualResetEventSlim ImportGate { get; } = new(false);

    public Task<WordImportScanResult> ScanAsync(string documentPath, CancellationToken ct = default)
    {
      ScanContext = SynchronizationContext.Current;
      ScanStarted.TrySetResult();
      if (BlockScan) ScanGate.Wait(ct);
      return Task.FromResult(new WordImportScanResult { Components = Enumerable.Range(1, 2).Select(i => new WordImportedComponent
      {
        FullName = $"Компонент {i}", OwnFormId = 1, OwnFormNumber = "67", OwnFormTitle = "Форма 67", ComponentTypeName = "Конденсатор"
      }).ToArray() });
    }

    public Task<Component> ImportAsync(WordImportedComponent item, CancellationToken ct = default)
    {
      ImportContext = SynchronizationContext.Current;
      ImportCalls++;
      if (ImportCalls == BlockImportNumber) { ImportBlocked.TrySetResult(); ImportGate.Wait(ct); }
      return Task.FromResult(new Component { Id = ImportCalls, FullName = item.FullName });
    }

    public Task ResolveComponentTypeAsync(WordImportedComponent item, string name, CancellationToken ct = default) => Task.CompletedTask;
  }
}
