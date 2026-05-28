using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using OperationMaps.Application.Services;

namespace OperationMaps.Wpf.Services
{
  public sealed class FilePicker : IFilePicker
  {
    public Task<string?> PickAsync(string title, string filter)
    {
      var dialog = new OpenFileDialog
      {
        Title = title,
        Filter = filter,
      };

      var result = dialog.ShowDialog() == true ? dialog.FileName : null;

      return Task.FromResult(result);
    }
  }
}
