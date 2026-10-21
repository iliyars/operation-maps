using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Wpf.Features.Catalog;
using OperationMaps.Wpf.Stores;
using Xunit;

namespace OperationMaps.Tests.Wpf;

public sealed class CatalogViewModelTests : IDisposable
{
  private readonly SqliteConnection _connection;
  private readonly CatalogDbContext _db;
  private readonly CatalogViewModel _vm;

  public CatalogViewModelTests()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();
    _db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    _db.Database.EnsureCreated();

    var form4Resource = new FormParameter { RowNumber = 1, Name = "Ресурс", Unit = "ч" };
    var form4 = new Form { Number = "4", Title = "Форма 4", Parameters = [form4Resource, new FormParameter { RowNumber = 2, Name = "Срок службы" }] };
    var voltage = new FormParameter { RowNumber = 1, Name = "Напряжение", Unit = "В" };
    var form67 = new Form { Number = "67", Title = "Форма 67", Parameters = [voltage] };

    var capacitor = new ComponentType { Name = "Конденсатор" };
    var family = new Family { Name = "К10-79", ComponentType = capacitor };
    var verified = new Component { FullName = "К10-79-50В-0,1мкФ", Family = family, OwnForm = form67 };
    var unverified = new Component { FullName = "К10-79-25В-4,7мкФ", Family = family, OwnForm = form67, NeedsAdminReview = true };

    var chipType = new ComponentType { Name = "Микросхема" };
    var chipFamily = new Family { Name = "1564ЛА3", ComponentType = chipType };
    var chip = new Component { FullName = "1564ЛА3", Family = chipFamily };

    _db.AddRange(form4, form67, capacitor, family, verified, unverified, chipType, chipFamily, chip);
    _db.SaveChanges();

    _db.FamilyNtdValues.Add(new FamilyNtdValue { FamilyId = family.Id, FormParameterId = form4Resource.Id, Value = "100000" });
    _db.ComponentNtdValues.Add(new ComponentNtdValue { ComponentId = verified.Id, FormParameterId = voltage.Id, Value = "50" });
    _db.SaveChanges();

    _vm = new CatalogViewModel(_db, new SessionStore(), new ComponentEntryService(_db), new ComponentNameParser());
  }

  private CatalogFamilyNode Family(string name) => _vm.Types.SelectMany(t => t.AllFamilies).Single(f => f.Name == name);

  [Fact]
  public async Task Lists_types_families_and_components()
  {
    await _vm.OnNavigatedToAsync();

    Assert.Equal(["Конденсатор", "Микросхема"], _vm.Types.Select(t => t.Name));
    Assert.Equal(2, _vm.FamilyCount);
    Assert.Equal(3, _vm.ComponentCount);
    Assert.True(Family("К10-79").HasUnverified);
  }

  [Fact]
  public async Task Family_card_shows_form4_values_with_dash_for_missing()
  {
    await _vm.OnNavigatedToAsync();

    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-79"));

    Assert.Equal(2, _vm.VisibleComponents.Count);
    Assert.Equal("К10-79", _vm.Card!.Title);
    Assert.Equal(["100000", "—"], _vm.Card.Parameters.Select(p => p.Value));
    Assert.Equal("Ресурс, ч", _vm.Card.Parameters[0].Name);
  }

  [Fact]
  public async Task Component_card_shows_own_form_values_and_review_flag()
  {
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-79"));

    var verified = _vm.VisibleComponents.Single(c => c.FullName == "К10-79-50В-0,1мкФ");
    await _vm.SelectComponentCommand.ExecuteAsync(verified);

    Assert.Equal("ФОРМА 67 — ПО НТД", _vm.Card!.ParametersHeader);
    Assert.Equal("50", Assert.Single(_vm.Card.Parameters).Value);
    Assert.False(_vm.Card.NeedsReview);

    var unverified = _vm.VisibleComponents.Single(c => c.NeedsReview);
    await _vm.SelectComponentCommand.ExecuteAsync(unverified);

    Assert.True(_vm.Card!.NeedsReview);
  }

  [Fact]
  public async Task Component_without_own_form_explains_it()
  {
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("1564ЛА3"));

    await _vm.SelectComponentCommand.ExecuteAsync(_vm.VisibleComponents.Single());

    Assert.NotNull(_vm.Card!.EmptyMessage);
    Assert.Empty(_vm.Card.Parameters);
  }

  [Fact]
  public async Task Search_by_component_name_finds_its_family_and_filters_components()
  {
    await _vm.OnNavigatedToAsync();

    _vm.SearchText = "4,7мкФ";

    var capacitors = _vm.Types.Single(t => t.Name == "Конденсатор");
    Assert.True(capacitors.IsExpanded);
    Assert.Single(capacitors.Families);
    Assert.Empty(_vm.Types.Single(t => t.Name == "Микросхема").Families);

    await _vm.SelectFamilyCommand.ExecuteAsync(capacitors.Families[0]);
    Assert.Equal("К10-79-25В-4,7мкФ", Assert.Single(_vm.VisibleComponents).FullName);
  }

  public void Dispose()
  {
    _db.Dispose();
    _connection.Dispose();
  }
}
