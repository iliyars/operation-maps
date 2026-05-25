using OperationMaps.Domain.Enums;

namespace OperationMaps.Domain.Entities.Users;

public class AppUser
{
  public int Id { get; set; }
  public string Login { get; set; } = "";
  public string DisplayName { get; set; } = "";
  public UserRole Role { get; set; }
  public string PasswordHash { get; set; } = "";
}
