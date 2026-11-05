using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Threading;

namespace OperationMaps.Wpf.Services;

/// <summary>UI-thread operation scopes; overlapping work cannot clear another operation's status.</summary>
public sealed class OperationStatus : ObservableObject
{
  private readonly List<Scope> _active = [];
  public bool IsBusy => _active.Count > 0;
  public bool IsIdle => !IsBusy;
  public string Message => _active.LastOrDefault()?.Message ?? "Готово";

  public Scope Begin(string message)
  {
    var scope = new Scope(this, message);
    _active.Add(scope);
    Refresh();
    return scope;
  }

  private void Refresh()
  {
    OnPropertyChanged(nameof(IsBusy));
    OnPropertyChanged(nameof(IsIdle));
    OnPropertyChanged(nameof(Message));
  }

  public sealed class Scope(OperationStatus owner, string message) : IDisposable
  {
    internal string Message { get; private set; } = message;
    private bool _disposed;

    public async Task ReportAsync(string message, CancellationToken ct = default)
    {
      ct.ThrowIfCancellationRequested();
      if (_disposed) return;
      Message = message;
      owner.Refresh();
      // Let WPF render the message before synchronous OpenXML/SQLite work starts.
      if (System.Windows.Application.Current is { } app)
        await app.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background, ct);
    }

    public void Dispose()
    {
      if (_disposed) return;
      _disposed = true;
      owner._active.Remove(this);
      owner.Refresh();
    }
  }
}
