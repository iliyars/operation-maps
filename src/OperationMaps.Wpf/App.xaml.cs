using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewLoacation;
using OperationMaps.Wpf.Main;
using OperationMaps.Wpf.Shell.Features.Welcome;
using OperationMaps.Wpf.ViewModels;
using Serilog;

namespace OperationMaps.Wpf;

public partial class App : System.Windows.Application
{
  private IHost _host;

  private static IHost BuildHost() =>
    Host.CreateDefaultBuilder()
      .UseSerilog((ctx, cfg) => cfg
                .MinimumLevel.Information()
                .WriteTo.File(
                    path: "logs/operationmaps-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14))
                   .ConfigureServices((ctx, services) =>
            {
              var connectionString = ctx.Configuration.GetConnectionString("OperationMaps")
                  ?? throw new InvalidOperationException(
                      "Connection string 'OperationMaps' not found.");

              services.AddInfrastructure(connectionString);
              services.AddPresentation();
            })
            .Build();

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        _host = BuildHost();
        await _host.StartAsync();

        // Register View DataTemplates by convention (ViewModel → View)
        ViewTemplateRegistrar.Register(Assembly.GetExecutingAssembly());

        // Seed the database
        await InitializeDatabaseAsync();

        // Navigate to the initial screen
        var navigation = _host.Services.GetRequiredService<INavigationService>();
        await navigation.NavigateAsync<WelcomeViewModel>(addToHistory: false);

        //Show the window
        var vm = _host.Services.GetRequiredService<MainViewModel>();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        // Navigate to the initial screen
        await navigation.NavigateAsync<WelcomeViewModel>(addToHistory: false);
    }

    private async void OnExit(object sender, ExitEventArgs e)
  {
    await _host.StopAsync();
    _host.Dispose();
    Log.CloseAndFlush();
  }

  private async Task InitializeDatabaseAsync()
  {
    using var scope = _host.Services.CreateAsyncScope();
    var db = scope.ServiceProvider
        .GetRequiredService<OperationMapsDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
  }
}
