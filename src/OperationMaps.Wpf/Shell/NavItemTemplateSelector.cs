using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OperationMaps.Wpf.Shell
{
  public class NavItemTemplateSelector : DataTemplateSelector
  {
    public DataTemplate? SeparatorTemplate { get; set; }
    public DataTemplate? NavItemTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is NavItemViewModel { IsSeparator: true }
            ? SeparatorTemplate
            : NavItemTemplate;
  }
}
