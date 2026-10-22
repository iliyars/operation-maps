using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewLocation;
using OperationMaps.Wpf.Main;
using OperationMaps.Wpf.Features.Welcome;
using Serilog;
using OperationMaps.Wpf.Shared.Themes;

namespace OperationMaps.Wpf;

public partial class App : System.Windows.Application
{
  private IHost? _host;

  /// <summary>
  /// Absolute, so the path shown to the user in error messages is real
  /// regardless of the working directory the app was started from.
  /// </summary>
  private static readonly string LogsFolder = Path.Combine(AppContext.BaseDirectory, "logs");

  // Guards against a cascade of dialogs when the same failure repeats
  // (e.g. a binding that throws on every layout pass).
  private bool _isShowingError;

  public App()
  {
    // Without these, any exception that escapes a command or event handler
    // closes the app silently — taking unsaved work with it.
    DispatcherUnhandledException += OnDispatcherUnhandledException;
    TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
  }

  private static IHost BuildHost() =>
    Host.CreateDefaultBuilder()
      .UseSerilog((ctx, cfg) => cfg
                .MinimumLevel.Information()
                .WriteTo.File(
                    path: Path.Combine(LogsFolder, "operationmaps-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14))
                   .ConfigureServices((ctx, services) =>
            {
              var connectionString = ctx.Configuration.GetConnectionString("Catalog")
                  ?? throw new InvalidOperationException(
                      "Connection string 'Catalog' not found.");

              var catalogOptions = ctx.Configuration.GetSection("Catalog").Get<CatalogDatabaseOptions>()
                  ?? new CatalogDatabaseOptions();

              services.AddInfrastructure(connectionString, catalogOptions);
              services.AddPresentation();
            })
            .Build();

  private async void OnStartup(object sender, StartupEventArgs e)
  {
    try
    {
      _host = BuildHost();
      await _host.StartAsync();
      ThemeManager.Instance.Initialize(System.Windows.Application.Current.Resources);
      // Register View DataTemplates by convention (ViewModel → View)
      ViewTemplateRegistrar.Register(Assembly.GetExecutingAssembly());

      // Check / upgrade / back up the catalog database
      if (!await InitializeDatabaseAsync())
      {
        Shutdown(1);
        return;
      }

      //Show the window
      var vm = _host.Services.GetRequiredService<MainViewModel>();
      var window = new MainWindow { DataContext = vm };
      window.Show();

      // Navigate to the initial screen
      var navigation = _host.Services.GetRequiredService<INavigationService>();
      await navigation.NavigateAsync<WelcomeViewModel>(addToHistory: false);
    }
    catch (Exception ex)
    {
      // No window yet (or a half-initialized one): without this the process
      // would just vanish, with no hint why.
      Log.Fatal(ex, "Startup failed");
      MessageBox.Show(
          "Не удалось запустить программу.\n\n" +
          $"{ex.Message}\n\n" +
          $"Подробности — в журнале:\n{LogsFolder}",
          "OperationMaps",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      Shutdown(1);
    }
  }

  private async void OnExit(object sender, ExitEventArgs e)
  {
    if (_host is not null)
    {
      await _host.StopAsync();
      _host.Dispose();
    }
    Log.CloseAndFlush();
  }

  /// <summary>False when the program can't work with this database — the user has been told why.</summary>
  private async Task<bool> InitializeDatabaseAsync()
  {
    using var scope = _host!.Services.CreateAsyncScope();
    var initializer = scope.ServiceProvider.GetRequiredService<CatalogDatabaseInitializer>();

    var status = await initializer.InitializeAsync();
    var message = status switch
    {
      CatalogStartupStatus.DatabaseNotFound =>
          "Не найден файл справочника:\n" + initializer.DatabasePath + "\n\n" +
          "Проверьте, что общая папка доступна (подключение к сети), " +
          "или исправьте путь в appsettings.json рядом с программой.",
      CatalogStartupStatus.SchemaUpdateRequired =>
          "Эта версия программы новее базы справочника:\n" + initializer.DatabasePath + "\n\n" +
          "Базу должен обновить администратор — запустив у себя новую версию программы. " +
          "После этого программа здесь заработает.",
      CatalogStartupStatus.ProgramTooOld =>
          "База справочника уже обновлена более новой версией программы:\n" + initializer.DatabasePath + "\n\n" +
          "Обновите OperationMaps до последней версии.",
      _ => null,
    };

    if (message is null) return true;

    Log.Warning("Catalog startup check failed: {Status} ({Path})", status, initializer.DatabasePath);
    MessageBox.Show(message, "OperationMaps", MessageBoxButton.OK, MessageBoxImage.Warning);
    return false;
  }

  // ── Unhandled exceptions ──────────────────────────────────────────────────

  private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
  {
    Log.Error(e.Exception, "Unhandled UI exception");

    // Keep the app alive: the failed action is lost, but the open project
    // (possibly with unsaved edits) is not.
    e.Handled = true;

    if (_isShowingError) return;
    _isShowingError = true;
    try
    {
      MessageBox.Show(
          "Произошла непредвиденная ошибка — последнее действие не выполнено.\n" +
          "Программа продолжит работу; на всякий случай сохраните проект.\n\n" +
          $"{e.Exception.Message}\n\n" +
          $"Подробности — в журнале:\n{LogsFolder}",
          "Ошибка",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
    }
    finally
    {
      _isShowingError = false;
    }
  }

  private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
  {
    // A fire-and-forget task failed with nobody awaiting it — nothing on
    // screen depends on it, so just record it instead of crashing.
    Log.Error(e.Exception, "Unobserved task exception");
    e.SetObserved();
  }

  private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
  {
    // Background-thread failure: the runtime terminates the process after
    // this handler, so all we can do is leave a trace.
    Log.Fatal(e.ExceptionObject as Exception, "Fatal unhandled exception");
    Log.CloseAndFlush();
  }
}
