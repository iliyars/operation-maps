using System.Security.Cryptography;

namespace OperationMaps.Infrastructure.Services;

/// <summary>
/// Salted, deliberately slow password hashing (PBKDF2-HMAC-SHA256, built
/// into .NET). The database never holds a password, only
/// <c>PBKDF2-SHA256$iterations$salt$hash</c>: a stolen copy of the database
/// reveals no passwords, and every guess costs the attacker the same slow
/// computation. The iteration count is stored per hash, so it can be raised
/// later without invalidating existing passwords.
/// </summary>
public static class PasswordHasher
{
  private const string Scheme = "PBKDF2-SHA256";
  private const int Iterations = 600_000; // OWASP recommendation for PBKDF2-HMAC-SHA256
  private const int SaltSize = 16;
  private const int HashSize = 32;

  public static string Hash(string password)
  {
    var salt = RandomNumberGenerator.GetBytes(SaltSize);
    var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
    return $"{Scheme}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
  }

  public static bool Verify(string password, string stored)
  {
    var parts = stored.Split('$');
    if (parts.Length != 4 || parts[0] != Scheme || !int.TryParse(parts[1], out var iterations))
      return false;

    byte[] salt, expected;
    try
    {
      salt = Convert.FromBase64String(parts[2]);
      expected = Convert.FromBase64String(parts[3]);
    }
    catch (FormatException)
    {
      return false;
    }

    var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

    // Constant-time comparison: timing must not reveal how many bytes matched.
    return CryptographicOperations.FixedTimeEquals(actual, expected);
  }
}
