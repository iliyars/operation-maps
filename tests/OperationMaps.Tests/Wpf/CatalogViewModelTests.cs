using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
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

    _vm = new CatalogViewModel(TestCatalogDbContextFactory.From(_db), new SessionStore(), new ComponentEntryService(TestCatalogDbContextFactory.From(_db)), new ComponentNameParser(), new CatalogEditService(TestCatalogDbContextFactory.From(_db)), new Moq.Mock<IDialogService>().Object);
  }

  private CatalogFamilyNode Family(string name) => _vm.Types.SelectMany(t => t.AllFamilies).Single(f => f.Name == name);

  [Fact]
  public async Task Session_subscription_is_removed_on_leave_and_not_duplicated_on_return()
  {
    var session = new SessionStore();
    var factory = TestCatalogDbContextFactory.From(_db);
    var vm = new CatalogViewModel(factory, session, new ComponentEntryService(factory),
      new ComponentNameParser(), new CatalogEditService(factory), new Moq.Mock<IDialogService>().Object);
    var notifications = 0;
    vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(CatalogViewModel.IsAdmin)) notifications++; };
    await vm.OnNavigatedFromAsync();
    session.SignIn(new AdminAccount(1, "admin", "Администратор"));
    Assert.Equal(0, notifications);
    await vm.OnNavigatedToAsync();
    Assert.True(vm.Regimes.CanEdit);
    await vm.OnNavigatedToAsync();
    notifications = 0;
    session.SignOut();
    Assert.Equal(1, notifications);
    Assert.False(vm.Regimes.CanEdit);
    await vm.OnNavigatedFromAsync();
  }

  [Fact]
  public async Task Family_and_component_cards_show_family_and_component_notes()
  {
    var component = await _db.Components.SingleAsync(c => c.FullName == "К10-79-50В-0,1мкФ");
    var resource = await _db.FormParameters.SingleAsync(p => p.Form.Number == "4" && p.RowNumber == 1);
    var voltage = await _db.FormParameters.SingleAsync(p => p.Form.Number == "67");
    _db.FamilyNotes.Add(new FamilyNote { FamilyId = component.FamilyId, FormParameterId = resource.Id,
      Note = new Note { Text = "Примечание ресурса" } });
    _db.ComponentNotes.Add(new ComponentNote { ComponentId = component.Id, FormParameterId = voltage.Id,
      Note = new Note { Text = "Примечание напряжения" } });
    await _db.SaveChangesAsync();
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-79"));
    Assert.True(_vm.Card!.HasNotes);
    Assert.Equal(2, _vm.Card.Notes.Count);
    Assert.Contains(_vm.Card.Notes, n => n.Text == "Примечание напряжения"
      && n.Parameter.Contains(component.FullName) && n.Parameter.Contains("Форма 67"));
    await _vm.SelectComponentCommand.ExecuteAsync(_vm.VisibleComponents.Single(c => c.Id == component.Id));
    Assert.Equal(2, _vm.Card!.Notes.Count);
    Assert.Contains(_vm.Card.Notes, n => n.Text == "Примечание ресурса" && n.Parameter.Contains("Форма 4"));
    Assert.Contains(_vm.Regimes.FormSections.Single(s => s.Title == "Форма 67").Parameters,
      p => p.Notes.Contains("Примечание напряжения"));
  }

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
  public async Task Component_card_previews_catalog_regimes_and_reset_without_editing_default_values()
  {
    var component = await _db.Components.SingleAsync(c => c.FullName == "К10-79-50В-0,1мкФ");
    var resource = await _db.FormParameters.SingleAsync(p => p.Form.Number == "4" && p.RowNumber == 1);
    _db.ComponentRegimes.Add(new ComponentRegime
    {
      ComponentId = component.Id, Name = "Облегчённый", Condition = "U ≤ 0,6 Uном",
      Values = [new ComponentRegimeValue { FormParameterId = resource.Id, Value = "150000" }]
    });
    await _db.SaveChangesAsync();
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-79"));
    Assert.False(_vm.Regimes.IsAvailable);
    await _vm.SelectComponentCommand.ExecuteAsync(_vm.VisibleComponents.Single(c => c.Id == component.Id));
    Assert.True(_vm.Regimes.IsAvailable);
    Assert.True(_vm.Regimes.IsCatalogPreview);
    Assert.Equal(new[] { "Форма 4", "Форма 67" }, _vm.Regimes.FormSections.Select(s => s.Title));
    Assert.Equal(new[] { 1, 2 }, _vm.Regimes.FormSections[0].Parameters.Select(p => p.RowNumber));
    Assert.DoesNotContain("Форма 4", _vm.Regimes.FormSections[0].Parameters[0].Label);
    Assert.False(_vm.Regimes.CanEdit);
    Assert.False(_vm.Regimes.SaveCommand.CanExecute(null));
    Assert.False(_vm.ShowBaseParameters);
    var previewResource = _vm.Regimes.Values.Single(v => v.ParameterId == resource.Id);
    Assert.Equal("100000", previewResource.EffectiveValue);
    _vm.Regimes.Selected = _vm.Regimes.Choices.Single(c => c.Name == "Облегчённый");
    Assert.Equal("150000", previewResource.EffectiveValue);
    Assert.Equal("U ≤ 0,6 Uном", _vm.Regimes.Condition);
    _vm.Regimes.ResetCommand.Execute(null);
    Assert.Equal("100000", previewResource.EffectiveValue);
    Assert.Equal("100000", await _db.FamilyNtdValues.Where(v => v.FormParameterId == resource.Id).Select(v => v.Value).SingleAsync());
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("1564ЛА3"));
    Assert.True(_vm.Regimes.IsAvailable);
    Assert.Null(_vm.Regimes.Selected!.Selection);
    Assert.False(_vm.ShowBaseParameters);
  }

  [Fact]
  public async Task Single_component_family_offers_regimes_directly_in_family_card()
  {
    var component = await _db.Components.SingleAsync(c => c.FullName == "1564ЛА3");
    var resource = await _db.FormParameters.SingleAsync(p => p.Form.Number == "4" && p.RowNumber == 1);
    _db.ComponentRegimes.Add(new ComponentRegime
    {
      ComponentId = component.Id, Name = "Облегчённый", Condition = "Пониженная нагрузка",
      Values = [new ComponentRegimeValue { FormParameterId = resource.Id, Value = "150000" }]
    });
    await _db.SaveChangesAsync();
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("1564ЛА3"));

    Assert.Null(_vm.SelectedComponent);
    Assert.Equal("1564ЛА3", _vm.Card!.Title);
    Assert.True(_vm.Regimes.IsAvailable);
    Assert.False(_vm.ShowFamilyComponentPicker);
    _vm.Regimes.Selected = _vm.Regimes.Choices.Single(c => c.Name == "Облегчённый");
    Assert.Equal("150000", _vm.Regimes.Values.Single(v => v.ParameterId == resource.Id).EffectiveValue);

    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-79"));
    Assert.True(_vm.ShowFamilyComponentPicker);
    Assert.False(_vm.Regimes.IsAvailable);
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
