using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf;
using OperationMaps.Wpf.Features.ExportCheck;
using OperationMaps.Wpf.Features.Overview;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Tests.Wpf;

public sealed class ProjectOverviewTests : IDisposable
{
  private readonly SqliteConnection _connection = new("Data Source=:memory:");
  private readonly ServiceProvider _provider;
  private readonly ProjectStore _store;
  private readonly ProjectOverviewViewModel _overview;
  private readonly OwnFormState _state;

  public ProjectOverviewTests()
  {
    _connection.Open();
    using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    db.Database.EnsureCreated();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
    services.AddPresentation();
    services.AddSingleton(OperationMaps.Wpf.Services.UserPreferences.CreateInMemory());
    services.AddSingleton<IDbContextFactory<CatalogDbContext>>(TestCatalogDbContextFactory.From(db));
    var registration = new Moq.Mock<IRegistrationSheetService>();
    registration.SetupGet(r => r.TemplateExists).Returns(true);
    services.AddSingleton(registration.Object);
    _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    _store = _provider.GetRequiredService<ProjectStore>();
    var entry = Entry(67, "C1");
    _store.Load("Плата", Path.GetTempPath(), new ProjectMatchResult { Matched = [entry], Unresolved = [], Warnings = [] });
    _store.DocumentTitle = "Плата управления";
    _store.DocumentNumber = "ДЕМО.001";
    _store.HeadName = "Иванов И.И.";
    _store.HeadOccupation = "Директор";
    _state = AddForm(67);
    _overview = _provider.GetRequiredService<ProjectOverviewViewModel>();
  }

  private static ComponentMatchEntry Entry(int formId, string position) => new()
  {
    Imported = new ImportedComponent { RawName = position, DetectedCategory = "Конденсатор", Positions = [position] },
    MatchResult = new MatchResult
    {
      IsMatched = true,
      MatchedComponent = new Component { Id = formId, FullName = position, OwnFormId = formId },
      RequiredForms = [new Form { Id = formId, Number = formId.ToString(), Title = "Рабочие режимы" }],
    },
  };

  private OwnFormState AddForm(int id)
  {
    var state = new OwnFormState(id, id.ToString(), "Рабочие режимы", [], null, null);
    state.SetParameters([new FormParameterRowVm(new FormParameter
    {
      Id = 1, RowNumber = 1, Name = "Напряжение", IsRequired = true,
    }, state.Columns)]);
    var component = _store.Components.Single(c => c.Entry.MatchResult.MatchedComponent!.OwnFormId == id);
    var column = new FormColumnVm(component);
    column.NtdValues[1] = "10";
    state.Columns.Add(column);
    _store.AddOwnForm(state);
    return state;
  }

  [Fact]
  public async Task Overview_uses_existing_form_state_and_refreshes_after_edits()
  {
    await _overview.OnNavigatedToAsync();
    Assert.Equal("0 из 1", _overview.FieldProgress);
    Assert.True(_overview.HasIssues);
    var form = Assert.Single(_overview.Forms);
    Assert.True(form.HasTemplate);
    Assert.Equal(0, form.FilledGroups);
    Assert.Contains(_overview.Tasks, t => t.Issue.Target == ExportIssueTarget.OwnForm && t.Issue.FormId == 67);
    Assert.False(_store.IsDirty);

    _state.Columns[0].SetCellValue(1, "5");
    await _overview.OnNavigatedToAsync();
    Assert.Equal("1 из 1", _overview.FieldProgress);
    Assert.Equal(1, Assert.Single(_overview.Forms).FilledGroups);
    Assert.False(_overview.HasIssues);
    Assert.Equal("Проект готов к экспорту", _overview.Readiness);
    Assert.True(_store.IsDirty);
  }

  [Fact]
  public async Task Similar_component_issues_are_grouped_into_one_action()
  {
    foreach (var position in new[] { "X1", "X2" })
      _store.Components.Add(new(new ComponentMatchEntry
      {
        Imported = new ImportedComponent { RawName = position, Positions = [position], DetectedCategory = "Неизвестно" },
        MatchResult = new MatchResult { IsMatched = false },
      }));
    await _overview.OnNavigatedToAsync();
    var task = Assert.Single(_overview.Tasks, t => t.Issue.Target == ExportIssueTarget.Components);
    Assert.Equal(2, task.Count);
    Assert.Equal("Перейти к компонентам", _overview.ContinueLabel);
    await _overview.OpenTaskCommand.ExecuteAsync(task);
    Assert.IsType<OperationMaps.Wpf.Features.Components.ComponentsViewModel>(
        _provider.GetRequiredService<INavigationService>().CurrentScreen);
  }

  [Fact]
  public async Task Continue_returns_to_last_form_instead_of_first_incomplete_form()
  {
    _store.Components.Add(new(Entry(68, "R1")));
    AddForm(68);
    var navigation = _provider.GetRequiredService<INavigationService>();
    await navigation.NavigateAsync<OwnFormViewModel>(68);
    Assert.Equal(68, _store.LastOwnFormId);
    await _overview.OnNavigatedToAsync();
    Assert.Equal("Продолжить форму 68", _overview.ContinueLabel);
    await _overview.ContinueCommand.ExecuteAsync(null);
    Assert.Equal(68, Assert.IsType<OwnFormViewModel>(navigation.CurrentScreen).FormId);

    _store.Load("Другой проект", Path.GetTempPath(), new ProjectMatchResult { Matched = [], Unresolved = [], Warnings = [] });
    Assert.Null(_store.LastOwnFormId);
  }

  public void Dispose()
  {
    _provider.Dispose();
    _connection.Dispose();
  }
}
