using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OperationMaps.Wpf.Features.Components
{
    /// <summary>
    /// Interaction logic for ComponentsView.xaml
    /// </summary>
    public partial class ComponentsView : UserControl
    {
        // Pane layout is pure view state, but the view is re-created on every
        // navigation — static so the user's choice survives switching screens
        // (for the app session; not saved between runs).
        private static bool _detailsHidden;
        private static double _detailsWidth = 320;

        public ComponentsView()
        {
            InitializeComponent();
            ApplyDetailsState();
        }

        private void ToggleDetails_Click(object sender, RoutedEventArgs e)
        {
            if (!_detailsHidden && DetailColumn.ActualWidth > 0)
                _detailsWidth = DetailColumn.ActualWidth;

            _detailsHidden = !_detailsHidden;
            ApplyDetailsState();
        }

        private void DetailSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (DetailColumn.ActualWidth > 0)
                _detailsWidth = DetailColumn.ActualWidth;
        }

        private void ApplyDetailsState()
        {
            DetailColumn.Width = new GridLength(_detailsHidden ? 0 : _detailsWidth);
            DetailSplitter.Visibility = _detailsHidden ? Visibility.Collapsed : Visibility.Visible;
            DetailToggleIcon.Text = _detailsHidden ? "" : "";
            DetailToggleText.Text = _detailsHidden ? "Показать панель" : "Скрыть панель";
        }
    }
}
