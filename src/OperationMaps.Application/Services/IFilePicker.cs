using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OperationMaps.Application.Services
{
  public interface IFilePicker
  {
    Task<string?> PickAsync(string title, string filter);
  }
}
