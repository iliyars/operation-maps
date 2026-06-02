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
    /// Interaction logic for Form4View.xaml
    /// </summary>
    public partial class Form4View : UserControl
    {
        public Form4View()
        {
            InitializeComponent();
        }

        private void NoteInputBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true && sender is TextBox tb)
            {
                tb.Dispatcher.BeginInvoke(
                    () => { tb.Focus(); tb.SelectAll(); },
                    System.Windows.Threading.DispatcherPriority.Input);
            }
            else if (e.NewValue is false)
            {
                Keyboard.ClearFocus();
                Focus();
            }
        }
    }
}
