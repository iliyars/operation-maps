using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace OperationMaps.Wpf.Infrastructure.Navigation;

public sealed class NavigationService : INavigationService, IDisposable
{
  private readonly IServiceProvider _serviceProvider;
  private readonly Stack<NavigationEntry> _backStack = new();
  private readonly Stack<NavigationEntry> _forwardStack = new();
  // Serialize lifecycle changes, but allow leaving a screen while it initializes.
  private readonly SemaphoreSlim _transitions = new(1, 1);
  private CancellationTokenSource? _initialization;
  private long _version;
  private bool _disposed;
  private object? _currentParameter;

  public IScreen? CurrentScreen { get; private set; }
  public Type? CurrentScreenType => CurrentScreen?.GetType();
  public bool CanGoBack => _backStack.Count > 0;
  public bool CanGoForward => _forwardStack.Count > 0;
  public event EventHandler<NavigationChangedEventArgs> CurrentScreenChanged = delegate { };

  public NavigationService(IServiceProvider serviceProvider)
    => _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

  public async Task NavigateAsync<TScreen>(object? parameter = null, bool addToHistory = true,
      CancellationToken cancellationToken = default) where TScreen : class, IScreen
  {
    await TransitionAsync(
      () => new NavigationEntry(_serviceProvider.GetRequiredService<TScreen>(), parameter),
      previous =>
      {
        if (addToHistory && previous is not null)
        {
          _backStack.Push(previous);
          _forwardStack.Clear();
        }
      }, cancellationToken);
  }

  public Task<bool> GoBackAsync(CancellationToken cancellationToken = default)
    => MoveAsync(_backStack, _forwardStack, cancellationToken);

  public Task<bool> GoForwardAsync(CancellationToken cancellationToken = default)
    => MoveAsync(_forwardStack, _backStack, cancellationToken);

  private Task<bool> MoveAsync(Stack<NavigationEntry> source, Stack<NavigationEntry> target, CancellationToken ct)
    => TransitionAsync(() => source.TryPeek(out var entry) ? entry : null, previous =>
    {
      source.Pop();
      if (previous is not null) target.Push(previous);
    }, ct);

  private async Task<bool> TransitionAsync(Func<NavigationEntry?> destination,
      Action<NavigationEntry?> commitHistory, CancellationToken ct)
  {
    NavigationEntry? previous;
    NavigationEntry next;
    NavigationEntry[] backBefore;
    NavigationEntry[] forwardBefore;
    CancellationTokenSource operation;
    long version;
    Task initialization;
    Task? restoration = null;
    CancellationTokenSource? restoring = null;
    await _transitions.WaitAsync(ct);
    try
    {
      ObjectDisposedException.ThrowIf(_disposed, this);
      ct.ThrowIfCancellationRequested();
      var selected = destination();
      if (selected is null) return false;
      next = selected;
      previous = CurrentScreen is { } current ? new(current, _currentParameter) : null;
      backBefore = _backStack.ToArray();
      forwardBefore = _forwardStack.ToArray();
      if (previous?.Screen is INavigatedFrom leaving)
        await leaving.OnNavigatedFromAsync(ct);
      ct.ThrowIfCancellationRequested();
      ObjectDisposedException.ThrowIf(_disposed, this);
      _initialization?.Cancel();
      operation = CancellationTokenSource.CreateLinkedTokenSource(ct);
      _initialization = operation;
      version = ++_version;
      commitHistory(previous);
      CurrentScreen = next.Screen;
      _currentParameter = next.Parameter;
      initialization = InitializeAndPublishAsync(next, previous?.Screen, operation.Token);
    }
    finally { _transitions.Release(); }

    try
    {
      await initialization;
      return true;
    }
    catch (Exception)
    {
      // A departed screen must not restore itself or report stale failures.
      await _transitions.WaitAsync();
      try
      {
        if (_disposed || version != _version) return false;
        operation.Cancel();
        if (next.Screen is INavigatedFrom leaving)
          try { await leaving.OnNavigatedFromAsync(); }
          catch (Exception ex) { Log.Warning(ex, "Could not leave failed screen"); }
        Restore(_backStack, backBefore);
        Restore(_forwardStack, forwardBefore);
        CurrentScreen = previous?.Screen;
        _currentParameter = previous?.Parameter;
        ++_version;
        // Resume the previous screen after its OnNavigatedFrom cancelled its work.
        if (previous is not null)
        {
          restoring = new CancellationTokenSource();
          _initialization = restoring;
          restoration = InitializeAsync(previous, restoring.Token);
        }
        CurrentScreenChanged.Invoke(this, new(next.Screen, previous?.Screen, previous?.Parameter));
      }
      finally { _transitions.Release(); }
      if (restoration is not null)
        try { await restoration; }
        catch (OperationCanceledException) when (restoring!.IsCancellationRequested) { }
        catch (Exception ex) { Log.Warning(ex, "Could not resume previous screen"); }
      throw;
    }
    finally
    {
      if (ReferenceEquals(_initialization, operation)) _initialization = null;
      if (restoring is not null)
      {
        if (ReferenceEquals(_initialization, restoring)) _initialization = null;
        restoring.Dispose();
      }
      operation.Dispose();
    }
  }

  private static async Task InitializeAsync(NavigationEntry entry, CancellationToken ct)
  {
    if (entry.Screen is INavigatedTo arriving)
      await arriving.OnNavigatedToAsync(entry.Parameter, ct);
    ct.ThrowIfCancellationRequested();
  }

  private async Task InitializeAndPublishAsync(NavigationEntry entry, IScreen? previous, CancellationToken ct)
  {
    // Publish under the transition gate, after synchronous metadata is initialized.
    var initialization = InitializeAsync(entry, ct);
    CurrentScreenChanged.Invoke(this, new(previous, entry.Screen, entry.Parameter));
    await initialization;
  }

  private static void Restore(Stack<NavigationEntry> stack, NavigationEntry[] snapshot)
  {
    stack.Clear();
    foreach (var entry in snapshot.Reverse()) stack.Push(entry);
  }

  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;
    ++_version;
    _initialization?.Cancel();
    foreach (var screen in _backStack.Concat(_forwardStack).Select(e => e.Screen)
        .Append(CurrentScreen).OfType<IScreen>().Distinct())
      if (screen is IDisposable disposable) disposable.Dispose();
    _backStack.Clear();
    _forwardStack.Clear();
  }

  private sealed record NavigationEntry(IScreen Screen, object? Parameter);
}
