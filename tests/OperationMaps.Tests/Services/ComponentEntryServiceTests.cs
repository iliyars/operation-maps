using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;

namespace OperationMaps.Tests.Services;

public sealed class ComponentEntryServiceTests : IDisposable
{
  private readonly SqliteConnection _connection = new("DataSource=:memory:");
  private readonly CatalogDbContext _db;
  private readonly ComponentEntryService _service;
  private readonly ComponentType _type = new() { Name = "Конденсатор" };
  private readonly Form _form4 = new()
  {
    Number = "4", Title = "Форма 4",
    Parameters = [new FormParameter { RowNumber = 1, Name = "Ресурс" }],
  };
  private readonly Form _ownForm = new()
  {
    Number = "67", Title = "Конденсаторы",
    Parameters = [new FormParameter { RowNumber = 1, Name = "Напряжение" }],
  };

  public ComponentEntryServiceTests()
  {
    _connection.Open();
    _db = new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    _db.Database.EnsureCreated();
    _db.AddRange(_type, _form4, _ownForm);
    _db.SaveChanges();
    _service = new(TestCatalogDbContextFactory.From(_db));
  }

  private NewComponentInput Input(int? familyId = null, bool invalidParameter = false) => new()
  {
    ComponentTypeId = _type.Id,
    ExistingFamilyId = familyId,
    NewFamilyName = "К10-79",
    FullName = "К10-79-25В-1мкФ",
    OwnFormId = _ownForm.Id,
    Form4Values = new Dictionary<int, string> { [_form4.Parameters.Single().Id] = "10000" },
    OwnFormValues = new Dictionary<int, string>
    {
      [invalidParameter ? int.MaxValue : _ownForm.Parameters.Single().Id] = "25",
    },
    PinValues = new Dictionary<int, string> { [_ownForm.Parameters.Single().Id] = "1, 2" },
  };

  [Fact]
  public async Task Late_failure_rolls_back_new_family_and_all_component_data_and_allows_retry()
  {
    // The invalid FK fails on the final save, after family and component IDs were assigned.
    await Assert.ThrowsAsync<DbUpdateException>(() => _service.CreateComponentAsync(Input(invalidParameter: true)));

    Assert.Empty(await _db.Families.AsNoTracking().ToListAsync());
    Assert.Empty(await _db.Components.AsNoTracking().ToListAsync());
    Assert.Empty(await _db.FamilyForms.AsNoTracking().ToListAsync());
    Assert.Empty(await _db.FamilyNtdValues.AsNoTracking().ToListAsync());
    Assert.Empty(await _db.ComponentNtdValues.AsNoTracking().ToListAsync());
    Assert.Empty(await _db.ComponentPinValues.AsNoTracking().ToListAsync());

    var component = await _service.CreateComponentAsync(Input());
    Assert.Equal("К10-79", component.Family.Name);
    Assert.Equal("67", component.OwnForm!.Number);
    Assert.Single(await _db.Components.AsNoTracking().ToListAsync());
    Assert.Single(await _db.ComponentNtdValues.AsNoTracking().ToListAsync());
    Assert.Single(await _db.ComponentPinValues.AsNoTracking().ToListAsync());
  }

  [Fact]
  public async Task Late_failure_does_not_modify_existing_family()
  {
    var family = new Family { Name = "К10-79", ComponentTypeId = _type.Id };
    _db.Families.Add(family);
    await _db.SaveChangesAsync();

    await Assert.ThrowsAsync<DbUpdateException>(
        () => _service.CreateComponentAsync(Input(family.Id, invalidParameter: true)));

    Assert.Single(await _db.Families.AsNoTracking().ToListAsync());
    Assert.Empty(await _db.FamilyForms.AsNoTracking().ToListAsync());
    Assert.Empty(await _db.FamilyNtdValues.AsNoTracking().ToListAsync());
    Assert.Empty(await _db.Components.AsNoTracking().ToListAsync());
  }

  public void Dispose()
  {
    _db.Dispose();
    _connection.Dispose();
  }
}
