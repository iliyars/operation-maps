using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OperationMaps.Wpf.Features.Form4
{
    public partial class Form4View : UserControl
    {
        private bool _isPanelOpen = false;
        private bool _isPanelAnimating = false;

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

        private void ToggleConditionsPanel_Click(object sender, RoutedEventArgs e)
        {
            if (_isPanelOpen) ClosePanel();
            else OpenPanel();
        }

        private void ConditionsPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isPanelOpen) ClosePanel();
        }

        private void OpenPanel()
        {
            if (_isPanelAnimating) return;
            _isPanelOpen = true;
            ToggleIcon.Text = "\uE76C";
            AnimatePanel(from: 300, to: 0, onComplete: null);
        }

        private void ClosePanel()
        {
            if (_isPanelAnimating) return;
            AnimatePanel(from: 0, to: 300, onComplete: () =>
            {
                _isPanelOpen = false;
                ToggleIcon.Text = "\uE76B";
            });
        }

        private void AnimatePanel(double from, double to, Action? onComplete)
        {
            var transform = (TranslateTransform)ConditionsSideBar.RenderTransform;
            var anim = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };

            _isPanelAnimating = true;
            anim.Completed += (_, _) =>
            {
                _isPanelAnimating = false;
                onComplete?.Invoke();
            };

            transform.BeginAnimation(TranslateTransform.XProperty, anim);
        }
    }
}
