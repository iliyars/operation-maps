using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Application.Services;

namespace OperationMaps.Wpf.Features.Admin;

/// <summary>
/// "Войти как администратор". On a catalog without any admin yet, the same
/// dialog instead creates the first admin account (login, name, password).
/// </summary>
public sealed partial class LoginDialogViewModel : ObservableObject
{
  private readonly IAdminAccountService _accounts;

  public bool IsFirstAdmin { get; }

  public string Heading => IsFirstAdmin ? "Создание первого администратора" : "Вход администратора";

  public string Hint => IsFirstAdmin
      ? "В базе ещё нет ни одного администратора. Задайте логин и пароль — они понадобятся для входа. " +
        "Других администраторов можно будет добавить позже."
      : "Режим администратора позволяет редактировать справочник ЭРИ. Он действует до выхода или закрытия программы.";

  public string SubmitText => IsFirstAdmin ? "Создать и войти" : "Войти";

  [ObservableProperty] private string _login = "";
  [ObservableProperty] private string _displayName = "";
  [ObservableProperty] private string? _error;
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanSubmit))]
  private bool _isBusy;

  /// <summary>Password hashing takes a moment — no double submits meanwhile.</summary>
  public bool CanSubmit => !IsBusy;

  /// <summary>Set when the dialog succeeded.</summary>
  public AdminAccount? Result { get; private set; }

  public LoginDialogViewModel(IAdminAccountService accounts, bool isFirstAdmin)
  {
    _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    IsFirstAdmin = isFirstAdmin;
  }

  /// <summary>
  /// Passwords come straight from the PasswordBoxes (WPF deliberately doesn't
  /// allow binding them). True when the dialog can close.
  /// </summary>
  public async Task<bool> SubmitAsync(string password, string repeatPassword)
  {
    Error = null;

    if (string.IsNullOrWhiteSpace(Login))
    {
      Error = "Введите логин.";
      return false;
    }

    IsBusy = true;
    try
    {
      if (IsFirstAdmin)
      {
        if (password != repeatPassword)
        {
          Error = "Пароли не совпадают.";
          return false;
        }
        Result = await _accounts.CreateAsync(Login, DisplayName, password);
      }
      else
      {
        Result = await _accounts.VerifyAsync(Login, password);
        if (Result is null)
        {
          // Deliberately not saying which of the two was wrong.
          Error = "Неверный логин или пароль.";
          return false;
        }
      }
      return true;
    }
    catch (InvalidOperationException ex)
    {
      Error = ex.Message;
      return false;
    }
    finally
    {
      IsBusy = false;
    }
  }
}
