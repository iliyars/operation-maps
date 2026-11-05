using System.Windows;
using OperationMaps.Wpf.Features.OwnForm.Commands;

namespace OperationMaps.Wpf.Features.OwnForm.MergeDialog;

public partial class MergeConflictDialog : Window
{
  private readonly ColumnMergePlan _plan;
  public MergeConflictDialog(ColumnMergePlan plan)
  {
    InitializeComponent();
    _plan = plan;
    DataContext = plan;
  }

  private void Confirm_Click(object sender, RoutedEventArgs e)
  {
    if (!_plan.IsResolved) { ValidationMessage.Visibility = Visibility.Visible; return; }
    DialogResult = true;
  }
}
