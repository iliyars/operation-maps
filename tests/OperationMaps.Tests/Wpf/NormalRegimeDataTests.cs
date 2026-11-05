using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Models;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Wpf;
using OperationMaps.Wpf.Features.Catalog;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.ExportCheck;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Tests.Wpf;

public sealed class NormalRegimeDataTests : IDisposable
{
  private readonly SqliteConnection _connection = new("Data Source=:memory:");
  private readonly CatalogDbContext _db;
  private readonly TestCatalogDbContextFactory _factory;
  private readonly FormParameter _resource = new() { RowNumber = 1, Name = "Ресурс", Unit = "ч" };
  private readonly FormParameter _voltage = new() { RowNumber = 1, Name = "Напряжение", Unit = "В" };
  private readonly Component _component;
  private const string Relief = "Используется в облегчённом режиме при U ≤ 0,6 Uном";

  public NormalRegimeDataTests()
  {
    _connection.Open();
    _db = new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    _db.Database.EnsureCreated();
    var form4 = new Form { Number = "4", Title = "Форма 4", Parameters = [_resource] };
    var form67 = new Form { Number = "67", Title = "Форма 67", Parameters = [_voltage] };
    var family = new Family { Name = "К10-79", ComponentType = new ComponentType { Name = "Конденсатор" } };
    _component = new Component { FullName = "К10-79-25В-1мкФ", Family = family, OwnForm = form67, NeedsAdminReview = true };
    _db.AddRange(form4, form67, _component);
    _db.SaveChanges();
    _factory = TestCatalogDbContextFactory.From(_db);
  }

  private async Task AddReliefAsync(FormParameter? parameter = null)
  {
    _db.ComponentRegimes.Add(new ComponentRegime
    {
      ComponentId = _component.Id, Name = "Облегчённый режим", Condition = Relief,
      Values = [new ComponentRegimeValue { FormParameterId = (parameter ?? _resource).Id, Value = "150000" }],
    });
    await _db.SaveChangesAsync();
  }

  private Task<ProjectMatchResult> MatchAsync() => new ComponentMatcher(_factory, new ComponentNameParser()).MatchAllAsync([
    new ImportedComponent { RawName = _component.FullName, DetectedCategory = "Конденсатор", Positions = ["C1"] }
  ]);

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("—")]
  [InlineData("неизвестно")]
  [InlineData("?")]
  [InlineData("— *")]
  public async Task Relief_without_known_normal_value_requires_editing_even_for_nonrequired_parameter(string? normal)
  {
    if (normal is not null)
      _db.FamilyNtdValues.Add(new() { FamilyId = _component.FamilyId, FormParameterId = _resource.Id, Value = normal });
    await _db.SaveChangesAsync();
    await AddReliefAsync();
    var entry = Assert.Single((await MatchAsync()).Matched);
    var vm = new ProjectComponentVm(entry);
    Assert.True(vm.IsMatched);
    Assert.True(vm.RequiresEdit);
    Assert.Contains("Ресурс", Assert.Single(vm.RequiredEditReasons));
    Assert.Contains("нормального режима", vm.RequiredEditSummary);
  }

  [Fact]
  public async Task Previously_verified_component_still_requires_editing_when_normal_data_is_missing()
  {
    _component.NeedsAdminReview = false;
    await _db.SaveChangesAsync();
    await AddReliefAsync();
    var entry = Assert.Single((await MatchAsync()).Matched);
    Assert.False(entry.MatchResult.MatchedComponent!.NeedsAdminReview);
    Assert.True(new ProjectComponentVm(entry).RequiresEdit);
  }

  [Fact]
  public async Task Normal_and_relief_values_remain_independent_when_switching_conditions()
  {
    _db.FamilyNtdValues.Add(new() { FamilyId = _component.FamilyId, FormParameterId = _resource.Id, Value = "100000" });
    await _db.SaveChangesAsync();
    await AddReliefAsync();
    var component = new ProjectComponentVm(Assert.Single((await MatchAsync()).Matched));
    await component.LoadNtdValuesAsync(_db);
    Assert.False(component.RequiresEdit);
    Assert.Equal("100000", Assert.Single(component.NtdValues).Value);
    Assert.True(component.ApplyAllReliefNotes());
    Assert.Equal("150000", Assert.Single(component.NtdValues).Value);
    component.SelectRegime(null);
    Assert.Equal("100000", Assert.Single(component.NtdValues).Value);
    Assert.Equal("100000", (await _db.FamilyNtdValues.AsNoTracking().SingleAsync()).Value);
    Assert.Equal("150000", (await _db.ComponentRegimeValues.AsNoTracking().SingleAsync()).Value);
  }

  [Fact]
  public async Task Normal_value_can_be_stored_as_separate_normal_regime_for_same_parameter()
  {
    await AddReliefAsync();
    _db.ComponentRegimes.Add(new ComponentRegime
    {
      ComponentId = _component.Id, Name = "Нормальный", Condition = "При нормальном режиме",
      Values = [new ComponentRegimeValue { FormParameterId = _resource.Id, Value = "100000" }],
    });
    await _db.SaveChangesAsync();
    Assert.Empty(Assert.Single((await MatchAsync()).Matched).MatchResult.RequiredEditReasons);
    Assert.Equal(2, await _db.ComponentRegimeValues.CountAsync());
  }

  [Fact]
  public async Task Own_form_requires_its_own_normal_value_and_cannot_be_verified_until_corrected()
  {
    await AddReliefAsync(_voltage);
    var service = new ComponentEntryService(_factory);
    var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkVerifiedAsync(_component.Id));
    Assert.Contains("нормального режима", error.Message);
    Assert.True((await _db.Components.AsNoTracking().SingleAsync()).NeedsAdminReview);
    _db.ComponentNtdValues.Add(new() { ComponentId = _component.Id, FormParameterId = _voltage.Id, Value = "10" });
    await _db.SaveChangesAsync();
    await service.MarkVerifiedAsync(_component.Id);
    Assert.False((await _db.Components.AsNoTracking().SingleAsync()).NeedsAdminReview);
    Assert.Empty(Assert.Single((await MatchAsync()).Matched).MatchResult.RequiredEditReasons);
  }

  [Fact]
  public async Task Catalog_marks_missing_normal_data_and_updates_after_saving_normal_regime()
  {
    await AddReliefAsync();
    var session = new SessionStore();
    session.SignIn(new AdminAccount(1, "admin", "Администратор"));
    var catalog = new CatalogViewModel(_factory, session, new ComponentEntryService(_factory), new ComponentNameParser(),
        new CatalogEditService(_factory), Mock.Of<IDialogService>());
    try
    {
      await catalog.OnNavigatedToAsync();
      var family = catalog.Types.SelectMany(t => t.AllFamilies).Single();
      Assert.True(Assert.Single(family.Components).RequiresEdit);
      await catalog.SelectFamilyCommand.ExecuteAsync(family);
      await catalog.SelectComponentCommand.ExecuteAsync(Assert.Single(catalog.VisibleComponents));
      Assert.True(catalog.Card!.RequiresEdit);
      Assert.False(catalog.VerifyCommand.CanExecute(null));
      catalog.Regimes.NewRegimeCommand.Execute(null);
      catalog.Regimes.Name = "Нормальный режим";
      catalog.Regimes.Condition = "При нормальном режиме";
      catalog.Regimes.Values.Single(v => v.ParameterId == _resource.Id).Value = "100000";
      await catalog.Regimes.SaveCommand.ExecuteAsync(null);
      Assert.Equal("", catalog.Regimes.Error);
      Assert.False(catalog.SelectedComponent!.RequiresEdit);
      Assert.False(catalog.Card!.RequiresEdit);
      Assert.True(catalog.VerifyCommand.CanExecute(null));
      Assert.Equal(2, await _db.ComponentRegimeValues.CountAsync());
    }
    finally { await catalog.OnNavigatedFromAsync(); }
  }

  [Fact]
  public async Task Export_check_still_reports_missing_normal_value_when_relief_is_selected()
  {
    await AddReliefAsync();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
    services.AddPresentation();
    services.AddSingleton(OperationMaps.Wpf.Services.UserPreferences.CreateInMemory());
    services.AddSingleton<IDbContextFactory<CatalogDbContext>>(_factory);
    await using var provider = services.BuildServiceProvider();
    var store = provider.GetRequiredService<ProjectStore>();
    store.Load("Режимы", Path.GetTempPath(), await MatchAsync());
    var component = Assert.Single(store.Components);
    component.SelectRegime(new ComponentRegimeSelection("Облегчённый режим", Relief,
        [new RegimeParameterValue("4", 1, "150000")]));
    var issues = await provider.GetRequiredService<ProjectExportCheckService>().CheckAsync();
    Assert.True(component.RequiresEdit);
    Assert.Contains(issues, i => i.Target == ExportIssueTarget.Components && i.Message.Contains("нормального режима") && i.Message.Contains("Ресурс"));
  }

  public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
