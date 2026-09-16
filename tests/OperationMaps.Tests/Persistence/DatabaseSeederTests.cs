using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Infrastructure.Persistence;
using Xunit;

namespace OperationMaps.Tests.Persistence;

public class DatabaseSeederTests : IDisposable
{
  private readonly SqliteConnection _connection;
  private readonly CatalogDbContext _db;

  public DatabaseSeederTests()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();

    var options = new DbContextOptionsBuilder<CatalogDbContext>()
        .UseSqlite(_connection)
        .Options;

    _db = new CatalogDbContext(options);
    _db.Database.EnsureCreated();
  }

  [Fact]
  public async Task SeedAsync_adds_forms_missing_from_a_previously_seeded_database()
  {
    await DatabaseSeeder.SeedAsync(_db);
    var totalBefore = await _db.Forms.CountAsync();

    // Simulate a database that was seeded by an older version of
    // DatabaseSeeder, before AddForm86() existed — this is exactly the
    // real-world state that used to get stuck forever, because SeedAsync
    // returned early the moment Form 4 was already present, regardless
    // of what else had (or hadn't) been added since.
    var form86 = await _db.Forms.SingleAsync(f => f.Number == "86");
    _db.Forms.Remove(form86);
    await _db.SaveChangesAsync();

    await DatabaseSeeder.SeedAsync(_db);

    var numbers = await _db.Forms.Select(f => f.Number).ToListAsync();
    Assert.Contains("86", numbers);
    Assert.Equal(totalBefore, numbers.Count); // only the missing one came back
  }

  [Fact]
  public async Task SeedAsync_does_not_duplicate_forms_or_catalog_data_on_repeat_calls()
  {
    await DatabaseSeeder.SeedAsync(_db);
    var formsAfterFirst = await _db.Forms.CountAsync();
    var familiesAfterFirst = await _db.Families.CountAsync();
    var componentsAfterFirst = await _db.Components.CountAsync();

    await DatabaseSeeder.SeedAsync(_db);

    Assert.Equal(formsAfterFirst, await _db.Forms.CountAsync());
    Assert.Equal(familiesAfterFirst, await _db.Families.CountAsync());
    Assert.Equal(componentsAfterFirst, await _db.Components.CountAsync());
  }

  [Fact]
  public async Task SeedAsync_seeds_every_form_that_has_a_map_json()
  {
    await DatabaseSeeder.SeedAsync(_db);

    var numbers = (await _db.Forms.Select(f => f.Number).ToListAsync()).ToHashSet();

    // These are the forms with a working map.json/template.docx as of this
    // test (see docs/adding-a-form.md) — if one of these stops being
    // seeded, exporting it would be reachable in code but never in the app.
    foreach (var expected in new[] { "4", "55", "64", "67", "68", "69", "83", "86" })
      Assert.Contains(expected, numbers);
  }

  public void Dispose()
  {
    _db.Dispose();
    _connection.Dispose();
  }
}
