using Microsoft.Extensions.DependencyInjection;
using OperationMaps.Wpf.Infrastructure.Navigation;

namespace OperationMaps.Tests.Wpf;

public sealed class NavigationServiceTests
{
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Leaving_loading_screen_cancels_it_and_ignores_late_success_or_failure(bool fail)
  {
    var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var first = new ControlledScreen { Initialize = _ => ready.Task };
    var second = new ControlledScreen();
    var screens = new Queue<ControlledScreen>([first, second]);
    using var provider = new ServiceCollection().AddTransient(_ => screens.Dequeue()).BuildServiceProvider();
    using var navigation = new NavigationService(provider);
    var changes = new List<IScreen?>();
    navigation.CurrentScreenChanged += (_, e) => changes.Add(e.CurrentScreen);
    var loading = navigation.NavigateAsync<ControlledScreen>(67);
    Assert.Same(first, navigation.CurrentScreen);
    await navigation.NavigateAsync<ControlledScreen>(68);
    Assert.True(first.Token.IsCancellationRequested);
    if (fail) ready.SetException(new IOException("Поздняя ошибка"));
    else ready.SetResult();
    await loading.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Same(second, navigation.CurrentScreen);
    Assert.Equal([first, second], changes);
    // The screen can be visited again after its old initialization has ended.
    first.Initialize = _ => Task.CompletedTask;
    Assert.True(await navigation.GoBackAsync());
    Assert.Same(first, navigation.CurrentScreen);
    Assert.Equal(67, first.Parameter);
    Assert.True(await navigation.GoForwardAsync());
    Assert.Same(second, navigation.CurrentScreen);
    Assert.Equal(68, second.Parameter);
  }

  [Fact]
  public async Task Failed_transition_restores_screen_parameter_and_both_history_stacks()
  {
    var first = new ControlledScreen();
    var second = new ControlledScreen();
    var failed = new ControlledScreen { Initialize = _ => Task.FromException(new IOException("Не удалось открыть")) };
    var screens = new Queue<ControlledScreen>([first, second, failed]);
    using var provider = new ServiceCollection().AddTransient(_ => screens.Dequeue()).BuildServiceProvider();
    using var navigation = new NavigationService(provider);
    await navigation.NavigateAsync<ControlledScreen>(67);
    await navigation.NavigateAsync<ControlledScreen>(68);
    await navigation.GoBackAsync();
    Assert.True(navigation.CanGoForward);
    await Assert.ThrowsAsync<IOException>(() => navigation.NavigateAsync<ControlledScreen>(86));
    Assert.Same(first, navigation.CurrentScreen);
    Assert.Equal(67, first.Parameter);
    Assert.False(navigation.CanGoBack);
    Assert.True(navigation.CanGoForward);
    Assert.Equal(1, failed.LeaveCalls);
    Assert.True(await navigation.GoForwardAsync());
    Assert.Same(second, navigation.CurrentScreen);
    Assert.Equal(68, second.Parameter);
  }

  [Fact]
  public async Task Failed_back_transition_does_not_pop_history()
  {
    var first = new ControlledScreen();
    var second = new ControlledScreen();
    var screens = new Queue<ControlledScreen>([first, second]);
    using var provider = new ServiceCollection().AddTransient(_ => screens.Dequeue()).BuildServiceProvider();
    using var navigation = new NavigationService(provider);
    await navigation.NavigateAsync<ControlledScreen>(67);
    await navigation.NavigateAsync<ControlledScreen>(68);
    first.Initialize = _ => Task.FromException(new IOException("Ошибка формы"));
    await Assert.ThrowsAsync<IOException>(() => navigation.GoBackAsync());
    Assert.Same(second, navigation.CurrentScreen);
    Assert.True(navigation.CanGoBack);
    Assert.False(navigation.CanGoForward);
    first.Initialize = _ => Task.CompletedTask;
    Assert.True(await navigation.GoBackAsync());
    Assert.Same(first, navigation.CurrentScreen);
  }

  [Fact]
  public async Task Cancelled_current_transition_restores_previous_screen()
  {
    var first = new ControlledScreen();
    var second = new ControlledScreen { Initialize = ct => Task.Delay(Timeout.Infinite, ct) };
    var screens = new Queue<ControlledScreen>([first, second]);
    using var provider = new ServiceCollection().AddTransient(_ => screens.Dequeue()).BuildServiceProvider();
    using var navigation = new NavigationService(provider);
    await navigation.NavigateAsync<ControlledScreen>(67);
    using var cancellation = new CancellationTokenSource();
    var pending = navigation.NavigateAsync<ControlledScreen>(68, cancellationToken: cancellation.Token);
    cancellation.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
    Assert.Same(first, navigation.CurrentScreen);
    Assert.False(navigation.CanGoBack);
  }

  [Fact]
  public async Task Resolution_failure_does_not_leave_current_screen_or_change_history()
  {
    using var provider = new ServiceCollection().AddSingleton<ControlledScreen>().BuildServiceProvider();
    using var navigation = new NavigationService(provider);
    await navigation.NavigateAsync<ControlledScreen>(67);
    var current = provider.GetRequiredService<ControlledScreen>();
    await Assert.ThrowsAsync<InvalidOperationException>(() => navigation.NavigateAsync<RecordingScreen>(68));
    Assert.Same(current, navigation.CurrentScreen);
    Assert.Equal(0, current.LeaveCalls);
    Assert.False(navigation.CanGoBack);
    Assert.False(navigation.CanGoForward);
  }

  [Fact]
  public async Task Overlapping_departures_are_serialized_without_overlapping_history_mutations()
  {
    var leaving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var first = new ControlledScreen { Leave = () => leaving.Task };
    var second = new ControlledScreen();
    var third = new ControlledScreen();
    var screens = new Queue<ControlledScreen>([first, second, third]);
    using var provider = new ServiceCollection().AddTransient(_ => screens.Dequeue()).BuildServiceProvider();
    using var navigation = new NavigationService(provider);
    var changes = new List<IScreen?>();
    navigation.CurrentScreenChanged += (_, e) => changes.Add(e.CurrentScreen);
    await navigation.NavigateAsync<ControlledScreen>(67);
    var toSecond = navigation.NavigateAsync<ControlledScreen>(68);
    var toThird = navigation.NavigateAsync<ControlledScreen>(86);
    Assert.False(toSecond.IsCompleted);
    Assert.False(toThird.IsCompleted);
    Assert.Equal(1, first.LeaveCalls);
    leaving.SetResult();
    await Task.WhenAll(toSecond, toThird).WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Same(third, navigation.CurrentScreen);
    Assert.Equal([first, second, third], changes);
    Assert.True(await navigation.GoBackAsync());
    Assert.Same(second, navigation.CurrentScreen);
    Assert.True(await navigation.GoBackAsync());
    Assert.Same(first, navigation.CurrentScreen);
    Assert.Equal(67, first.Parameter);
  }

  public sealed class ControlledScreen : IScreen, INavigatedTo, INavigatedFrom
  {
    public Func<CancellationToken, Task> Initialize { get; set; } = _ => Task.CompletedTask;
    public Func<Task> Leave { get; set; } = () => Task.CompletedTask;
    public CancellationToken Token { get; private set; }
    public object? Parameter { get; private set; }
    public int LeaveCalls { get; private set; }
    public Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default)
    {
      Parameter = parameter;
      Token = cancellationToken;
      return Initialize(cancellationToken);
    }
    public Task OnNavigatedFromAsync(CancellationToken cancellationToken = default)
    {
      LeaveCalls++;
      return Leave();
    }
  }

  [Fact]
  public async Task History_restores_each_screen_with_its_own_parameter_in_both_directions()
  {
    using var provider = new ServiceCollection().AddTransient<RecordingScreen>().BuildServiceProvider();
    using var navigation = new NavigationService(provider);
    var changes = new List<NavigationChangedEventArgs>();
    navigation.CurrentScreenChanged += (_, change) => changes.Add(change);

    await navigation.NavigateAsync<RecordingScreen>(67);
    var first = Assert.IsType<RecordingScreen>(navigation.CurrentScreen);
    await navigation.NavigateAsync<RecordingScreen>(68);
    var second = Assert.IsType<RecordingScreen>(navigation.CurrentScreen);
    await navigation.NavigateAsync<RecordingScreen>();
    var third = Assert.IsType<RecordingScreen>(navigation.CurrentScreen);

    for (var cycle = 0; cycle < 2; cycle++)
    {
      Assert.True(await navigation.GoBackAsync());
      AssertCurrent(second, 68);
      Assert.True(await navigation.GoBackAsync());
      AssertCurrent(first, 67);
      Assert.False(await navigation.GoBackAsync());
      Assert.True(await navigation.GoForwardAsync());
      AssertCurrent(second, 68);
      Assert.True(await navigation.GoForwardAsync());
      AssertCurrent(third, null);
      Assert.False(await navigation.GoForwardAsync());
    }

    void AssertCurrent(RecordingScreen screen, object? parameter)
    {
      Assert.Same(screen, navigation.CurrentScreen);
      Assert.Equal(parameter, screen.Parameters.Last());
      Assert.Same(screen, changes.Last().CurrentScreen);
      Assert.Equal(parameter, changes.Last().Parameter);
    }
  }

  [Fact]
  public async Task Navigation_without_history_still_remembers_current_parameter_for_next_transition()
  {
    using var provider = new ServiceCollection().AddTransient<RecordingScreen>().BuildServiceProvider();
    using var navigation = new NavigationService(provider);
    var parameter = new object();
    await navigation.NavigateAsync<RecordingScreen>(parameter, addToHistory: false);
    var original = Assert.IsType<RecordingScreen>(navigation.CurrentScreen);
    Assert.False(navigation.CanGoBack);

    await navigation.NavigateAsync<RecordingScreen>(68);
    Assert.True(await navigation.GoBackAsync());
    Assert.Same(original, navigation.CurrentScreen);
    Assert.Same(parameter, original.Parameters.Last());

    await navigation.NavigateAsync<RecordingScreen>(86);
    Assert.False(navigation.CanGoForward);
    Assert.True(await navigation.GoBackAsync());
    Assert.Same(parameter, original.Parameters.Last());
  }

  public sealed class RecordingScreen : IScreen, INavigatedTo
  {
    public List<object?> Parameters { get; } = [];

    public Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default)
    {
      Parameters.Add(parameter);
      return Task.CompletedTask;
    }
  }
}
