// App.xaml.cs — изменения относительно текущей версии помечены // ★

using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.ViewModels; // ★
using OperationMaps.Wpf.Views;      // ★
using Serilog;

namespace OperationMaps.Wpf;

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
    var connectionString = configuration.GetConnectionString("OperationMaps")
        ?? throw new InvalidOperationException(
            "Connection string 'OperationMaps' not found in appsettings.json");

    services.AddInfrastructure(connectionString);

    // ★ IDbContextFactory нужен CatalogViewModel (создаёт короткие scoped-контексты)
    services.AddDbContextFactory<OperationMapsDbContext>(options =>
        options.UseSqlite(connectionString));

    // ★ ViewModels
    services.AddTransient<CatalogViewModel>();

    // ★ Views
    services.AddTransient<CatalogView>();

    // Главное окно
    services.AddSingleton<MainWindow>();
  }

  private async void OnStartup(object sender, StartupEventArgs e)
  {
    await _host.StartAsync();

    // Сидер
    await using var scope = _host.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<OperationMapsDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);     // ★ подключаем сидер здесь

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
