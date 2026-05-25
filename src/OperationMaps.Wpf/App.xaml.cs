using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;
using Serilog;
using Application = System.Windows.Application;

namespace OperationMaps.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
  private readonly IHost _host;

  public App()
  {
    _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
              config.SetBasePath(AppContext.BaseDirectory);
              config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
              ConfigureServices(context.Configuration, services);
            })
            .UseSerilog((context, loggerConfig) =>
            {
              var logFile = context.Configuration["Serilog:LogFile"]
                            ?? "logs/operationmaps-.log";
              loggerConfig
                  .MinimumLevel.Information()
                  .WriteTo.File(
                      Path.Combine(AppContext.BaseDirectory, logFile),
                      rollingInterval: RollingInterval.Day,
                      retainedFileCountLimit: 14);
            })
            .Build();
  }

  private static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
  {
    // строка подключения из appsettings.json
    var connectionString = configuration.GetConnectionString("OperationMaps")
        ?? throw new InvalidOperationException(
            "Connection string 'OperationMaps' not found in appsettings.json");

    // регистрируем слой Infrastructure (DbContext + провайдер SQLite)
    services.AddInfrastructure(connectionString);

    // главное окно — резолвится из DI (не через StartupUri)
    services.AddSingleton<MainWindow>();

    // сюда позже: ViewModels, прикладные сервисы
    // services.AddTransient<MainViewModel>();
  }

  private async void OnStartup(object sender, StartupEventArgs e)
  {
    await _host.StartAsync();

    using (var scope = _host.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<OperationMapsDbContext>();
      await db.Database.MigrateAsync();
    }

    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
    mainWindow.Show();
  }

  private async void OnExit(object sender, ExitEventArgs e)
  {
    await _host.StopAsync();
    _host.Dispose();
    Log.CloseAndFlush();
  }

}

