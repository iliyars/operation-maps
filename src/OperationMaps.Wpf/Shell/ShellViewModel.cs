using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Application.Importing;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Features.Catalog;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.Unresolved;
using OperationMaps.Wpf.Features.Welcome;
using OperationMaps.Wpf.Features.Form4;
using OperationMaps.Wpf.Features.OwnForm;

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

      NavItems.Add(NavItemViewModel.Separator("ФОРМЫ"));

      NavItems.Add(new NavItemViewModel
      {
        Label = "Форма 4",
        Icon = "\uE9D9",
        IsVisible = false,
        ScreenType = typeof(Form4ViewModel),
        Command = new AsyncRelayCommand(
              () => _navigation.NavigateAsync<Form4ViewModel>()),
      });

      NavItems.Add(NavItemViewModel.Separator("ЭКСПОРТ"));

      NavItems.Add(new NavItemViewModel
      {
        Label = "Сохранить .omaps",
        Icon = "\uE74E",
        IsVisible = false,
        ScreenType = null,
        Command = new AsyncRelayCommand(() => Task.CompletedTask),
      });

      NavItems.Add(new NavItemViewModel
      {
        Label = "Экспорт Word",
        Icon = "\uE8A5",
        IsVisible = false,
        ScreenType = null,
        Command = new AsyncRelayCommand(() => Task.CompletedTask),
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
          case nameof(Form4ViewModel):
            item.IsVisible = true;
            break;
          case null when item.Label is "Сохранить .omaps" or "Экспорт Word":
            item.IsVisible = true;
            break;
        }
      }

      var formsSection = NavItems
        .FirstOrDefault(i => i.IsSeparator && i.Label == "ФОРМЫ");

      if (formsSection is null) return;

      var formsIndex = NavItems.IndexOf(formsSection);

      // Remove previously added dynamic form items
      var dynamicItems = NavItems
          .Where(i => i.IsDynamic)
          .ToList();
      foreach (var item in dynamicItems)
        NavItems.Remove(item);

      // Collect unique forms from match result (excluding Form 4)
      var forms = matchResult.Matched
          .Concat(matchResult.Unresolved)
          .SelectMany(e => e.MatchResult.RequiredForms)
          .Where(f => f.Number != "4")
          .DistinctBy(f => f.Id)
          .OrderBy(f => f.Number);

            System.Diagnostics.Debug.WriteLine($"Dynamic forms count: {forms.Count()}");
            foreach (var f in forms)
                System.Diagnostics.Debug.WriteLine($"  Form: {f.Number} - {f.Title}");

            int insertIndex = formsIndex + 2;

      foreach (var form in forms)
      {
        var formId = form.Id;
        var formLabel = $"Форма {form.Number}";

        var navItem = new NavItemViewModel
        {
          Label = formLabel,
          Icon = "\uE9D9",
          IsVisible = true,
          IsDynamic = true,
          ScreenType = typeof(OwnFormViewModel),
          Command = new AsyncRelayCommand(
                () => _navigation.NavigateAsync<OwnFormViewModel>(
                    parameter: formId)),
        };

        NavItems.Insert(insertIndex++, navItem);
      }
    }

  }
}
