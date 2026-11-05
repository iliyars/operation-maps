using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf;
using OperationMaps.Wpf.Features.Catalog;
using OperationMaps.Wpf.Features.Form4;
using OperationMaps.Wpf.Services;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Tests.Wpf;

public sealed class CatalogAndForm4LoadingTests : IDisposable
{
  private readonly SqliteConnection _connection = new("DataSource=:memory:");
  private readonly ServiceProvider _provider;
  private readonly DelayedFactory _factory;
  private readonly Family _first;
  private readonly Family _second;

  public CatalogAndForm4LoadingTests()
  {
    _connection.Open();
    var options = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options;
    using var db = new CatalogDbContext(options);
    db.Database.EnsureCreated();
    var type = new ComponentType { Name = "Конденсатор" };
    _first = new Family { Name = "Первое", ComponentType = type };
    _second = new Family { Name = "Второе", ComponentType = type };
    var row = new FormParameter { RowNumber = 1, Name = "Ресурс" };
    var form = new Form { Number = "4", Title = "Форма 4", Parameters = [row] };
    db.AddRange(form, _first, _second,
      new Component { FullName = "Первый компонент", Family = _first },
      new Component { FullName = "Второй компонент", Family = _second });
    db.SaveChanges();
    db.FamilyNtdValues.AddRange(
      new FamilyNtdValue { Family = _first, FormParameter = row, Value = "100" },
      new FamilyNtdValue { Family = _second, FormParameter = row, Value = "200" });
    db.SaveChanges();
    _factory = new(options);
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
    services.AddPresentation();
    services.AddSingleton<IDbContextFactory<CatalogDbContext>>(_factory);
    services.AddSingleton(UserPreferences.CreateInMemory());
    services.AddSingleton(Mock.Of<IDialogService>());
    _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Late_family_or_component_card_does_not_replace_new_family(bool selectComponent)
  {
    var vm = _provider.GetRequiredService<CatalogViewModel>();
    await vm.OnNavigatedToAsync();
    var first = FamilyNode(vm, _first.Id);
    var second = FamilyNode(vm, _second.Id);
    var held = _factory.HoldNext();
    var old = selectComponent ? vm.SelectComponentCommand.ExecuteAsync(first.Components[0])
      : vm.SelectFamilyCommand.ExecuteAsync(first);
    Assert.True(vm.SelectFamilyCommand.CanExecute(second));
    await vm.SelectFamilyCommand.ExecuteAsync(second);
    var expected = vm.Card;
    held.Complete();
    await old.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Same(expected, vm.Card);
    Assert.Equal(_second.Id, vm.Card!.EntityId);
    Assert.Null(vm.SelectedComponent);
    Assert.Null(vm.SelectionError);
    await vm.OnNavigatedFromAsync();
  }

  [Fact]
  public async Task Returning_to_same_family_ignores_old_error_and_current_errors_are_visible()
  {
    var vm = _provider.GetRequiredService<CatalogViewModel>();
    await vm.OnNavigatedToAsync();
    var first = FamilyNode(vm, _first.Id);
    var held = _factory.HoldNext();
    var old = vm.SelectFamilyCommand.ExecuteAsync(first);
    await vm.SelectFamilyCommand.ExecuteAsync(FamilyNode(vm, _second.Id));
    await vm.SelectFamilyCommand.ExecuteAsync(first);
    held.Ready.SetException(new IOException("Старая ошибка"));
    await old;
    Assert.Equal(_first.Id, vm.Card!.EntityId);
    Assert.Null(vm.SelectionError);
    held = _factory.HoldNext();
    var failed = vm.SelectFamilyCommand.ExecuteAsync(first);
    held.Ready.SetException(new IOException("Нет доступа"));
    await failed;
    Assert.Null(vm.Card);
    Assert.Contains("Нет доступа", vm.SelectionError);
    await vm.SelectFamilyCommand.ExecuteAsync(first);
    Assert.Null(vm.SelectionError);
    Assert.NotNull(vm.Card);
    await vm.OnNavigatedFromAsync();
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Leaving_catalog_blocks_late_card_and_tree_results(bool fail)
  {
    var vm = _provider.GetRequiredService<CatalogViewModel>();
    var held = _factory.HoldNext();
    var oldTree = vm.OnNavigatedToAsync();
    await vm.OnNavigatedFromAsync();
    await vm.OnNavigatedToAsync();
    var types = vm.Types;
    if (fail) held.Ready.SetException(new IOException("Поздняя ошибка дерева"));
    else held.Complete();
    await oldTree;
    Assert.Same(types, vm.Types);
    Assert.False(vm.IsLoading);
    held = _factory.HoldNext();
    var oldCard = vm.SelectFamilyCommand.ExecuteAsync(FamilyNode(vm, _first.Id));
    await vm.OnNavigatedFromAsync();
    held.Ready.SetException(new IOException("Поздняя ошибка карточки"));
    await oldCard;
    Assert.Null(vm.Card);
    Assert.Null(vm.SelectionError);
    Assert.False(vm.Regimes.IsAvailable);
  }

  [Theory]
  [InlineData(false, false)]
  [InlineData(true, false)]
  [InlineData(false, true)]
  [InlineData(true, true)]
  public async Task Late_form4_build_does_not_publish_after_leave_or_project_replacement(bool replaceProject, bool fail)
  {
    LoadProject(_first);
    var vm = _provider.GetRequiredService<Form4ViewModel>();
    var held = _factory.HoldNext();
    var old = vm.OnNavigatedToAsync();
    Assert.True(vm.IsLoading);
    Assert.False(vm.ExportWordCommand.CanExecute(null));
    if (replaceProject)
    {
      LoadProject(_second);
      await vm.OnNavigatedToAsync();
      Assert.Equal(_second.Name, Assert.Single(vm.Groups).DisplayName);
    }
    else await vm.OnNavigatedFromAsync();
    if (fail) held.Ready.SetException(new IOException("Поздняя ошибка формы"));
    else held.Complete();
    await old.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.False(vm.IsLoading);
    if (replaceProject)
    {
      Assert.Equal(_second.Name, Assert.Single(vm.Groups).DisplayName);
      Assert.Equal("200", Assert.Single(vm.BuildWordFormData().Components).NtdValues[1]);
    }
    else
    {
      Assert.Empty(vm.Groups);
      Assert.Empty(vm.Parameters);
      Assert.Throws<InvalidOperationException>(() => vm.BuildWordFormData());
      await vm.OnNavigatedToAsync();
      Assert.Single(vm.Groups);
    }
  }

  [Fact]
  public async Task Late_regime_preview_cannot_replace_preview_of_new_family()
  {
    var vm = _provider.GetRequiredService<CatalogViewModel>();
    await vm.OnNavigatedToAsync();
    Request? held = null;
    vm.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName == nameof(CatalogViewModel.Card) && vm.Card?.EntityId == _first.Id)
        held = _factory.HoldNext();
    };
    var old = vm.SelectFamilyCommand.ExecuteAsync(FamilyNode(vm, _first.Id));
    Assert.NotNull(held);
    Assert.False(old.IsCompleted);
    await vm.SelectFamilyCommand.ExecuteAsync(FamilyNode(vm, _second.Id));
    Assert.True(vm.Regimes.IsAvailable);
    Assert.Equal("200", Assert.Single(vm.Regimes.Values).DefaultValue);
    held.Complete();
    await old;
    Assert.Equal(_second.Id, vm.Card!.EntityId);
    Assert.True(vm.Regimes.IsAvailable);
    Assert.Equal("200", Assert.Single(vm.Regimes.Values).DefaultValue);
    await vm.OnNavigatedFromAsync();
  }

  [Fact]
  public async Task Project_replacement_without_new_build_cannot_export_old_groups()
  {
    LoadProject(_first);
    var vm = _provider.GetRequiredService<Form4ViewModel>();
    await vm.OnNavigatedToAsync();
    Assert.Single(vm.Groups);
    LoadProject(_second);
    Assert.False(vm.ExportWordCommand.CanExecute(null));
    Assert.Throws<InvalidOperationException>(() => vm.BuildWordFormData());
    var held = _factory.HoldNext();
    var old = vm.OnNavigatedToAsync();
    LoadProject(_first);
    held.Complete();
    await old;
    Assert.Empty(vm.Groups);
    Assert.Empty(vm.Parameters);
    Assert.Throws<InvalidOperationException>(() => vm.BuildWordFormData());
  }

  private void LoadProject(Family family)
    => _provider.GetRequiredService<ProjectStore>().Load(family.Name, Path.GetTempPath(), new ProjectMatchResult
    {
      Matched = [new ComponentMatchEntry
      {
        Imported = new ImportedComponent { RawName = family.Name, DetectedCategory = "Конденсатор", Positions = ["C1"] },
        MatchResult = new MatchResult { IsMatched = false, MatchedFamily = family },
      }], Unresolved = [], Warnings = [],
    });

  private static CatalogFamilyNode FamilyNode(CatalogViewModel vm, int id)
    => vm.Types.SelectMany(t => t.AllFamilies).Single(f => f.Id == id);

  public void Dispose()
  {
    _provider.Dispose();
    _connection.Dispose();
  }

  private sealed class DelayedFactory(DbContextOptions<CatalogDbContext> options) : IDbContextFactory<CatalogDbContext>
  {
    private Request? _next;
    public Request HoldNext() => _next = new(options);
    public CatalogDbContext CreateDbContext() => new(options);
    public Task<CatalogDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
      if (_next is not { } request) return Task.FromResult(CreateDbContext());
      _next = null;
      return request.Ready.Task;
    }
  }

  private sealed class Request(DbContextOptions<CatalogDbContext> options)
  {
    public TaskCompletionSource<CatalogDbContext> Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public void Complete() => Ready.SetResult(new(options));
  }
}
