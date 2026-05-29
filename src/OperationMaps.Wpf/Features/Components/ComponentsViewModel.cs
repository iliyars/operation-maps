
using OperationMaps.Application.Importing;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;

namespace OperationMaps.Wpf.Features.Components
{
  public sealed partial class ComponentsViewModel : ScreenViewModelBase, INavigatedTo
  {
    public IReadOnlyList<ComponentMatchEntry> Components { get; private set; } = [];

    public Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default)
    {
      if (parameter is ProjectMatchResult result)
        Components = result.Matched;
      return Task.CompletedTask;
    }
  }
}
