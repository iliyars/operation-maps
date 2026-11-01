using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Users;
using OperationMaps.Domain.Enums;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Infrastructure.Services;

/// <summary>
/// Admin accounts in the catalog database's <c>Users</c> table — so they
/// move together with the catalog (e.g. to SQL Server later).
/// </summary>
public sealed class AdminAccountService : IAdminAccountService
{
  private readonly IDbContextFactory<CatalogDbContext> _dbFactory;

  public AdminAccountService(IDbContextFactory<CatalogDbContext> dbFactory)
  {
    _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
  }

  public async Task<bool> AnyAdminExistsAsync(CancellationToken ct = default)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    return await db.Users.AnyAsync(u => u.Role == UserRole.Admin, ct);
  }

  public async Task<AdminAccount?> VerifyAsync(string login, string password, CancellationToken ct = default)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    var normalized = login.Trim();
    var user = await db.Users.Where(u => u.Role == UserRole.Admin).AsNoTracking().FirstOrDefaultAsync(u => u.Login == normalized, ct);

    // Hashing runs on a worker thread: it's deliberately slow (~0.3 s).
    if (user is null || !await Task.Run(() => PasswordHasher.Verify(password, user.PasswordHash), ct))
      return null;

    return ToAccount(user);
  }

  public async Task<AdminAccount> CreateAsync(string login, string displayName, string password, CancellationToken ct = default)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    var normalized = login.Trim();
    if (normalized.Length == 0)
      throw new InvalidOperationException("Введите логин.");
    ValidatePassword(password);

    if (await db.Users.AnyAsync(u => u.Login == normalized, ct))
      throw new InvalidOperationException($"Логин «{normalized}» уже занят.");

    var user = new AppUser
    {
      Login = normalized,
      DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalized : displayName.Trim(),
      Role = UserRole.Admin,
      PasswordHash = await Task.Run(() => PasswordHasher.Hash(password), ct),
    };
    db.Users.Add(user);
    await db.SaveChangesAsync(ct);

    return ToAccount(user);
  }

  public async Task<IReadOnlyList<AdminAccount>> ListAsync(CancellationToken ct = default)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    return (await db.Users.Where(u => u.Role == UserRole.Admin).AsNoTracking().OrderBy(u => u.Login).ToListAsync(ct))
        .Select(ToAccount).ToList();
  }

  public async Task ChangePasswordAsync(int adminId, string newPassword, CancellationToken ct = default)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    ValidatePassword(newPassword);

    var user = await db.Users.Where(u => u.Role == UserRole.Admin).FirstOrDefaultAsync(u => u.Id == adminId, ct)
        ?? throw new InvalidOperationException("Администратор не найден.");

    user.PasswordHash = await Task.Run(() => PasswordHasher.Hash(newPassword), ct);
    await db.SaveChangesAsync(ct);
  }

  public async Task DeleteAsync(int adminId, CancellationToken ct = default)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    var user = await db.Users.Where(u => u.Role == UserRole.Admin).FirstOrDefaultAsync(u => u.Id == adminId, ct)
        ?? throw new InvalidOperationException("Администратор не найден.");

    if (await db.Users.Where(u => u.Role == UserRole.Admin).CountAsync(ct) <= 1)
      throw new InvalidOperationException(
          "Нельзя удалить последнего администратора — иначе режим администратора станет недоступен.");

    db.Users.Remove(user);
    await db.SaveChangesAsync(ct);
  }

  private static void ValidatePassword(string password)
  {
    if (password.Length < IAdminAccountService.MinPasswordLength)
      throw new InvalidOperationException(
          $"Пароль должен быть не короче {IAdminAccountService.MinPasswordLength} символов.");
  }

  private static AdminAccount ToAccount(AppUser u) => new(u.Id, u.Login, u.DisplayName);
}
