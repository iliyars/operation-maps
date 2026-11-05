using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Services;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Tests.Wpf;

public sealed class ComponentSelectionLoadingTests : IDisposable
{
  private readonly SqliteConnection _connection = new("DataSource=:memory:");
  private readonly DbContextOptions<CatalogDbContext> _options;
  private readonly DelayedFactory _factory = new();
  private readonly ProjectStore _store = new();
  private readonly ComponentsViewModel _vm;

  public ComponentSelectionLoadingTests()
  {
    _connection.Open();
    _options = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options;
    using var db = new CatalogDbContext(_options);
    db.Database.EnsureCreated();
    db.Forms.Add(new Form { Number = "4", Title = "Форма 4", Parameters = [new FormParameter { RowNumber = 1, Name = "Ресурс" }] });
    db.SaveChanges();
    _vm = new(_store, _factory, Mock.Of<IComponentEntryService>(), new ComponentNameParser(), UserPreferences.CreateInMemory());
  }

  [Fact]
  public async Task Rapid_reselection_loads_latest_component_and_observes_late_errors()
  {
    var first = Component("C1");
    var second = Component("C2");
    _vm.SelectedComponent = first;
    var old = _vm.SelectionLoadTask;
    _vm.SelectedComponent = second;
    var middle = _vm.SelectionLoadTask;
    _vm.SelectedComponent = first;
    var latest = _vm.SelectionLoadTask;
    _factory.Requests[2].Ready.SetResult(new(_options));
    await latest.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.True(first.NtdLoaded);
    _factory.Requests[0].Ready.SetException(new IOException("Устаревшая ошибка"));
    _factory.Requests[1].Ready.SetResult(new(_options));
    await Task.WhenAll(old, middle).WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Same(first, _vm.SelectedComponent);
    Assert.Null(_vm.SelectionError);
    Assert.True(_factory.Requests[0].Token.IsCancellationRequested);
    Assert.True(_factory.Requests[1].Token.IsCancellationRequested);
    Assert.False(second.NtdLoaded);
    Assert.Single(first.NtdValues);
  }

  [Fact]
  public async Task Leaving_cancels_load_and_returning_retries_same_selection()
  {
    var component = Component("C1");
    _vm.SelectedComponent = component;
    var old = _vm.SelectionLoadTask;
    await _vm.OnNavigatedFromAsync();
    _factory.Requests[0].Ready.SetResult(new(_options));
    await old.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(component.NtdLoaded);
    Assert.Null(_vm.SelectionError);
    var returning = _vm.OnNavigatedToAsync();
    _factory.Requests[1].Ready.SetResult(new(_options));
    await returning.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.True(component.NtdLoaded);
    Assert.False(component.IsLoadingNtd);
  }

  [Fact]
  public async Task Current_error_is_visible_and_clears_on_next_selection()
  {
    _vm.SelectedComponent = Component("C1");
    _factory.Requests[0].Ready.SetException(new IOException("Нет доступа к каталогу"));
    await _vm.SelectionLoadTask;
    Assert.Contains("Нет доступа", _vm.SelectionError);
    _vm.SelectedComponent = Component("C2");
    _factory.Requests[1].Ready.SetResult(new(_options));
    await _vm.SelectionLoadTask;
    Assert.Null(_vm.SelectionError);
  }

  [Fact]
  public async Task Collection_notifications_stop_on_leave_and_resume_once_after_repeated_returns()
  {
    var notifications = 0;
    _vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ComponentsViewModel.CountAll)) notifications++; };
    _store.Components.Add(Component("C1"));
    Assert.Equal(1, notifications);
    await _vm.OnNavigatedFromAsync();
    _store.Components.Add(Component("C2"));
    Assert.Equal(1, notifications);
    await _vm.OnNavigatedToAsync();
    await _vm.OnNavigatedToAsync();
    notifications = 0;
    _store.Components.Add(Component("C3"));
    Assert.Equal(1, notifications);
  }

  private static ProjectComponentVm Component(string position) => new(new ComponentMatchEntry
  {
    Imported = new ImportedComponent { RawName = "Компонент", DetectedCategory = "Конденсатор", Positions = [position] },
    MatchResult = new MatchResult { IsMatched = false },
  });

  public void Dispose()
  {
    _vm.OnNavigatedFromAsync().GetAwaiter().GetResult();
    _connection.Dispose();
  }

  private sealed class DelayedFactory : IDbContextFactory<CatalogDbContext>
  {
    public List<Request> Requests { get; } = [];
    public CatalogDbContext CreateDbContext() => throw new NotSupportedException();
    public Task<CatalogDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
      var request = new Request(cancellationToken);
      Requests.Add(request);
      return request.Ready.Task;
    }
  }

  private sealed record Request(CancellationToken Token)
  {
    public TaskCompletionSource<CatalogDbContext> Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
  }
}
