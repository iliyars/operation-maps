using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OperationMaps.Application.Importing;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;

namespace OperationMaps.Wpf.Shell.Features.Unresolved
{
  public sealed partial class UnresolvedViewModel : ScreenViewModelBase, INavigatedTo
  {
    public IReadOnlyList<ComponentMatchEntry> Components { get; private set; } = [];

    public Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default)
    {
      if (parameter is ProjectMatchResult result)
        Components = result.Unresolved;

      return Task.CompletedTask;
    }
  }
}
