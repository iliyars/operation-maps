using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OperationMaps.Wpf.Infrastructure.Navigation
{
  public interface INavigatedTo
  {
    Task OnNavigatedToAsync(object? parameter = null,
                            CancellationToken cancellationToken = default);
  }

  public interface INavigatedFrom
  {
    Task OnNavigatedFromAsync(CancellationToken cancellationToken = default);
  }
}
