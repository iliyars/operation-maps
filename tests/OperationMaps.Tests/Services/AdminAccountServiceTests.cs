using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using Xunit;

namespace OperationMaps.Tests.Services;

public sealed class AdminAccountServiceTests : IDisposable
{
  private readonly SqliteConnection _connection;
  private readonly CatalogDbContext _db;
  private readonly AdminAccountService _service;

  public AdminAccountServiceTests()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();
    _db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
    _db.Database.EnsureCreated();
    _service = new AdminAccountService(_db);
  }

  [Fact]
  public void Hash_never_contains_the_password_and_differs_per_call()
  {
    var first = PasswordHasher.Hash("секрет123");
    var second = PasswordHasher.Hash("секрет123");

    Assert.DoesNotContain("секрет123", first);
    Assert.NotEqual(first, second); // random salt
    Assert.True(PasswordHasher.Verify("секрет123", first));
    Assert.True(PasswordHasher.Verify("секрет123", second));
    Assert.False(PasswordHasher.Verify("секрет124", first));
  }

  [Fact]
  public void Verify_rejects_garbage_instead_of_throwing()
  {
    Assert.False(PasswordHasher.Verify("x", ""));
    Assert.False(PasswordHasher.Verify("x", "plain-text-password"));
    Assert.False(PasswordHasher.Verify("x", "PBKDF2-SHA256$1000$not-base64$!!"));
  }

  [Fact]
  public async Task First_admin_can_be_created_and_then_signs_in()
  {
    Assert.False(await _service.AnyAdminExistsAsync());

    var created = await _service.CreateAsync(" ilya ", "Илья", "пароль1");

    Assert.True(await _service.AnyAdminExistsAsync());
    Assert.Equal("ilya", created.Login);

    var signedIn = await _service.VerifyAsync("ilya", "пароль1");
    Assert.Equal(created.Id, signedIn?.Id);
    Assert.Null(await _service.VerifyAsync("ilya", "неверный"));
    Assert.Null(await _service.VerifyAsync("nobody", "пароль1"));

    // The database holds only the hash.
    var stored = await _db.Users.AsNoTracking().SingleAsync();
    Assert.DoesNotContain("пароль1", stored.PasswordHash);
  }

  [Fact]
  public async Task Rejects_duplicate_login_and_short_password()
  {
    await _service.CreateAsync("ilya", "Илья", "пароль1");

    var duplicate = await Assert.ThrowsAsync<InvalidOperationException>(
        () => _service.CreateAsync("ilya", "Другой", "пароль2"));
    Assert.Contains("занят", duplicate.Message);

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => _service.CreateAsync("anna", "Анна", "123"));
  }

  [Fact]
  public async Task Another_admin_can_reset_a_forgotten_password()
  {
    await _service.CreateAsync("ilya", "Илья", "пароль1");
    var anna = await _service.CreateAsync("anna", "Анна", "забытый");

    await _service.ChangePasswordAsync(anna.Id, "новыйпароль");

    Assert.Null(await _service.VerifyAsync("anna", "забытый"));
    Assert.NotNull(await _service.VerifyAsync("anna", "новыйпароль"));
  }

  [Fact]
  public async Task Last_admin_cannot_be_deleted()
  {
    var ilya = await _service.CreateAsync("ilya", "Илья", "пароль1");
    var anna = await _service.CreateAsync("anna", "Анна", "пароль2");

    await _service.DeleteAsync(anna.Id);
    Assert.Single(await _service.ListAsync());

    await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(ilya.Id));
    Assert.Single(await _service.ListAsync());
  }

  public void Dispose()
  {
    _db.Dispose();
    _connection.Dispose();
  }
}
