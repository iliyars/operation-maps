using System.Windows;
using System.Windows.Input;

namespace OperationMaps.Wpf.Shared.Behaviors;

/// <summary>
/// Attached behavior for dense data-entry forms (rows of parameter value
/// TextBoxes): lets Enter/Down move focus to the next control and Up move
/// it to the previous one, on top of the usual Tab/Shift+Tab. Both keys are
/// no-ops in a plain single-line TextBox by default, so repurposing them
/// here doesn't take anything away.
/// </summary>
public static class TextBoxNavigation
{
  public static readonly DependencyProperty EnterAndArrowsMoveFocusProperty =
      DependencyProperty.RegisterAttached(
          "EnterAndArrowsMoveFocus",
          typeof(bool),
          typeof(TextBoxNavigation),
          new PropertyMetadata(false, OnEnterAndArrowsMoveFocusChanged));

  public static void SetEnterAndArrowsMoveFocus(DependencyObject element, bool value)
      => element.SetValue(EnterAndArrowsMoveFocusProperty, value);

  public static bool GetEnterAndArrowsMoveFocus(DependencyObject element)
      => (bool)element.GetValue(EnterAndArrowsMoveFocusProperty);

  private static void OnEnterAndArrowsMoveFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is not UIElement element) return;

    if ((bool)e.NewValue)
      element.PreviewKeyDown += OnPreviewKeyDown;
    else
      element.PreviewKeyDown -= OnPreviewKeyDown;
  }

  private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
  {
    if (sender is not UIElement element) return;

    FocusNavigationDirection? direction = e.Key switch
    {
      Key.Enter or Key.Down => FocusNavigationDirection.Next,
      Key.Up => FocusNavigationDirection.Previous,
      _ => null,
    };

    if (direction is null) return;

    element.MoveFocus(new TraversalRequest(direction.Value));
    e.Handled = true;
  }
}
