using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Wpf.Features.Admin;

public sealed record AdminRowVm(AdminAccount Account, bool IsYou)
{
  public string Login => Account.Login;
  public string DisplayName => Account.DisplayName;
}

/// <summary>
/// "Администраторы" — admin-only screen: every admin has their own login,
/// so someone who leaves is simply removed, without changing anyone else's
/// password. Any admin can add admins and set a new password for any admin
/// (that's how a forgotten password is reset).
/// </summary>
public sealed partial class AdminsViewModel : ScreenViewModelBase, INavigatedTo
{
  private readonly IAdminAccountService _accounts;
  private readonly SessionStore _session;
  private readonly IDialogService _dialogs;

  public ObservableCollection<AdminRowVm> Admins { get; } = [];

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HasSelection))]
  [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
  private AdminRowVm? _selected;

  public bool HasSelection => Selected is not null;

  [ObservableProperty] private string _newLogin = "";
  [ObservableProperty] private string _newDisplayName = "";
  [ObservableProperty] private string? _addError;
  [ObservableProperty] private string? _passwordError;
  [ObservableProperty] private string? _passwordMessage;

  public AdminsViewModel(IAdminAccountService accounts, SessionStore session, IDialogService dialogs)
  {
    _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    _session = session ?? throw new ArgumentNullException(nameof(session));
    _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
  }

  public Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default)
      => ReloadAsync(selectId: _session.CurrentAdmin?.Id, cancellationToken);

  private async Task ReloadAsync(int? selectId, CancellationToken ct = default)
  {
    var me = _session.CurrentAdmin?.Id;
    Admins.Clear();
    foreach (var a in await _accounts.ListAsync(ct))
      Admins.Add(new AdminRowVm(a, a.Id == me));

    Selected = Admins.FirstOrDefault(a => a.Account.Id == selectId);
  }

  partial void OnSelectedChanged(AdminRowVm? value)
  {
    PasswordError = null;
    PasswordMessage = null;
  }

  // ── Add ───────────────────────────────────────────────────────────────────

  /// <summary>Passwords come from PasswordBoxes in the view. True on success.</summary>
  public async Task<bool> AddAsync(string password, string repeat)
  {
    AddError = null;
    if (password != repeat)
    {
      AddError = "Пароли не совпадают.";
      return false;
    }

    try
    {
      var created = await _accounts.CreateAsync(NewLogin, NewDisplayName, password);
      NewLogin = "";
      NewDisplayName = "";
      await ReloadAsync(created.Id);
      return true;
    }
    catch (InvalidOperationException ex)
    {
      AddError = ex.Message;
      return false;
    }
  }

  // ── Change password ───────────────────────────────────────────────────────

  public async Task<bool> ChangePasswordAsync(string password, string repeat)
  {
    PasswordError = null;
    PasswordMessage = null;
    if (Selected is null) return false;

    if (password != repeat)
    {
      PasswordError = "Пароли не совпадают.";
      return false;
    }

    try
    {
      await _accounts.ChangePasswordAsync(Selected.Account.Id, password);
      PasswordMessage = $"Пароль для «{Selected.Login}» изменён.";
      return true;
    }
    catch (InvalidOperationException ex)
    {
      PasswordError = ex.Message;
      return false;
    }
  }

  // ── Delete ────────────────────────────────────────────────────────────────

  [RelayCommand(CanExecute = nameof(CanDelete))]
  private async Task DeleteAsync()
  {
    if (Selected is null) return;

    var answer = _dialogs.AskYesNoCancel(
        $"Удалить администратора «{Selected.Login}»? Он больше не сможет войти в режим администратора.",
        "Удаление администратора");
    if (answer != true) return;

    try
    {
      await _accounts.DeleteAsync(Selected.Account.Id);
      await ReloadAsync(_session.CurrentAdmin?.Id);
    }
    catch (InvalidOperationException ex)
    {
      _dialogs.ShowWarning(ex.Message, "Удаление администратора");
    }
  }

  // Removing yourself while signed in would leave the session pointing at a
  // deleted account; another admin can do it.
  private bool CanDelete => Selected is not null && !Selected.IsYou;
}
