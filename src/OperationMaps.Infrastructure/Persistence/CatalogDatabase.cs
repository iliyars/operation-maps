using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OperationMaps.Infrastructure.Persistence;

/// <summary>
/// appsettings.json → "Catalog" section.
/// <para>
/// The catalog may live in a shared network folder (until a real database
/// server exists), opened by several people at once. SQLite tolerates many
/// readers there, but concurrent WRITES over a network share can corrupt
/// the file — so only ONE copy of the app (the admin's) is allowed to
/// change the schema and run the seeder.
/// </para>
/// </summary>
public sealed class CatalogDatabaseOptions
{
  /// <summary>
  /// True on the admin's copy only (and in development): applies pending
  /// migrations, runs the seeder, and may create the database file. Every
  /// other copy refuses to touch the schema and never creates an empty file
  /// behind a mistyped path.
  /// </summary>
  public bool UpdateSchemaOnStartup { get; set; } = true;

  /// <summary>How many days of backups to keep in the "backups" folder next to the database.</summary>
  public int BackupRetentionDays { get; set; } = 14;
}

public enum CatalogStartupStatus
{
  Ok,

  /// <summary>File missing and this copy isn't allowed to create it (wrong path / no network).</summary>
  DatabaseNotFound,

  /// <summary>The program expects a newer schema; only the admin's copy may upgrade it.</summary>
  SchemaUpdateRequired,

  /// <summary>The database was upgraded by a newer version of the program.</summary>
  ProgramTooOld,
}

public static class CatalogConnection
{
  /// <summary>
  /// Normalizes the configured connection string for a possibly shared file:
  /// absolute path (relative = next to the program, %VARS% expanded), no
  /// pooled connections holding the file open, wait up to 30 s on a busy
  /// database instead of failing with "database is locked", and — unless
  /// <paramref name="allowCreate"/> — never create a new empty file.
  /// </summary>
  public static string Build(string configured, bool allowCreate)
  {
    var builder = new SqliteConnectionStringBuilder(configured);

    var path = Environment.ExpandEnvironmentVariables(builder.DataSource);
    if (path != ":memory:" && !Path.IsPathRooted(path))
      path = Path.Combine(AppContext.BaseDirectory, path);

    builder.DataSource = path;
    builder.Pooling = false;
    builder.DefaultTimeout = 30;
    builder.Mode = allowCreate ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadWrite;
    return builder.ToString();
  }

  public static string DataSourceOf(string connectionString)
      => new SqliteConnectionStringBuilder(connectionString).DataSource;
}

/// <summary>
/// Startup work on the catalog database: schema/version checks, migration
/// and seeding (admin copy only), and backups.
/// </summary>
public sealed class CatalogDatabaseInitializer
{
  private readonly CatalogDbContext _db;
  private readonly CatalogDatabaseOptions _options;
  private readonly ILogger _logger;

  public CatalogDatabaseInitializer(
      CatalogDbContext db,
      CatalogDatabaseOptions options,
      ILogger<CatalogDatabaseInitializer>? logger = null)
  {
    _db = db ?? throw new ArgumentNullException(nameof(db));
    _options = options ?? throw new ArgumentNullException(nameof(options));
    _logger = logger ?? NullLogger<CatalogDatabaseInitializer>.Instance;
  }

  public string DatabasePath => CatalogConnection.DataSourceOf(_db.Database.GetConnectionString()!);

  public async Task<CatalogStartupStatus> InitializeAsync(CancellationToken ct = default)
  {
    if (!_options.UpdateSchemaOnStartup && !File.Exists(DatabasePath))
      return CatalogStartupStatus.DatabaseNotFound;

    var known = _db.Database.GetMigrations().ToHashSet();
    var applied = (await _db.Database.GetAppliedMigrationsAsync(ct)).ToList();

    if (applied.Any(m => !known.Contains(m)))
      return CatalogStartupStatus.ProgramTooOld;

    var pending = (await _db.Database.GetPendingMigrationsAsync(ct)).ToList();
    if (pending.Count > 0)
    {
      if (!_options.UpdateSchemaOnStartup)
        return CatalogStartupStatus.SchemaUpdateRequired;

      // An upgrade is the riskiest moment for the data — keep the "before".
      if (applied.Count > 0)
        await BackupAsync($"catalog-{DateTime.Now:yyyy-MM-dd_HHmmss}-before-update.db", ct);

      _logger.LogInformation("Applying catalog migrations: {Migrations}", string.Join(", ", pending));
      await _db.Database.MigrateAsync(ct);
    }

    if (_options.UpdateSchemaOnStartup)
      await DatabaseSeeder.SeedAsync(_db);

    // First start of the day (on any copy) makes the daily backup.
    await BackupAsync($"catalog-{DateTime.Now:yyyy-MM-dd}.db", ct);
    PruneOldBackups();

    return CatalogStartupStatus.Ok;
  }

  private string BackupFolder => Path.Combine(Path.GetDirectoryName(DatabasePath)!, "backups");

  /// <summary>
  /// Consistent copy via SQLite's online backup API (safe while others read
  /// or write — unlike copying the file). A failure only logs: no backup is
  /// never a reason to keep the user out of the program.
  /// </summary>
  private async Task BackupAsync(string fileName, CancellationToken ct)
  {
    var target = Path.Combine(BackupFolder, fileName);
    if (File.Exists(target)) return;

    var temp = target + $".{Environment.ProcessId}.tmp";
    try
    {
      Directory.CreateDirectory(BackupFolder);

      await _db.Database.OpenConnectionAsync(ct);
      try
      {
        var source = (SqliteConnection)_db.Database.GetDbConnection();
        using var destination = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = temp, Pooling = false }.ToString());
        source.BackupDatabase(destination);
      }
      finally
      {
        await _db.Database.CloseConnectionAsync();
      }

      // Another copy of the program may have made the same backup meanwhile.
      if (!File.Exists(target))
        File.Move(temp, target);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
    {
      _logger.LogWarning(ex, "Catalog backup {File} failed", target);
    }
    finally
    {
      if (File.Exists(temp))
        try { File.Delete(temp); } catch (IOException) { }
    }
  }

  private void PruneOldBackups()
  {
    if (!Directory.Exists(BackupFolder)) return;

    var cutoff = DateTime.Now.AddDays(-Math.Max(1, _options.BackupRetentionDays));
    foreach (var file in Directory.EnumerateFiles(BackupFolder, "catalog-*.db"))
    {
      try
      {
        if (File.GetLastWriteTime(file) < cutoff)
          File.Delete(file);
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
      {
        _logger.LogWarning(ex, "Could not delete old backup {File}", file);
      }
    }
  }
}
