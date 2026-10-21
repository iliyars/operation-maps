using System.Windows;
using OperationMaps.Wpf.Shared.Controls;

namespace OperationMaps.Wpf.Features.Admin;

public partial class LoginDialog : Window
{
  private LoginDialogViewModel Vm => (LoginDialogViewModel)DataContext;

  public LoginDialog(LoginDialogViewModel vm)
  {
    InitializeComponent();
    WindowChromeFixes.Apply(this);
    DataContext = vm;
    Loaded += (_, _) => LoginBox.Focus();
  }

  private async void Submit_Click(object sender, RoutedEventArgs e)
  {
    if (await Vm.SubmitAsync(PasswordBox.Password, RepeatBox.Password))
      DialogResult = true;
    else
      PasswordBox.Focus();
  }
}
