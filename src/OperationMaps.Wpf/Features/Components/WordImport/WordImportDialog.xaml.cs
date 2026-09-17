using System.Windows;

namespace OperationMaps.Wpf.Features.Components.WordImport
{
  public partial class WordImportDialog : Window
  {
    public WordImportDialog(WordImportDialogViewModel viewModel)
    {
      InitializeComponent();
      DataContext = viewModel;

      Loaded += async (_, _) => await viewModel.InitializeAsync();
    }
  }
}
