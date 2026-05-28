using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Shell.Features.Catalog;
using OperationMaps.Wpf.Shell.Features.Welcome;
using OperationMaps.Wpf.ViewModels;

namespace OperationMaps.Wpf.Shell
{
  public sealed partial class ShellViewModel : ObservableObject
  {
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private IScreen? _currentScreen;

    public ObservableCollection<NavItemViewModel> NavItems { get; } = [];

    public ShellViewModel(INavigationService navigation)
    {
      _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

      _navigation.CurrentScreenChanged += OnCurrentScreenChanged;

      BuildNavItems();
    }

    private void OnCurrentScreenChanged(object? sender, NavigationChangedEventArgs e)
    {
      CurrentScreen = e.CurrentScreen;

      foreach (var item in NavItems)
        item.IsActive = item.ScreenType == e.CurrentScreen?.GetType();
    }

    // ── Nav items ─────────────────────────────────────────────────────────────

    private void BuildNavItems()
    {
      NavItems.Add(NavItemViewModel.Separator("ПРОЕКТ"));

      NavItems.Add(new NavItemViewModel
      {
        Label = "Добро пожаловать",
        Icon = "\uE80F",
        ScreenType = typeof(WelcomeViewModel),
        Command = new AsyncRelayCommand(
              () => _navigation.NavigateAsync<WelcomeViewModel>()),
      });

      NavItems.Add(NavItemViewModel.Separator("СИСТЕМА"));

      NavItems.Add(new NavItemViewModel
      {
        Label = "Справочник ЭРИ",
        Icon = "\uE8FD",
        IsVisible = false,
        ScreenType = typeof(CatalogViewModel),
        Command = new AsyncRelayCommand(
              () => _navigation.NavigateAsync<CatalogViewModel>()),
      });
    }

    // ── Admin visibility (called from ShellView code-behind) ──────────────────

    public void ApplyAdminMode(bool isAdmin)
    {
      foreach (var item in NavItems)
      {
        if (item.ScreenType == typeof(CatalogViewModel))
          item.IsVisible = isAdmin;
      }
    }

    // ── Project loaded (called after XML import on Step В) ───────────────────

    public void OnProjectLoaded(string projectName)
    {
      // TODO: reveal Components / Unresolved / Export items
    }

  }
}
