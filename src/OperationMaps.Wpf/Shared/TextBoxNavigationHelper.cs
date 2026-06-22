using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; // VisualTreeHelper
using System.Windows.Media.Media3D;

namespace OperationMaps.Wpf.Shared
{
  public static class TextBoxNavigationHelper
  {
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(TextBoxNavigationHelper),
            new PropertyMetadata(false, OnEnableChanged));

    public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
    public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not UIElement container) return;

      if ((bool)e.NewValue)
        container.PreviewKeyDown += Container_PreviewKeyDown;
      else
        container.PreviewKeyDown -= Container_PreviewKeyDown;
    }

    private static void Container_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key is not (Key.Enter or Key.Up or Key.Down)) return;
      if (Keyboard.FocusedElement is not TextBox current) return;
      if (sender is not DependencyObject container) return;

      if (current.AcceptsReturn) return;

      var boxes = new List<TextBox>();
      CollectTextBoxes(container, boxes);

      int index = boxes.IndexOf(current);
      if (index < 0) return;

      int targetIndex = e.Key == Key.Up ? index - 1 : index + 1;
      if (targetIndex < 0 || targetIndex >= boxes.Count) return;

      var target = boxes[targetIndex];
      target.Focus();
      target.SelectAll();
      e.Handled = true;
    }

    /// <summary>
    /// Walks the VISUAL tree (not the logical tree) under <paramref name="node"/>.
    /// We must use the visual tree here: the parameter fields live inside an
    /// ItemsControl bound via ItemsSource, and WPF deliberately excludes
    /// ItemsSource-generated content from the logical tree (LogicalChildren
    /// returns empty for an ItemsControl once ItemsSource is set). The visual
    /// tree does contain the generated containers, in the same document order
    /// (the default ItemsControl panel — StackPanel — adds them in source order),
    /// so depth-first visual traversal gives us exactly the order we want.
    /// </summary>
    private static void CollectTextBoxes(DependencyObject node, List<TextBox> result)
    {
      if (node is TextBox { IsEnabled: true, IsVisible: true } tb)
        result.Add(tb);

      if (node is not Visual && node is not System.Windows.Media.Media3D.Visual3D)
        return;

      int count = VisualTreeHelper.GetChildrenCount(node);
      for (int i = 0; i < count; i++)
      {
        var child = VisualTreeHelper.GetChild(node, i);
        CollectTextBoxes(child, result);
      }
    }
  }
}
