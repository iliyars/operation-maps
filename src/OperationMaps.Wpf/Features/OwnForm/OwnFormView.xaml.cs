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
    public OwnFormView()
    {
      InitializeComponent();
      Loaded += (_, _) =>
      {
        AttachNavigation(DataContext as OwnFormViewModel);
        if (_navigationVm is not null)
          ComponentListColumn.Width = new GridLength(_navigationVm.WorkspacePreferences.OwnFormListWidth);
      };
      Unloaded += (_, _) => AttachNavigation(null);
      DataContextChanged += (_, _) => { if (IsLoaded) AttachNavigation(DataContext as OwnFormViewModel); };
    }

    private OwnFormViewModel? _navigationVm;

    private void ComponentListSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
    {
      if (_navigationVm is null || ComponentListColumn.ActualWidth <= 0) return;
      _navigationVm.WorkspacePreferences.OwnFormListWidth = Math.Clamp(ComponentListColumn.ActualWidth, 180, 520);
      _navigationVm.WorkspacePreferences.Save();
    }

    private void AttachNavigation(OwnFormViewModel? vm)
    {
      if (_navigationVm is not null) _navigationVm.MissingParameterRequested -= FocusMissingParameter;
      _navigationVm = vm;
      if (vm is not null) vm.MissingParameterRequested += FocusMissingParameter;
    }

    private void FocusMissingParameter(FormParameterRowVm parameter)
    {
      var vm = _navigationVm;
      Dispatcher.BeginInvoke(() =>
      {
        if (vm is null || vm != _navigationVm || !IsLoaded) return;
        UpdateLayout();
        var detail = vm.ParameterDetails.FirstOrDefault(p => p.FormParameterId == parameter.FormParameterId
            || p.HasOptionalRow && p.OptionalFormParameterId == parameter.FormParameterId);
        if (detail is null) return;
        if (ParameterRows.ItemContainerGenerator.ContainerFromItem(detail) is not ContentPresenter container) return;
        container.BringIntoView();
        var inputName = parameter.IsOptional ? "OptionalSchemeInput" : "SchemeInput";
        if (ParameterRows.ItemTemplate.FindName(inputName, container) is TextBox input && input.IsVisible)
        {
          input.Focus();
          input.SelectAll();
        }
        else if (parameter.IsLoadFactorResult && LoadFactorPicker.IsVisible)
          LoadFactorPicker.Focus();
        else
        {
          // A computed row cannot accept input; don't leave focus in an off-screen TextBox.
          container.Focusable = true;
          container.Focus();
        }
      }, System.Windows.Threading.DispatcherPriority.Input);
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

    /// <summary>
    /// Routes a click on a column list item to either single-selection
    /// (plain click) or multi-selection for Merge (Ctrl+click).
    /// MouseBinding cannot branch on Keyboard.Modifiers declaratively,
    /// so the routing happens here.
    /// </summary>
    private void ColumnItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (sender is not Border border) return;
      if (border.Tag is not ColumnListItemVm item) return;
      if (DataContext is not OwnFormViewModel vm) return;

      if (Keyboard.Modifiers == ModifierKeys.Control)
        vm.ToggleColumnSelectionCommand.Execute(item.Column);
      else
        vm.SelectColumnCommand.Execute(item.Column);

      e.Handled = true;
    }

  }
}
