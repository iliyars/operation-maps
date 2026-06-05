using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Main;
using OperationMaps.Wpf.Services;
using OperationMaps.Wpf.Shell;
using OperationMaps.Wpf.Features.Catalog;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.Unresolved;
using OperationMaps.Wpf.Features.Welcome;
using OperationMaps.Wpf.Stores;
using OperationMaps.Wpf.Features.Form4;
using OperationMaps.Wpf.Features.OwnForm;

namespace OperationMaps.Wpf
{
  public static class ServiceCollectionExtension
  {
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
      // Navigation
      services.AddSingleton<INavigationService, NavigationService>();

      //services
      services.AddSingleton<IFilePicker, FilePicker>();

      // Stores
      services.AddSingleton<ProjectStore>();

      // Shell
      services.AddSingleton<ShellViewModel>();

      // Main window
      services.AddSingleton<MainViewModel>();

      // Screens — transient so each navigation gets a fresh instance
      services.AddTransient<WelcomeViewModel>();
      services.AddTransient<CatalogViewModel>();
      services.AddTransient<ComponentsViewModel>();
      services.AddTransient<Form4ViewModel>();
      services.AddTransient<OwnFormViewModel>();
      services.AddTransient<UnresolvedViewModel>();

      return services;
    }
  }
}
