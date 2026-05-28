using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OperationMaps.Wpf.Infrastructure.Navigation
{
  public sealed class NavigationChangedEventArgs : EventArgs
  {
    public IScreen? PreviousScreen { get; }

    public IScreen? CurrentScreen { get; }

    public object? Parameter { get; }

    public NavigationChangedEventArgs(
      IScreen? previousScreen,
      IScreen? currentScreen,
      object? parameter)
    {
      PreviousScreen = previousScreen;
      CurrentScreen = currentScreen;
      Parameter = parameter;
    }
  }
}
