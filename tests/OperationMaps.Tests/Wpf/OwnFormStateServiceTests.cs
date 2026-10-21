using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Features.OwnForm.Commands;
using OperationMaps.Wpf.Stores;
using Xunit;

namespace OperationMaps.Tests.Wpf;

/// <summary>
/// Own-form table state must outlive the (transient) OwnFormViewModel:
/// before it moved into ProjectStore, every edit was lost as soon as the
/// user navigated to another form and back, and the final report only saw
/// the form that happened to be on screen.
/// </summary>
public sealed class OwnFormStateServiceTests : IDisposable
{
  private readonly SqliteConnection _connection;
  private readonly CatalogDbContext _db;
  private readonly ProjectStore _store = new();

  private Form _form = null!;
  private Family _family = null!;
  private int _baseParamId;
  private int _resultParamId;

  public OwnFormStateServiceTests()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();

    _db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
        .UseSqlite(_connection)
        .Options);
    _db.Database.EnsureCreated();

    Seed();
  }

  private void Seed()
  {
    var type = new ComponentType { Name = "Резистор" };
    _family = new Family { Name = "Р1-12", ComponentType = type };

    var baseParam = new FormParameter { RowNumber = 1, Name = "Напряжение", CanBeLoadFactorBase = true };
    var resultParam = new FormParameter { RowNumber = 2, Name = "Коэффициент нагрузки", IsLoadFactorResult = true };
    _form = new Form { Number = "99", Title = "Тестовая форма", Parameters = [baseParam, resultParam] };

    _db.AddRange(type, _family, _form);
    _db.SaveChanges();

    _baseParamId = baseParam.Id;
    _resultParamId = resultParam.Id;
  }

  /// <summary>Adds a catalog component with NTD value "10" for the base parameter, plus its project entry.</summary>
  private ProjectComponentVm AddProjectComponent(string fullName, params string[] positions)
  {
    var component = new Component { FullName = fullName, Family = _family, OwnFormId = _form.Id };
    _db.Components.Add(component);
    _db.SaveChanges();

    _db.ComponentNtdValues.Add(new ComponentNtdValue
    {
      ComponentId = component.Id,
      FormParameterId = _baseParamId,
      Value = "10",
    });
    _db.SaveChanges();

    var vm = new ProjectComponentVm(new ComponentMatchEntry
    {
      Imported = new ImportedComponent
      {
        ImportIndex = _store.Components.Count,
        RawName = fullName,
        DetectedCategory = "Резистор",
        Positions = positions,
      },
      MatchResult = new MatchResult
      {
        IsMatched = true,
        MatchedFamily = _family,
        MatchedComponent = component,
      },
    });
    _store.Components.Add(vm);
    return vm;
  }

  // Each navigation resolves a fresh service, exactly like the transient ViewModel does.
  private Task<OwnFormState?> OpenFormAsync() => new OwnFormStateService(_store, _db).GetAsync(_form.Id);

  [Fact]
  public async Task EditsSurviveReopeningTheForm()
  {
    AddProjectComponent("Р1-12-0,125-1 кОм", "R1", "R2");

    var first = await OpenFormAsync();
    first!.Columns[0].SetCellValue(_baseParamId, "5");

    var reopened = await OpenFormAsync();

    Assert.Same(first, reopened);
    Assert.Equal("5", reopened!.Columns[0].GetCellValue(_baseParamId));
    Assert.Equal("10", reopened.Columns[0].GetNtdValue(_baseParamId));
  }

  [Fact]
  public async Task SplitAndUndoHistorySurviveReopeningTheForm()
  {
    AddProjectComponent("Р1-12-0,125-1 кОм", "R1", "R2");

    var state = await OpenFormAsync();
    state!.History.Execute(new SplitColumnCommand(state.Columns, state.Columns[0], ["R1"], ["R2"]));

    var reopened = await OpenFormAsync();

    Assert.Equal(2, reopened!.Columns.Count);
    Assert.True(reopened.History.CanUndo);

    reopened.History.Undo();
    Assert.Single(reopened.Columns);
  }

  [Fact]
  public async Task LoadFactorIsRecalculatedOnSchemeValueChange()
  {
    AddProjectComponent("Р1-12-0,125-1 кОм", "R1");

    var state = await OpenFormAsync();
    state!.Columns[0].SetCellValue(_baseParamId, "5");

    Assert.Equal("0,5 (1)", state.Columns[0].GetCellValue(_resultParamId));
  }

  [Fact]
  public async Task LoadFactorIsRecalculatedForColumnsCreatedBySplit()
  {
    // Split clones columns; previously nobody subscribed the clones to
    // load-factor recalculation, so their result row silently went stale.
    AddProjectComponent("Р1-12-0,125-1 кОм", "R1", "R2");

    var state = await OpenFormAsync();
    state!.History.Execute(new SplitColumnCommand(state.Columns, state.Columns[0], ["R1"], ["R2"]));

    state.Columns[1].SetCellValue(_baseParamId, "2");

    Assert.Equal("0,2 (1)", state.Columns[1].GetCellValue(_resultParamId));
  }

  [Fact]
  public async Task ComponentMatchedLaterIsAddedWithoutLosingExistingEdits()
  {
    AddProjectComponent("Р1-12-0,125-1 кОм", "R1");

    var state = await OpenFormAsync();
    state!.Columns[0].SetCellValue(_baseParamId, "5");

    // e.g. resolved via the "add component" wizard after the form was first opened
    AddProjectComponent("Р1-12-0,25-2 кОм", "R5");

    var reopened = await OpenFormAsync();

    Assert.Equal(2, reopened!.Columns.Count);
    Assert.Equal("5", reopened.Columns[0].GetCellValue(_baseParamId));
    Assert.Equal("R5", reopened.Columns[1].Positions);
  }

  [Fact]
  public async Task ClosingTheProjectDropsFormState()
  {
    AddProjectComponent("Р1-12-0,125-1 кОм", "R1");
    await OpenFormAsync();

    _store.Clear();

    Assert.Empty(_store.OwnForms);
  }

  public void Dispose()
  {
    _db.Dispose();
    _connection.Dispose();
  }
}
