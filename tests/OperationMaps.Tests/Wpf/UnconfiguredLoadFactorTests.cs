using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Importing;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Stores;
using Xunit;

namespace OperationMaps.Tests.Wpf;

/// <summary>
/// Forms whose load-factor base isn't known yet (58/60/75/77 and most others):
/// the user picks the base per component, an admin can store a default.
/// </summary>
public sealed class UnconfiguredLoadFactorTests : IDisposable
{
  private readonly SqliteConnection _connection;
  private readonly CatalogDbContext _db;
  private readonly ProjectStore _store = new();
  private Form _form = null!;
  private int _powerId, _currentId, _resultId;

  public UnconfiguredLoadFactorTests()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();
    _db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    _db.Database.EnsureCreated();

    // No CanBeLoadFactorBase / IsLoadFactorResult flags — just the printed row names.
    var power = new FormParameter { RowNumber = 1, Name = "Средняя мощность", Unit = "Вт" };
    var current = new FormParameter { RowNumber = 2, Name = "Ток коллектора", Unit = "А" };
    var result = new FormParameter { RowNumber = 3, Name = "Коэффициент нагрузки" };
    _form = new Form { Number = "58", Title = "Транзисторы", Parameters = [power, current, result] };

    var type = new ComponentType { Name = "Транзистор" };
    var family = new Family { Name = "2Т", ComponentType = type };
    _db.AddRange(_form, type, family);
    _db.SaveChanges();
    (_powerId, _currentId, _resultId) = (power.Id, current.Id, result.Id);

    foreach (var (name, position) in new[] { ("2Т630А", "VT1"), ("2Т630Б", "VT2") })
    {
      var component = new Component { FullName = name, Family = family, OwnFormId = _form.Id };
      _db.Components.Add(component);
      _db.SaveChanges();
      _db.ComponentNtdValues.AddRange(
          new ComponentNtdValue { ComponentId = component.Id, FormParameterId = power.Id, Value = "10" },
          new ComponentNtdValue { ComponentId = component.Id, FormParameterId = current.Id, Value = "2" });
      _db.SaveChanges();

      _store.Components.Add(new ProjectComponentVm(new ComponentMatchEntry
      {
        Imported = new ImportedComponent { RawName = name, DetectedCategory = "Транзистор", Positions = [position] },
        MatchResult = new MatchResult { IsMatched = true, MatchedFamily = family, MatchedComponent = component },
      }));
    }
  }

  private Task<OwnFormState?> OpenAsync(ProjectStore? store = null)
      => new OwnFormStateService(store ?? _store, _db).GetAsync(_form.Id);

  [Fact]
  public async Task User_picks_the_base_parameter_per_component()
  {
    var state = (await OpenAsync())!;

    Assert.Equal([_powerId, _currentId], state.LoadFactorOptions.Select(o => o.FormParameterId));
    Assert.Null(state.DefaultLoadFactorParameterId);
    Assert.True(state.Parameters.Single(p => p.FormParameterId == _resultId).IsLoadFactorResult); // read-only row

    var column = state.Columns[0];
    column.SetCellValue(_powerId, "5");
    Assert.Equal("", column.GetCellValue(_resultId)); // no base chosen yet

    column.LoadFactorBaseParameterId = _powerId;
    Assert.Equal("0,5 (1)", column.GetCellValue(_resultId));
  }

  [Fact]
  public async Task Admin_default_reaches_components_without_a_choice_and_persists()
  {
    var state = (await OpenAsync())!;
    state.Columns[1].LoadFactorBaseParameterId = _currentId; // explicit user choice

    await new CatalogEditService(_db).SetDefaultLoadFactorParameterAsync(_form.Id, _powerId);
    state.SetDefaultLoadFactorParameter(_powerId);

    Assert.Equal(_powerId, state.Columns[0].LoadFactorBaseParameterId);
    Assert.Equal(_currentId, state.Columns[1].LoadFactorBaseParameterId); // kept

    // Next project / next user: the default comes from the catalog.
    var reopened = (await OpenAsync(new ProjectStore { }))!;
    Assert.Equal(_powerId, reopened.DefaultLoadFactorParameterId);
  }

  public void Dispose()
  {
    _db.Dispose();
    _connection.Dispose();
  }
}
