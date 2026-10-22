using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Infrastructure.Persistence;
using Xunit;

namespace OperationMaps.Tests.Persistence;

/// <summary>
/// Startup behaviour for a catalog that may sit in a shared folder: only the
/// admin's copy changes the schema; other copies never create or upgrade it.
/// </summary>
public sealed class CatalogDatabaseInitializerTests : IDisposable
{
  private readonly string _folder = Path.Combine(Path.GetTempPath(), $"catalog-test-{Guid.NewGuid():N}");
  private string DbPath => Path.Combine(_folder, "catalog.db");
  private string Backups => Path.Combine(_folder, "backups");

  public CatalogDatabaseInitializerTests() => Directory.CreateDirectory(_folder);

  private CatalogDbContext Open(bool isAdminCopy)
      => new(new DbContextOptionsBuilder<CatalogDbContext>()
          .UseSqlite(CatalogConnection.Build($"Data Source={DbPath}", allowCreate: isAdminCopy))
          .Options);

  private static Task<CatalogStartupStatus> InitAsync(CatalogDbContext db, bool isAdminCopy, int retentionDays = 14)
      => new CatalogDatabaseInitializer(db, new CatalogDatabaseOptions
      {
        UpdateSchemaOnStartup = isAdminCopy,
        BackupRetentionDays = retentionDays,
      }).InitializeAsync();

  [Fact]
  public void Connection_string_is_safe_for_a_shared_file()
  {
    var built = new SqliteConnectionStringBuilder(CatalogConnection.Build("Data Source=catalog.db", allowCreate: false));

    Assert.Equal(Path.Combine(AppContext.BaseDirectory, "catalog.db"), built.DataSource); // not the working directory
    Assert.False(built.Pooling);
    Assert.Equal(30, built.DefaultTimeout);
    Assert.Equal(SqliteOpenMode.ReadWrite, built.Mode); // never creates an empty file
  }

  [Fact]
  public async Task Admin_copy_creates_seeds_and_backs_up_a_new_catalog()
  {
    await using var db = Open(isAdminCopy: true);

    Assert.Equal(CatalogStartupStatus.Ok, await InitAsync(db, isAdminCopy: true));

    Assert.True(await db.Forms.AnyAsync(f => f.Number == "4"));
    Assert.True(File.Exists(Path.Combine(Backups, $"catalog-{DateTime.Now:yyyy-MM-dd}.db")));
  }

  [Fact]
  public async Task User_copy_reports_a_missing_file_instead_of_creating_an_empty_one()
  {
    await using var db = Open(isAdminCopy: false);

    Assert.Equal(CatalogStartupStatus.DatabaseNotFound, await InitAsync(db, isAdminCopy: false));
    Assert.False(File.Exists(DbPath));
  }

  [Fact]
  public async Task User_copy_works_with_an_up_to_date_catalog()
  {
    await using (var admin = Open(isAdminCopy: true))
      await InitAsync(admin, isAdminCopy: true);

    await using var user = Open(isAdminCopy: false);
    Assert.Equal(CatalogStartupStatus.Ok, await InitAsync(user, isAdminCopy: false));
  }

  [Fact]
  public async Task User_copy_does_not_upgrade_an_outdated_catalog()
  {
    // A catalog file without the program's migrations applied.
    await using (var raw = Open(isAdminCopy: true))
      await raw.Database.OpenConnectionAsync();

    await using var user = Open(isAdminCopy: false);
    Assert.Equal(CatalogStartupStatus.SchemaUpdateRequired, await InitAsync(user, isAdminCopy: false));
    Assert.Empty(await user.Database.GetAppliedMigrationsAsync());
  }

  [Fact]
  public async Task Catalog_upgraded_by_a_newer_program_is_reported()
  {
    await using (var admin = Open(isAdminCopy: true))
    {
      await InitAsync(admin, isAdminCopy: true);
      await admin.Database.ExecuteSqlRawAsync(
          "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('29991231000000_FromTheFuture', '99.0')");
    }

    await using var user = Open(isAdminCopy: false);
    Assert.Equal(CatalogStartupStatus.ProgramTooOld, await InitAsync(user, isAdminCopy: false));
  }

  [Fact]
  public async Task Old_backups_are_pruned()
  {
    Directory.CreateDirectory(Backups);
    var old = Path.Combine(Backups, "catalog-2000-01-01.db");
    await File.WriteAllTextAsync(old, "old");
    File.SetLastWriteTime(old, DateTime.Now.AddDays(-30));

    await using var db = Open(isAdminCopy: true);
    await InitAsync(db, isAdminCopy: true, retentionDays: 14);

    Assert.False(File.Exists(old));
  }

  public void Dispose()
  {
    SqliteConnection.ClearAllPools();
    try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
  }
}
