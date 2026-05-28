using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OperationMaps.Wpf.Infrastructure.ViewLoacation
{
  public class ViewTemplateRegistrar
  {
    public static void Register(Assembly assembly)
    {
      ArgumentNullException.ThrowIfNull(assembly);

      var screenViewModelTypes = assembly
          .GetTypes()
          .Where(t =>
              !t.IsAbstract &&
              t.IsAssignableTo(typeof(ScreenViewModelBase)) &&
              t.Name.EndsWith("ViewModel", StringComparison.Ordinal));

      foreach (var viewModelType in screenViewModelTypes)
      {
        // WelcomeViewModel  →  WelcomeView
        var viewName = viewModelType.Name[..^"Model".Length]; // strip "Model"
        var viewFullName = $"{viewModelType.Namespace}.{viewName}";
        var viewType = assembly.GetType(viewFullName);

        if (viewType is null || !viewType.IsAssignableTo(typeof(UserControl)))
          continue;

        var template = new DataTemplate(viewModelType)
        {
          VisualTree = new FrameworkElementFactory(viewType)
        };

        template.Seal();

        System.Windows.Application.Current.Resources.Add(template.DataTemplateKey, template);
      }
    }
  }
}
