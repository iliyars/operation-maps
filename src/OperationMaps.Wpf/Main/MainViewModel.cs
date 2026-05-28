using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Wpf.Shell;

namespace OperationMaps.Wpf.Main
{
  public sealed class MainViewModel : ObservableObject
  {
    public string Title { get; } = "OperationMaps";
    public ShellViewModel Shell { get; }

    public MainViewModel(ShellViewModel shell)
    {
      Shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }
  }
}
