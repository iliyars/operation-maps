using System.Windows;
using OperationMaps.Wpf.Views;

namespace OperationMaps.Wpf;

public partial class MainWindow : Window
{
  public MainWindow(CatalogView catalogView)
  {
    InitializeComponent();
    // Показываем справочник. Позже: сюда придёт NavigationService / TabControl.
    MainContent.Content = catalogView;
  }
}
