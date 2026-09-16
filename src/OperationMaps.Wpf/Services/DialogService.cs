using System.Windows;
using OperationMaps.Application.Services;

namespace OperationMaps.Wpf.Services
{
  public sealed class DialogService : IDialogService
  {
    public void ShowError(string message, string? title = null)
    {
      MessageBox.Show(message, title ?? "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowWarning(string message, string? title = null)
    {
      MessageBox.Show(message, title ?? "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
  }
}
