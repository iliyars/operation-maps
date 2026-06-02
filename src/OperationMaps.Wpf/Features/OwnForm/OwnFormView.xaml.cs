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

namespace OperationMaps.Wpf.Features.OwnForm
{
  /// <summary>
  /// Interaction logic for OwnFormView.xaml
  /// </summary>
  public partial class OwnFormView : UserControl
  {
    private OwnFormViewModel? _vm;
    public OwnFormView()
    {
      InitializeComponent();
      DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender,
         DependencyPropertyChangedEventArgs e)
    {
      if (_vm is not null)
        _vm.SplitRequested -= OnSplitRequested;

      _vm = e.NewValue as OwnFormViewModel;

      if (_vm is not null)
        _vm.SplitRequested += OnSplitRequested;
    }

    private void OnSplitRequested(FormColumnVm column)
    {
      var positions = column.Component.Entry.Imported.Positions;

      var dialog = new SplitDialog.SplitDialog(positions)
      {
        Owner = Window.GetWindow(this)
      };

      if (dialog.ShowDialog() != true || dialog.Result is null)
        return;

      var result = dialog.Result;

      if (result.Count < 2)
        return;

      // Two groups → standard split
      // Always use ExecuteMultiSplit — works for 2 or N groups
      _vm!.ExecuteMultiSplit(column, dialog.Result);


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
