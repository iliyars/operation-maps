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
    private readonly SessionStore _session;

    /// <summary>
    /// "OperationMaps — Проект* · администратор: Имя" — the star marks unsaved
    /// changes (as in Word); the suffix makes admin mode impossible to miss.
    /// </summary>
    public string Title
    {
      get
      {
        var title = _store.HasProject
            ? $"OperationMaps — {_store.ProjectName}{(_store.IsDirty ? "*" : "")}"
            : "OperationMaps";
        return _session.CurrentAdmin is { } admin
            ? $"{title} · администратор: {admin.DisplayName}"
            : title;
      }
    }

    public ShellViewModel Shell { get; }

    public MainViewModel(ShellViewModel shell, ProjectStore store, SessionStore session)
    {
      Shell = shell ?? throw new ArgumentNullException(nameof(shell));
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _session = session ?? throw new ArgumentNullException(nameof(session));
      _session.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Title));

      _store.PropertyChanged += (_, e) =>
      {
        if (e.PropertyName is nameof(ProjectStore.ProjectName) or nameof(ProjectStore.IsDirty))
          OnPropertyChanged(nameof(Title));
      };
    }
  }
}
