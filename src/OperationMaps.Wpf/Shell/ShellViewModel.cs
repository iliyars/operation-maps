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
using OperationMaps.Wpf.Stores;
using OperationMaps.Infrastructure.Word;
using OperationMaps.Application.Word;
using System.IO;

namespace OperationMaps.Wpf.Shell
{
  public sealed partial class ShellViewModel : ObservableObject
  {

    private readonly INavigationService _navigation;
    private readonly ProjectStore _store;
    private readonly WordReportBuilder _reportBuilder;
    private readonly WordFormMapLoader _mapLoader;

    // Keeps references to dynamic form ViewModels so we can call
    // BuildWordFormData() on each during report export.
    // Populated in OnProjectLoaded, cleared on next project load.
    private readonly List<(string FormNumber, Func<WordFormData> BuildData)> _formBuilders = [];

    [ObservableProperty] private IScreen? _currentScreen;
    [ObservableProperty] private bool _isExportingReport;
    [ObservableProperty] private string _themeIcon = "\uE708";
    [ObservableProperty] private string _themeLabel = "Тёмная тема";

    public ObservableCollection<NavItemViewModel> NavItems { get; } = [];

    public ShellViewModel(
        INavigationService navigation,
        ProjectStore store,
        WordReportBuilder reportBuilder,
        WordFormMapLoader mapLoader)
    {
      _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _reportBuilder = reportBuilder ?? throw new ArgumentNullException(nameof(reportBuilder));
      _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));

      _navigation.CurrentScreenChanged += OnCurrentScreenChanged;

      BuildNavItems();
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void OnCurrentScreenChanged(object? sender, NavigationChangedEventArgs e)
    {
      CurrentScreen = e.CurrentScreen;

      int? activeFormId = e.CurrentScreen is OwnFormViewModel own ? own.FormId : null;

      foreach (var item in NavItems)
      {
        if (item.IsSeparator) continue;

        item.IsActive = item.ScreenType == typeof(OwnFormViewModel)
            ? activeFormId.HasValue && item.FormId == activeFormId
            : item.ScreenType == e.CurrentScreen?.GetType();
      }
    }

    // ── Export Report command ─────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanExportReport))]
    private async Task ExportReportAsync(CancellationToken ct = default)
    {
      var formsFolder = _store.FormsFolder;
      var outputPath = _store.ReportDocumentPath;
      if (formsFolder is null || outputPath is null) return;

      Directory.CreateDirectory(formsFolder);

      IsExportingReport = true;
      try
      {
        var coverPath = _mapLoader.GetTemplatePath("Cover");

        // Build (data, templatePath) for each form in order
        var forms = _formBuilders
            .Select(f => (f.BuildData(), _mapLoader.GetTemplatePath(f.FormNumber)))
            .ToList();

        var bytes = await _reportBuilder.BuildAsync(coverPath, forms, ct);
        await File.WriteAllBytesAsync(outputPath, bytes, ct);
      }
      finally
      {
        IsExportingReport = false;
      }
    }

    private bool CanExportReport => _store.HasProject && !IsExportingReport;

    partial void OnIsExportingReportChanged(bool value)
        => ExportReportCommand.NotifyCanExecuteChanged();

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
        Label = "Итоговый документ",
        Icon = "\uE8A5",
        IsVisible = false,
        ScreenType = null,
        Command = ExportReportCommand,
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

    // ── Project loaded ────────────────────────────────────────────────────────

    public void OnProjectLoaded(string projectName, ProjectMatchResult matchResult)
    {
      // Reset form builders from previous project
      _formBuilders.Clear();

      // Make nav items visible
      foreach (var item in NavItems)
      {
        switch (item.ScreenType?.Name)
        {
          case nameof(ComponentsViewModel):
          case nameof(Form4ViewModel):
            item.IsVisible = true;
            break;
          case null when item.Label is "Сохранить .omaps" or "Итоговый документ":
            item.IsVisible = true;
            break;
        }
      }

      // Register Form 4 builder
      _formBuilders.Add(("4", BuildForm4Data));

      // Dynamic own-form nav items
      var formsSection = NavItems.FirstOrDefault(i => i.IsSeparator && i.Label == "ФОРМЫ");
      if (formsSection is null) return;

      var insertIndex = NavItems.IndexOf(formsSection) + 2;

      foreach (var item in NavItems.Where(i => i.IsDynamic).ToList())
        NavItems.Remove(item);

      var forms = matchResult.Matched
          .Concat(matchResult.Unresolved)
          .SelectMany(e => e.MatchResult.RequiredForms)
          .Where(f => f.Number != "4")
          .DistinctBy(f => f.Id)
          .OrderBy(f => f.Number);

      foreach (var form in forms)
      {
        var formId = form.Id;
        var formNumber = form.Number;

        NavItems.Insert(insertIndex++, new NavItemViewModel
        {
          Label = $"Форма {formNumber}",
          Icon = "\uE9D9",
          IsVisible = true,
          IsDynamic = true,
          ScreenType = typeof(OwnFormViewModel),
          FormId = formId,
          Command = new AsyncRelayCommand(
                () => _navigation.NavigateAsync<OwnFormViewModel>(parameter: formId)),
        });

        // Register own-form builder — captured by lambda when ViewModel is active
        // We use a deferred lookup: at export time the current screen may already
        // hold the populated ViewModel. If not navigated yet, data will be empty
        // (the user hasn't opened that form). This is fine — WordService writes
        // empty strings into those slots, and the user can always re-export.
        _formBuilders.Add((formNumber, () => BuildOwnFormData(formNumber, formId)));
      }

      ExportReportCommand.NotifyCanExecuteChanged();
    }

    // ── Data builders (called at export time) ─────────────────────────────────

    private WordFormData BuildForm4Data()
    {
      // If Form4ViewModel is currently active — use its builder directly
      if (_navigation.CurrentScreen is Form4ViewModel form4)
        return form4.BuildWordFormData();

      // Not navigated yet — return empty data with the correct FormNumber
      // so WordService fills blank pages (preserves template structure)
      return new WordFormData { FormNumber = "4" };
    }

    private WordFormData BuildOwnFormData(string formNumber, int formId)
    {
      if (_navigation.CurrentScreen is OwnFormViewModel own && own.FormId == formId)
        return own.BuildWordFormData();

      return new WordFormData { FormNumber = formNumber };
    }

    // ── Theme ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleTheme()
    {
      Shared.Themes.ThemeManager.Instance.Toggle();
      var isDark = Shared.Themes.ThemeManager.Instance.Current == Shared.Themes.AppTheme.Dark;
      ThemeIcon = isDark ? "\uE706" : "\uE708";
      ThemeLabel = isDark ? "Светлая тема" : "Тёмная тема";
    }

    // ── Admin visibility ──────────────────────────────────────────────────────

    public void ApplyAdminMode(bool isAdmin)
    {
      foreach (var item in NavItems)
        if (item.ScreenType == typeof(CatalogViewModel))
          item.IsVisible = isAdmin;
    }


  }
}
