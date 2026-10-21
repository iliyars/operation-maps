using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Wpf.Features.Catalog;
using OperationMaps.Wpf.Features.Components.AddComponent;
using OperationMaps.Wpf.Stores;
using Xunit;

namespace OperationMaps.Tests.Wpf;

/// <summary>Admin adds components in "Справочник ЭРИ" through the shared wizard.</summary>
public sealed class CatalogAddComponentTests : IDisposable
{
  private readonly SqliteConnection _connection;
  private readonly CatalogDbContext _db;
  private Family _family = null!;
  private Form _form67 = null!;

  public CatalogAddComponentTests()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();
    _db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    _db.Database.EnsureCreated();

    var form4 = new Form { Number = "4", Title = "Форма 4", Parameters = [new FormParameter { RowNumber = 1, Name = "Ресурс" }] };
    _form67 = new Form { Number = "67", Title = "Конденсаторы", Parameters = [new FormParameter { RowNumber = 1, Name = "Напряжение" }] };
    var capacitor = new ComponentType { Name = "Конденсатор" };
    _db.ComponentTypes.Add(new ComponentType { Name = "Микросхема" });
    _family = new Family { Name = "К10-79", ComponentType = capacitor };
    _db.AddRange(form4, _form67, capacitor, _family,
        new Component { FullName = "К10-79-50В-0,1мкФ", Family = _family, OwnForm = _form67 });
    _db.SaveChanges();
  }

  private AddComponentDialogViewModel Wizard(AddComponentTarget target)
      => new(_db, new ComponentEntryService(_db), new ComponentNameParser(), target);

  private async Task ChooseForm67Async(AddComponentDialogViewModel wizard)
  {
    wizard.SelectedOwnForm = wizard.OwnFormOptions.Single(o => o.Number == "67");
    await Task.Delay(50); // own-form parameters load in the background
  }

  [Fact]
  public async Task Admin_adds_component_to_existing_family_as_verified()
  {
    var wizard = Wizard(AddComponentTarget.ForCatalog(_family.Id, _family.Name, "Конденсатор"));
    await wizard.InitializeAsync();

    Assert.True(wizard.IsStep2); // family known — straight to its own form
    Assert.Equal("Конденсатор", wizard.ComponentTypeName);
    Assert.False(wizard.SaveCommand.CanExecute(null)); // no name yet

    wizard.ComponentFullName = "  К10-79-25В-1,0мкФ ";
    await ChooseForm67Async(wizard);
    wizard.OwnFormRows[0].Value = "25";
    await wizard.SaveCommand.ExecuteAsync(null);

    Assert.True(wizard.IsCompleted, wizard.ErrorMessage);
    var saved = await _db.Components.AsNoTracking().Include(c => c.NtdValues)
        .SingleAsync(c => c.FullName == "К10-79-25В-1,0мкФ");
    Assert.False(saved.NeedsAdminReview);
    Assert.Equal(_family.Id, saved.FamilyId);
    Assert.Equal("25", Assert.Single(saved.NtdValues).Value);
  }

  [Fact]
  public async Task New_family_prefills_component_name_from_family_name()
  {
    var wizard = Wizard(AddComponentTarget.ForCatalog(null, null, "Микросхема"));
    await wizard.InitializeAsync();

    Assert.True(wizard.IsStep1);
    Assert.True(wizard.ShowTypePicker);
    Assert.Equal("Микросхема", wizard.SelectedType?.Name);

    wizard.FamilyName = "1564ЛА3";
    wizard.GoNextCommand.Execute(null);

    Assert.True(wizard.IsStep2);
    Assert.Equal("1564ЛА3", wizard.ComponentFullName);

    await ChooseForm67Async(wizard);
    await wizard.SaveCommand.ExecuteAsync(null);

    Assert.True(wizard.IsCompleted, wizard.ErrorMessage);
    var family = await _db.Families.AsNoTracking().Include(f => f.ComponentType).SingleAsync(f => f.Name == "1564ЛА3");
    Assert.Equal("Микросхема", family.ComponentType.Name);
  }

  [Fact]
  public async Task Duplicate_full_name_is_refused_with_a_clear_message()
  {
    var wizard = Wizard(AddComponentTarget.ForCatalog(_family.Id, _family.Name, "Конденсатор"));
    await wizard.InitializeAsync();
    wizard.ComponentFullName = "К10-79-50В-0,1мкФ";
    await ChooseForm67Async(wizard);

    await wizard.SaveCommand.ExecuteAsync(null);

    Assert.False(wizard.IsCompleted);
    Assert.Contains("уже есть", wizard.ErrorMessage);
    Assert.Equal(1, await _db.Components.CountAsync(c => c.FullName == "К10-79-50В-0,1мкФ"));
  }

  [Fact]
  public async Task Add_buttons_are_available_only_in_admin_mode()
  {
    var session = new SessionStore();
    var catalog = new CatalogViewModel(_db, session, new ComponentEntryService(_db), new ComponentNameParser());
    await catalog.OnNavigatedToAsync();
    await catalog.SelectFamilyCommand.ExecuteAsync(catalog.Types.SelectMany(t => t.AllFamilies).Single());

    Assert.False(catalog.AddFamilyCommand.CanExecute(null));
    Assert.False(catalog.AddComponentCommand.CanExecute(null));

    session.SignIn(new AdminAccount(1, "ilya", "Илья"));

    Assert.True(catalog.IsAdmin);
    Assert.True(catalog.AddFamilyCommand.CanExecute(null));
    Assert.True(catalog.AddComponentCommand.CanExecute(null));
  }

  [Fact]
  public async Task Admin_confirms_a_user_added_component()
  {
    _db.Components.Add(new Component { FullName = "К10-79-16В-10мкФ", FamilyId = _family.Id, OwnFormId = _form67.Id, NeedsAdminReview = true });
    await _db.SaveChangesAsync();

    var session = new SessionStore();
    var catalog = new CatalogViewModel(_db, session, new ComponentEntryService(_db), new ComponentNameParser());
    await catalog.OnNavigatedToAsync();
    Assert.Equal(1, catalog.UnverifiedCount);

    session.SignIn(new AdminAccount(1, "ilya", "Илья"));
    catalog.OnlyUnverified = true;

    var family = Assert.Single(catalog.Types.SelectMany(t => t.Families));
    await catalog.SelectFamilyCommand.ExecuteAsync(family);
    var pending = Assert.Single(catalog.VisibleComponents);
    await catalog.SelectComponentCommand.ExecuteAsync(pending);

    Assert.True(catalog.Card!.NeedsReview);
    Assert.True(catalog.VerifyCommand.CanExecute(null));

    await catalog.VerifyCommand.ExecuteAsync(null);

    Assert.False(catalog.Card!.NeedsReview);
    Assert.False(family.HasUnverified);
    Assert.Equal(0, catalog.UnverifiedCount);
    Assert.False((await _db.Components.AsNoTracking().SingleAsync(c => c.Id == pending.Id)).NeedsAdminReview);
  }

  [Fact]
  public async Task Regular_user_cannot_confirm()
  {
    _db.Components.Add(new Component { FullName = "К10-79-16В-10мкФ", FamilyId = _family.Id, OwnFormId = _form67.Id, NeedsAdminReview = true });
    await _db.SaveChangesAsync();

    var catalog = new CatalogViewModel(_db, new SessionStore(), new ComponentEntryService(_db), new ComponentNameParser());
    await catalog.OnNavigatedToAsync();
    await catalog.SelectFamilyCommand.ExecuteAsync(catalog.Types.SelectMany(t => t.AllFamilies).Single());
    await catalog.SelectComponentCommand.ExecuteAsync(catalog.VisibleComponents.Single(c => c.NeedsReview));

    Assert.False(catalog.VerifyCommand.CanExecute(null));
  }

  public void Dispose()
  {
    _db.Dispose();
    _connection.Dispose();
  }
}
