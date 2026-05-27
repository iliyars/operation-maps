using System.Windows;
using System.Windows.Controls;
using OperationMaps.Wpf.ViewModels;

namespace OperationMaps.Wpf.Views;

public partial class CatalogView : UserControl
{
  private CatalogViewModel Vm => (CatalogViewModel)DataContext;

  public CatalogView(CatalogViewModel vm)
  {
    InitializeComponent();
    DataContext = vm;
    Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
  }

  /// <summary>
  /// Кнопка «+ Новое в справочнике...» — простой диалог ввода текста.
  /// </summary>
  private async void OnCreateNoteClick(object sender, RoutedEventArgs e)
  {
    var dialog = new Window
    {
      Title = "Новое примечание",
      Width = 500,
      Height = 180,
      WindowStartupLocation = WindowStartupLocation.CenterOwner,
      Owner = Window.GetWindow(this),
      ResizeMode = ResizeMode.NoResize,
    };

    var stack = new StackPanel { Margin = new Thickness(16) };

    stack.Children.Add(new TextBlock
    {
      Text = "Текст примечания:",
      Margin = new Thickness(0, 0, 0, 6),
      FontWeight = FontWeights.SemiBold,
    });

    var textBox = new TextBox
    {
      TextWrapping = TextWrapping.Wrap,
      AcceptsReturn = false,
      Height = 60,
      VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
    };
    stack.Children.Add(textBox);

    var btnPanel = new StackPanel
    {
      Orientation = Orientation.Horizontal,
      HorizontalAlignment = HorizontalAlignment.Right,
      Margin = new Thickness(0, 10, 0, 0),
    };

    var btnOk = new Button
    {
      Content = "Создать",
      Padding = new Thickness(16, 4, 16, 4),
      Margin = new Thickness(0, 0, 8, 0),
      IsDefault = true,
    };
    var btnCancel = new Button
    {
      Content = "Отмена",
      Padding = new Thickness(16, 4, 16, 4),
      IsCancel = true,
    };

    btnOk.Click += (_, _) => { dialog.DialogResult = true; };
    btnCancel.Click += (_, _) => { dialog.DialogResult = false; };

    btnPanel.Children.Add(btnOk);
    btnPanel.Children.Add(btnCancel);
    stack.Children.Add(btnPanel);

    dialog.Content = stack;

    if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text))
    {
      await Vm.CreateNoteCommand.ExecuteAsync(textBox.Text.Trim());
    }
  }
}
