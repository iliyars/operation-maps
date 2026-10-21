using System.Windows;
using System.Windows.Controls;

namespace OperationMaps.Wpf.Features.Admin;

/// <summary>
/// PasswordBox.Password can't be data-bound (by WPF design, so the password
/// doesn't linger in bindable properties) — the buttons hand it over here.
/// </summary>
public partial class AdminsView : UserControl
{
  public AdminsView()
  {
    InitializeComponent();
  }

  private AdminsViewModel? Vm => DataContext as AdminsViewModel;

  private async void Add_Click(object sender, RoutedEventArgs e)
  {
    if (Vm is null) return;
    if (await Vm.AddAsync(AddPassword.Password, AddRepeat.Password))
    {
      AddPassword.Clear();
      AddRepeat.Clear();
    }
  }

  private async void ChangePassword_Click(object sender, RoutedEventArgs e)
  {
    if (Vm is null) return;
    if (await Vm.ChangePasswordAsync(ChangePassword.Password, ChangeRepeat.Password))
    {
      ChangePassword.Clear();
      ChangeRepeat.Clear();
    }
  }
}
