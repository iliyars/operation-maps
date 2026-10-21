using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Wpf.Shell;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Wpf.Main
{
  public sealed class MainViewModel : ObservableObject
  {
    private readonly ProjectStore _store;

    /// <summary>"OperationMaps — Проект*", the star marking unsaved changes (as in Word).</summary>
    public string Title => _store.HasProject
        ? $"OperationMaps — {_store.ProjectName}{(_store.IsDirty ? "*" : "")}"
        : "OperationMaps";

    public ShellViewModel Shell { get; }

    public MainViewModel(ShellViewModel shell, ProjectStore store)
    {
      Shell = shell ?? throw new ArgumentNullException(nameof(shell));
      _store = store ?? throw new ArgumentNullException(nameof(store));

      _store.PropertyChanged += (_, e) =>
      {
        if (e.PropertyName is nameof(ProjectStore.ProjectName) or nameof(ProjectStore.IsDirty))
          OnPropertyChanged(nameof(Title));
      };
    }
  }
}
