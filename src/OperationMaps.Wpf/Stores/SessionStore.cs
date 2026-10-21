using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Application.Services;

namespace OperationMaps.Wpf.Stores;

/// <summary>
/// Who is using the app right now. Everyone starts as a regular user;
/// admin mode lasts until "Выйти" or until the app is closed — it is never
/// remembered between runs.
/// </summary>
public sealed partial class SessionStore : ObservableObject
{
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(IsAdmin))]
  private AdminAccount? _currentAdmin;

  public bool IsAdmin => CurrentAdmin is not null;

  public void SignIn(AdminAccount admin) => CurrentAdmin = admin;

  public void SignOut() => CurrentAdmin = null;
}
