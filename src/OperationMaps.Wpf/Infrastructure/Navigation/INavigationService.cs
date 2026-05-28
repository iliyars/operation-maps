namespace OperationMaps.Wpf.Infrastructure.Navigation;

public interface INavigationService
{
  IScreen? CurrentScreen { get; }

  Type? CurrentScreenType { get; }

  bool CanGoBack { get; }

  bool CanGoForward { get; }

  event EventHandler<NavigationChangedEventArgs> CurrentScreenChanged;

  Task NavigateAsync<TScreen>(
    object? parameter = null,
    bool addToHistory = true,
    CancellationToken cancellationToken = default) where TScreen : class, IScreen;

  Task<bool> GoBackAsync(CancellationToken cancellationToken = default);

  Task<bool> GoForwardAsync(CancellationToken cancellationToken = default);

}
