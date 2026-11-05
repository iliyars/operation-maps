using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OperationMaps.Application.Importing;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.Components;

namespace OperationMaps.Tests.Wpf;

public sealed class ProjectComponentLoadingTests : IDisposable
{
  private readonly SqliteConnection _connection = new("DataSource=:memory:");
  private readonly QueryGate _gate = new();
  private readonly DbContextOptions<CatalogDbContext> _options;
  private readonly Family _first;
  private readonly Family _second;

  public ProjectComponentLoadingTests()
  {
    _connection.Open();
    _options = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).AddInterceptors(_gate).Options;
    using var db = new CatalogDbContext(_options);
    db.Database.EnsureCreated();
    var type = new ComponentType { Name = "Конденсатор" };
    _first = new Family { Name = "Первое", ComponentType = type };
    _second = new Family { Name = "Второе", ComponentType = type };
    var row = new FormParameter { RowNumber = 1, Name = "Ресурс" };
    db.AddRange(_first, _second, new Form { Number = "4", Title = "Форма 4", Parameters = [row] });
    db.SaveChanges();
    db.FamilyNtdValues.AddRange(
      new FamilyNtdValue { Family = _first, FormParameter = row, Value = "100" },
      new FamilyNtdValue { Family = _second, FormParameter = row, Value = "200" });
    db.SaveChanges();
    _gate.Armed = true;
  }

  [Fact]
  public async Task Second_caller_waits_for_loading_instead_of_reading_empty_data()
  {
    var component = Component();
    await using var firstDb = new CatalogDbContext(_options);
    await using var secondDb = new CatalogDbContext(_options);
    var first = component.LoadNtdValuesAsync(firstDb);
    await _gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
    var second = component.LoadNtdValuesAsync(secondDb);
    Assert.False(second.IsCompleted);
    _gate.Ready.SetResult();
    await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Equal("100", Assert.Single(component.NtdValues).Value);
    Assert.True(component.NtdLoaded);
  }

  [Fact]
  public async Task Reselecting_component_waits_for_cancelled_query_then_loads_fresh_data()
  {
    var component = Component();
    await using var firstDb = new CatalogDbContext(_options);
    await using var secondDb = new CatalogDbContext(_options);
    using var cancellation = new CancellationTokenSource();
    var first = component.LoadNtdValuesAsync(firstDb, cancellation.Token);
    await _gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
    cancellation.Cancel();
    var second = component.LoadNtdValuesAsync(secondDb);
    Assert.False(second.IsCompleted);
    _gate.Ready.SetResult();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
    await second.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Equal("100", Assert.Single(component.NtdValues).Value);
    Assert.True(component.NtdLoaded);
    Assert.False(component.IsLoadingNtd);
  }

  [Fact]
  public async Task Rematching_during_query_prevents_old_family_values_from_being_applied()
  {
    var component = Component();
    await using var db = new CatalogDbContext(_options);
    var loading = component.LoadNtdValuesAsync(db);
    await _gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
    component.ApplyNewMatch(new MatchResult { IsMatched = false, MatchedFamily = _second });
    _gate.Ready.SetResult();
    await loading.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(component.NtdLoaded);
    Assert.Empty(component.NtdValues);
    await component.LoadNtdValuesAsync(db);
    Assert.Equal("200", Assert.Single(component.NtdValues).Value);
  }

  private ProjectComponentVm Component() => new(new ComponentMatchEntry
  {
    Imported = new ImportedComponent { RawName = "Компонент", DetectedCategory = "Конденсатор", Positions = ["C1"] },
    MatchResult = new MatchResult { IsMatched = false, MatchedFamily = _first },
  });

  public void Dispose()
  {
    _gate.Ready.TrySetResult();
    _connection.Dispose();
  }

  private sealed class QueryGate : DbCommandInterceptor
  {
    private int _blocked;
    public bool Armed { get; set; }
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
      DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
      CancellationToken cancellationToken = default)
    {
      if (Armed && Interlocked.Exchange(ref _blocked, 1) == 0)
      {
        Started.SetResult();
        await Ready.Task;
      }
      return result;
    }
  }
}
