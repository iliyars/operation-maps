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

    public void ShowInfo(string message, string? title = null)
    {
      MessageBox.Show(message, title ?? "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public bool? AskYesNoCancel(string message, string? title = null)
    {
      return MessageBox.Show(message, title ?? "OperationMaps", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) switch
      {
        MessageBoxResult.Yes => true,
        MessageBoxResult.No => false,
        _ => null,
      };
    }
  }
}
