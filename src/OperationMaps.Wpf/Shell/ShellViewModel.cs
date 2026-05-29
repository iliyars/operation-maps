using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Application.Importing;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Shell.Features.Catalog;
using OperationMaps.Wpf.Shell.Features.Components;
using OperationMaps.Wpf.Shell.Features.Unresolved;
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

      NavItems.Add(new NavItemViewModel
      {
        Label = "Компоненты",
        Icon = "\uE9D5",
        IsVisible = false,
        ScreenType = typeof(ComponentsViewModel),
        Command = new AsyncRelayCommand(
              () => _navigation.NavigateAsync<ComponentsViewModel>()),
      });

      NavItems.Add(new NavItemViewModel
      {
        Label = "Не найдены",
        Icon = "\uE7BA",
        IsVisible = false,
        ScreenType = typeof(UnresolvedViewModel),
        Command = new AsyncRelayCommand(
              () => _navigation.NavigateAsync<UnresolvedViewModel>()),
      });

      NavItems.Add(NavItemViewModel.Separator("ЭКСПОРТ"));

      NavItems.Add(new NavItemViewModel
      {
        Label = "Сохранить .omaps",
        Icon = "\uE74E",
        IsVisible = false,
        ScreenType = null,
        Command = new AsyncRelayCommand(() => Task.CompletedTask), // TODO
      });

      NavItems.Add(new NavItemViewModel
      {
        Label = "Экспорт Word",
        Icon = "\uE8A5",
        IsVisible = false,
        ScreenType = null,
        Command = new AsyncRelayCommand(() => Task.CompletedTask), // TODO
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

    public void OnProjectLoaded(string projectName, ProjectMatchResult matchResult)
    {
      foreach (var item in NavItems)
      {
        switch (item.ScreenType?.Name)
        {
          case nameof(ComponentsViewModel):
            item.IsVisible = true;
            break;

          case nameof(UnresolvedViewModel):
            item.IsVisible = matchResult.Unresolved.Count > 0;
            break;

          case "SaveProject":
          case "ExportWord":
            item.IsVisible = true;
            break;

          case null when item.Label is "Сохранить .omaps" or "Экспорт Word":
            item.IsVisible = true;
            break;
        }
      }
    }

  }
}
