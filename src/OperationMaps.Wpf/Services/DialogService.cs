using System.Windows;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Shared.Dialogs;

namespace OperationMaps.Wpf.Services
{
  public sealed class DialogService : IDialogService
  {
    public void ShowError(string message, string? title = null)
    {
      Show(message, title ?? "Ошибка", MessageDialogKind.Error);
    }

    public void ShowWarning(string message, string? title = null)
    {
      Show(message, title ?? "Предупреждение", MessageDialogKind.Warning);
    }

    public void ShowInfo(string message, string? title = null)
    {
      Show(message, title ?? "Готово", MessageDialogKind.Information);
    }

    public bool? AskYesNoCancel(string message, string? title = null)
    {
      return Show(message, title ?? "OperationMaps", MessageDialogKind.Question);
    }

    private static bool? Show(string message, string title, MessageDialogKind kind)
    {
      var app = System.Windows.Application.Current ?? throw new InvalidOperationException("WPF application is not running.");
      if (!app.Dispatcher.CheckAccess())
        return app.Dispatcher.Invoke(() => Show(message, title, kind));
      var owner = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible)
          ?? (app.MainWindow is { IsVisible: true } main ? main : null);
      var dialog = new MessageDialog(message, title, kind);
      if (owner is not null) dialog.Owner = owner;
      else dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
      dialog.ShowDialog();
      return dialog.Answer;
    }
  }
}
