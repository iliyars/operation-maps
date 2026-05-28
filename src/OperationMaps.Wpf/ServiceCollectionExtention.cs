using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Main;
using OperationMaps.Wpf.Shell;
using OperationMaps.Wpf.Shell.Features.Catalog;
using OperationMaps.Wpf.Shell.Features.Welcome;

namespace OperationMaps.Wpf
{
  public static class ServiceCollectionExtention
  {
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
      // Navigation
      services.AddSingleton<INavigationService, NavigationService>();

      // Shell
      services.AddSingleton<ShellViewModel>();

      // Main window
      services.AddSingleton<MainViewModel>();

      // Screens — transient so each navigation gets a fresh instance
      services.AddTransient<WelcomeViewModel>();
      services.AddTransient<CatalogViewModel>();

      return services;
    }
  }
}
