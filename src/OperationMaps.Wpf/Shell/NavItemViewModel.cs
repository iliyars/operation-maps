using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OperationMaps.Wpf.Shell
{
  public sealed partial class NavItemViewModel : ObservableObject
  {
    // ── Display ──────────────────────────────────────────────────────────────

    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private string _icon = string.Empty;
    [ObservableProperty] private bool _isSeparator;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private bool _isActive;

    // ── Behaviour ────────────────────────────────────────────────────────────

    /// <summary>
    /// The ViewModel type this item navigates to.
    /// Used by <see cref="ShellViewModel"/> to mark the item as active.
    /// </summary>
    public Type? ScreenType { get; init; }

    public ICommand? Command { get; init; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>Creates a visual section separator (non-clickable heading).</summary>
    public static NavItemViewModel Separator(string label) => new()
    {
      Label = label,
      IsSeparator = true,
    };
  }
}
