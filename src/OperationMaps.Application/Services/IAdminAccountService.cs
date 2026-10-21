namespace OperationMaps.Application.Services;

/// <summary>An administrator account as the UI sees it — never carries the password hash.</summary>
public sealed record AdminAccount(int Id, string Login, string DisplayName);

/// <summary>
/// Administrator accounts (each admin has their own login and password).
/// Regular users don't log in at all — they only browse the catalog and add
/// components marked "не проверен".
/// </summary>
public interface IAdminAccountService
{
  public const int MinPasswordLength = 6;

  /// <summary>False on a fresh catalog — the first sign-in then creates the first admin.</summary>
  Task<bool> AnyAdminExistsAsync(CancellationToken ct = default);

  /// <summary>The account if login and password match; null otherwise (no hint which one was wrong).</summary>
  Task<AdminAccount?> VerifyAsync(string login, string password, CancellationToken ct = default);

  /// <summary>Throws <see cref="InvalidOperationException"/> with a user-facing message on invalid input.</summary>
  Task<AdminAccount> CreateAsync(string login, string displayName, string password, CancellationToken ct = default);

  Task<IReadOnlyList<AdminAccount>> ListAsync(CancellationToken ct = default);

  /// <summary>Throws <see cref="InvalidOperationException"/> with a user-facing message on invalid input.</summary>
  Task ChangePasswordAsync(int adminId, string newPassword, CancellationToken ct = default);

  /// <summary>Refuses to delete the last admin — that would lock everyone out of admin mode.</summary>
  Task DeleteAsync(int adminId, CancellationToken ct = default);
}
