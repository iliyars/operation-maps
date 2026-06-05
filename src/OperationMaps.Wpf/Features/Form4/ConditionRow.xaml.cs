using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OperationMaps.Wpf.Features.Form4
{
    /// <summary>
    /// Interaction logic for ConditionRow.xaml
    /// </summary>
    public partial class ConditionRow : UserControl
    {
        public static readonly DependencyProperty LabelProperty =
     DependencyProperty.Register(nameof(Label), typeof(string), typeof(ConditionRow));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(ConditionRow),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string? Label
        {
            get => (string?)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string? Value
        {
            get => (string?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public ConditionRow() => InitializeComponent();
    }

}

