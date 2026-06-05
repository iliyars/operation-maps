using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace OperationMaps.Wpf.Infrastructure.Navigation
{

  public sealed class NavigationService : INavigationService, IDisposable
  {
    private readonly IServiceProvider _serviceProvider;

    private readonly Stack<NavigationEntry> _backStack = new();

    private readonly Stack<NavigationEntry> _forwardStack = new();

    private bool _disposed;

    public IScreen? CurrentScreen { get; private set; }

    public Type? CurrentScreenType => CurrentScreen?.GetType();

    public bool CanGoBack => _backStack.Count > 0;

    public bool CanGoForward => _forwardStack.Count > 0;

    public event EventHandler<NavigationChangedEventArgs> CurrentScreenChanged = delegate { };

    public NavigationService(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider
        ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task NavigateAsync<TScreen>(
        object? parameter = null,
        bool addToHistory = true,
        CancellationToken cancellationToken = default)
        where TScreen : class, IScreen
        => NavigateCoreAsync(typeof(TScreen), parameter, addToHistory, cancellationToken);

    public async Task<bool> GoBackAsync(CancellationToken cancellationToken = default)
    {
      ThrowIfDisposed();

      if (!CanGoBack) return false;

      await MoveAsync(_backStack, _forwardStack, cancellationToken);
      return true;
    }

    public async Task<bool> GoForwardAsync(CancellationToken cancellationToken = default)
    {
      ThrowIfDisposed();

      if (!CanGoForward) return false;

      await MoveAsync(_forwardStack, _backStack, cancellationToken);
      return true;
    }

    public void Dispose()
    {
      if (_disposed) return;
      _disposed = true;

      TryDispose(CurrentScreen);

      foreach (var entry in _backStack) TryDispose(entry.Screen);
      foreach (var entry in _forwardStack) TryDispose(entry.Screen);

      _backStack.Clear();
      _forwardStack.Clear();
    }


    // ── Private ──────────────────────────────────────────────────────────────

    private async Task NavigateCoreAsync(
        Type screenType,
        object? parameter,
        bool addToHistory,
        CancellationToken cancellationToken)
    {
      ThrowIfDisposed();

      var previousScreen = CurrentScreen;

      if (addToHistory && previousScreen is not null)
      {
        _backStack.Push(new NavigationEntry(previousScreen, parameter));
        _forwardStack.Clear();
      }

      if (previousScreen is INavigatedFrom leavingScreen)
        await leavingScreen.OnNavigatedFromAsync(cancellationToken);

      var resolved = _serviceProvider.GetRequiredService(screenType);

      if (resolved is not IScreen newScreen)
        throw new InvalidOperationException(
            $"Type '{screenType.FullName}' does not implement {nameof(IScreen)}.");

      CurrentScreen = newScreen;

      if (newScreen is INavigatedTo arrivingScreen)
        await arrivingScreen.OnNavigatedToAsync(parameter, cancellationToken);

      CurrentScreenChanged.Invoke(
          this,
          new NavigationChangedEventArgs(previousScreen, CurrentScreen, parameter));
    }

    private async Task MoveAsync(
        Stack<NavigationEntry> source,
        Stack<NavigationEntry> target,
        CancellationToken cancellationToken)
    {
      var previousScreen = CurrentScreen;

      if (previousScreen is not null)
        target.Push(new NavigationEntry(previousScreen, null));

      if (previousScreen is INavigatedFrom leavingScreen)
        await leavingScreen.OnNavigatedFromAsync(cancellationToken);

      var entry = source.Pop();
      CurrentScreen = entry.Screen;

      if (entry.Screen is INavigatedTo arrivingScreen)
        await arrivingScreen.OnNavigatedToAsync(entry.Parameter, cancellationToken);

      CurrentScreenChanged.Invoke(
          this,
          new NavigationChangedEventArgs(previousScreen, CurrentScreen, entry.Parameter));
    }

    private void ThrowIfDisposed()
    {
      ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static void TryDispose(IScreen? screen)
    {
      if (screen is IDisposable disposable)
        disposable.Dispose();
    }

    // ── Nested types ─────────────────────────────────────────────────────────

    private sealed record NavigationEntry(IScreen Screen, object? Parameter);
  }


}
