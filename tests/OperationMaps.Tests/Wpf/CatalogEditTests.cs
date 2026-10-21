using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Wpf.Features.Catalog;
using OperationMaps.Wpf.Stores;
using Xunit;

namespace OperationMaps.Tests.Wpf;

/// <summary>Admin edits and deletes catalog records from "Справочник ЭРИ".</summary>
public sealed class CatalogEditTests : IDisposable
{
  private readonly SqliteConnection _connection;
  private readonly CatalogDbContext _db;
  private readonly Mock<IDialogService> _dialogs = new();
  private readonly SessionStore _session = new();
  private readonly CatalogViewModel _vm;

  private int _resourceParamId;
  private int _voltageParamId;

  public CatalogEditTests()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();
    _db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    _db.Database.EnsureCreated();

    var resource = new FormParameter { RowNumber = 1, Name = "Ресурс" };
    var voltage = new FormParameter { RowNumber = 1, Name = "Напряжение" };
    var form4 = new Form { Number = "4", Title = "Форма 4", Parameters = [resource] };
    var form67 = new Form { Number = "67", Title = "Конденсаторы", Parameters = [voltage] };
    var type = new ComponentType { Name = "Конденсатор" };
    var family = new Family { Name = "К10-79", ComponentType = type };
    var other = new Family { Name = "К10-84в", ComponentType = type };
    _db.AddRange(form4, form67, type, family, other,
        new Component { FullName = "К10-79-50В-0,1мкФ", Family = family, OwnForm = form67 },
        new Component { FullName = "К10-79-25В-1,0мкФ", Family = family, OwnForm = form67 },
        new Component { FullName = "К10-84в-1мкФ", Family = other, OwnForm = form67 });
    _db.SaveChanges();

    _resourceParamId = resource.Id;
    _voltageParamId = voltage.Id;
    _db.FamilyNtdValues.Add(new FamilyNtdValue { FamilyId = family.Id, FormParameterId = resource.Id, Value = "100000" });
    _db.SaveChanges();

    _session.SignIn(new AdminAccount(1, "ilya", "Илья"));
    _vm = new CatalogViewModel(_db, _session, new ComponentEntryService(_db), new ComponentNameParser(),
        new CatalogEditService(_db), _dialogs.Object);
  }

  private CatalogFamilyNode Family(string name) => _vm.Types.SelectMany(t => t.AllFamilies).Single(f => f.Name == name);

  private void AnswerYes() => _dialogs
      .Setup(d => d.AskYesNoCancel(It.IsAny<string>(), It.IsAny<string?>()))
      .Returns(true);

  [Fact]
  public async Task Admin_edits_family_name_and_form4_value()
  {
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-79"));

    _vm.StartEditCommand.Execute(null);
    Assert.True(_vm.IsEditing);
    Assert.Equal("К10-79", _vm.EditName);
    Assert.Equal("100000", _vm.Card!.Parameters[0].EditValue);

    _vm.EditName = "К10-79А";
    _vm.Card.Parameters[0].EditValue = "50000";
    await _vm.SaveEditCommand.ExecuteAsync(null);

    Assert.False(_vm.IsEditing);
    Assert.Null(_vm.EditError);
    Assert.Equal("К10-79А", _vm.Card!.Title); // re-selected after reload
    Assert.Equal("50000", _vm.Card.Parameters[0].Value);
  }

  [Fact]
  public async Task Clearing_a_value_removes_it()
  {
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-79"));

    _vm.StartEditCommand.Execute(null);
    _vm.Card!.Parameters[0].EditValue = "  ";
    await _vm.SaveEditCommand.ExecuteAsync(null);

    Assert.Equal("—", _vm.Card!.Parameters[0].Value);
    Assert.False(await _db.FamilyNtdValues.AnyAsync(v => v.FormParameterId == _resourceParamId));
  }

  [Fact]
  public async Task Admin_edits_component_name_and_value()
  {
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-79"));
    await _vm.SelectComponentCommand.ExecuteAsync(_vm.VisibleComponents.Single(c => c.FullName == "К10-79-50В-0,1мкФ"));

    _vm.StartEditCommand.Execute(null);
    _vm.EditName = "К10-79-63В-0,1мкФ";
    _vm.Card!.Parameters[0].EditValue = "63";
    await _vm.SaveEditCommand.ExecuteAsync(null);

    Assert.Equal("К10-79-63В-0,1мкФ", _vm.Card!.Title);
    Assert.Equal("63", _vm.Card.Parameters[0].Value);
    Assert.Equal(_voltageParamId, _vm.Card.Parameters[0].FormParameterId);
  }

  [Fact]
  public async Task Renaming_to_an_existing_name_shows_an_error_and_keeps_editing()
  {
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-79"));
    await _vm.SelectComponentCommand.ExecuteAsync(_vm.VisibleComponents.Single(c => c.FullName == "К10-79-50В-0,1мкФ"));

    _vm.StartEditCommand.Execute(null);
    _vm.EditName = "К10-84в-1мкФ";
    await _vm.SaveEditCommand.ExecuteAsync(null);

    Assert.True(_vm.IsEditing);
    Assert.Contains("уже есть", _vm.EditError);
  }

  [Fact]
  public async Task Deleting_the_last_component_offers_to_delete_the_empty_family()
  {
    AnswerYes();
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-84в"));
    await _vm.SelectComponentCommand.ExecuteAsync(_vm.VisibleComponents.Single());

    await _vm.DeleteCommand.ExecuteAsync(null);

    Assert.False(await _db.Components.AnyAsync(c => c.FullName == "К10-84в-1мкФ"));
    Assert.False(await _db.Families.AnyAsync(f => f.Name == "К10-84в"));
    Assert.DoesNotContain(_vm.Types.SelectMany(t => t.AllFamilies), f => f.Name == "К10-84в");
    _dialogs.Verify(d => d.AskYesNoCancel(It.Is<string>(m => m.Contains("больше нет компонентов")), It.IsAny<string?>()), Times.Once);
  }

  [Fact]
  public async Task Family_with_components_cannot_be_deleted()
  {
    AnswerYes();
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-79"));

    await _vm.DeleteCommand.ExecuteAsync(null);

    Assert.True(await _db.Families.AnyAsync(f => f.Name == "К10-79"));
    _dialogs.Verify(d => d.ShowWarning(It.Is<string>(m => m.Contains("Сначала удалите")), It.IsAny<string?>()), Times.Once);
  }

  [Fact]
  public async Task Cancelled_delete_keeps_the_component()
  {
    _dialogs.Setup(d => d.AskYesNoCancel(It.IsAny<string>(), It.IsAny<string?>())).Returns((bool?)null);
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-84в"));
    await _vm.SelectComponentCommand.ExecuteAsync(_vm.VisibleComponents.Single());

    await _vm.DeleteCommand.ExecuteAsync(null);

    Assert.True(await _db.Components.AnyAsync(c => c.FullName == "К10-84в-1мкФ"));
  }

  [Fact]
  public async Task Regular_user_cannot_edit_or_delete()
  {
    _session.SignOut();
    await _vm.OnNavigatedToAsync();
    await _vm.SelectFamilyCommand.ExecuteAsync(Family("К10-79"));

    Assert.False(_vm.StartEditCommand.CanExecute(null));
    Assert.False(_vm.DeleteCommand.CanExecute(null));
  }

  public void Dispose()
  {
    _db.Dispose();
    _connection.Dispose();
  }
}
