using System.Windows;
using OperationMaps.Wpf.Shared.Controls;

namespace OperationMaps.Wpf.Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WindowChromeFixes.Apply(this);
        }
    }
}
