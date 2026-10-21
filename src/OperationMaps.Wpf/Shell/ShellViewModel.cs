using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
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
using Microsoft.Extensions.DependencyInjection;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Features.Project;

namespace OperationMaps.Wpf.Shell
{
  public sealed partial class ShellViewModel : ObservableObject
  {

    private readonly INavigationService _navigation;
    private readonly ProjectStore _store;
    private readonly WordReportBuilder _reportBuilder;
    private readonly WordFormMapLoader _mapLoader;
    private readonly IDialogService _dialogService;
    private readonly IFilePicker _filePicker;
    private readonly ProjectFileService _projectFile;
    private readonly IServiceProvider _services;

    // One data builder per form of the current project, in report order.
    // Populated in OnProjectLoaded, cleared on next project load. Builders
    // read project state (ProjectStore), not whichever screen happens to be
    // open — so every form lands in the report with its data, not just the
    // one on screen.
    private readonly List<(string FormNumber, Func<CancellationToken, Task<WordFormData>> BuildData)> _formBuilders = [];

    [ObservableProperty] private IScreen? _currentScreen;
    [ObservableProperty] private bool _isExportingReport;
    [ObservableProperty] private string _themeIcon = "\uE708";
    [ObservableProperty] private string _themeLabel = "Тёмная тема";

    public ObservableCollection<NavItemViewModel> NavItems { get; } = [];

    public ShellViewModel(
        INavigationService navigation,
        ProjectStore store,
        WordReportBuilder reportBuilder,
        WordFormMapLoader mapLoader,
        IDialogService dialogService,
        IFilePicker filePicker,
        ProjectFileService projectFile,
        IServiceProvider services)
    {
      _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _reportBuilder = reportBuilder ?? throw new ArgumentNullException(nameof(reportBuilder));
      _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
      _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
      _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
      _projectFile = projectFile ?? throw new ArgumentNullException(nameof(projectFile));
      _services = services ?? throw new ArgumentNullException(nameof(services));

      _store.PropertyChanged += (_, e) =>
      {
        if (e.PropertyName == nameof(ProjectStore.HasProject))
        {
          SaveProjectCommand.NotifyCanExecuteChanged();
          SaveProjectAsCommand.NotifyCanExecuteChanged();
        }
      };

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

      OnPropertyChanged(nameof(ActiveUndoCommand));
      OnPropertyChanged(nameof(ActiveRedoCommand));
    }

    // ── Edit menu — delegates to whatever the active screen exposes; Undo/Redo
    //    history is scoped per-OwnFormViewModel (editing Form67 shouldn't let
    //    you undo something done in Form68), so there's no single global
    //    stack to bind to. Null when the active screen isn't an own-form,
    //    which WPF's Command binding renders as disabled. ─────────────────────

    public ICommand? ActiveUndoCommand => (CurrentScreen as OwnFormViewModel)?.UndoCommand;
    public ICommand? ActiveRedoCommand => (CurrentScreen as OwnFormViewModel)?.RedoCommand;

    // ── Project file (.omaps) ─────────────────────────────────────────────────

    /// <summary>
    /// Saves to the file the project came from; a project imported from XML
    /// and never saved goes next to that XML, named after it.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveProject))]
    private async Task<bool> SaveProjectAsync(CancellationToken ct = default)
    {
      var path = _store.ProjectFilePath ?? _projectFile.DefaultSavePath;
      return path is not null && await SaveProjectToAsync(path, ct);
    }

    [RelayCommand(CanExecute = nameof(CanSaveProject))]
    private async Task SaveProjectAsAsync(CancellationToken ct = default)
    {
      var path = _filePicker.SaveFile("Сохранить проект как", ProjectFileService.FileFilter);
      if (path is null) return;
      await SaveProjectToAsync(path, ct);
    }

    private bool CanSaveProject => _store.HasProject;

    private async Task<bool> SaveProjectToAsync(string path, CancellationToken ct)
    {
      try
      {
        await _projectFile.SaveAsync(path, ct);
        return true;
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
      {
        _dialogService.ShowError($"Не удалось сохранить проект:\n{path}\n\n{ex.Message}", "Сохранение");
        return false;
      }
    }

    [RelayCommand]
    private async Task OpenProjectAsync(CancellationToken ct = default)
    {
      if (!await ConfirmCloseProjectAsync()) return;

      var path = await _filePicker.PickAsync("Открыть проект", ProjectFileService.FileFilter);
      if (path is null) return;

      ProjectMatchResult matchResult;
      try
      {
        matchResult = await _projectFile.OpenAsync(path, ct);
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
      {
        _dialogService.ShowError($"Не удалось открыть проект:\n{path}\n\n{ex.Message}", "Открытие проекта");
        return;
      }

      OnProjectLoaded(_store.ProjectName!, matchResult);
      await _navigation.NavigateAsync<ComponentsViewModel>(parameter: matchResult, addToHistory: false, ct);
    }

    /// <summary>
    /// Before the current project is replaced or the app closes: if there are
    /// unsaved edits, asks whether to save them. False means "stay" — the
    /// user cancelled, or chose to save and saving failed.
    /// </summary>
    public async Task<bool> ConfirmCloseProjectAsync()
    {
      if (!_store.HasProject || !_store.IsDirty) return true;

      var answer = _dialogService.AskYesNoCancel(
          $"Сохранить изменения в проекте «{_store.ProjectName}»?", "Несохранённые изменения");

      return answer switch
      {
        true => await SaveProjectAsync(),
        false => true,
        null => false,
      };
    }

    // ── Export Report command ─────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanExportReport))]
    private async Task ExportReportAsync(CancellationToken ct = default)
    {
      var formsFolder = _store.FormsFolder;
      var outputPath = _store.ReportDocumentPath;
      if (formsFolder is null || outputPath is null) return;

      if (IsFileLocked(outputPath))
      {
        _dialogService.ShowWarning(
            $"Файл уже открыт в другой программе:\n{outputPath}\n\nЗакройте файл и повторите экспорт.",
            "Файл занят");
        return;
      }

      IsExportingReport = true;
      try
      {
        // Forms without a Word template/map yet are skipped (and listed to
        // the user) instead of failing the whole report.
        var forms = new List<(WordFormData, string)>();
        var skipped = new List<string>();

        foreach (var (formNumber, buildData) in _formBuilders)
        {
          string templatePath;
          try
          {
            templatePath = _mapLoader.GetTemplatePath(formNumber);
            _mapLoader.Load(formNumber);
          }
          catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
          {
            skipped.Add(formNumber);
            continue;
          }

          forms.Add((await buildData(ct), templatePath));
        }

        if (forms.Count == 0)
        {
          _dialogService.ShowWarning(
              "Ни для одной формы проекта нет шаблона Word — собирать нечего.",
              "Итоговый документ");
          return;
        }

        var bytes = await _reportBuilder.BuildAsync(TryGetCoverTemplatePath(), forms, ct);

        Directory.CreateDirectory(formsFolder);
        await File.WriteAllBytesAsync(outputPath, bytes, ct);

        var message = $"Итоговый документ сохранён:\n{outputPath}";
        if (skipped.Count > 0)
          message += "\n\nПропущены формы без шаблона Word: " +
                     string.Join(", ", skipped.Select(n => $"Форма {n}"));
        _dialogService.ShowInfo(message, "Итоговый документ");
      }
      catch (OperationCanceledException)
      {
      }
      catch (Exception ex)
      {
        _dialogService.ShowError(
            $"Не удалось собрать итоговый документ.\n\n{ex.Message}",
            "Итоговый документ");
      }
      finally
      {
        IsExportingReport = false;
      }
    }

    /// <summary>
    /// Cover page template, if one is shipped (Word/Resources/FormCover).
    /// Without it the report starts directly with the first form.
    /// </summary>
    private string? TryGetCoverTemplatePath()
    {
      try { return _mapLoader.GetTemplatePath("Cover"); }
      catch (FileNotFoundException) { return null; }
    }

    private static bool IsFileLocked(string path)
    {
      if (!File.Exists(path)) return false;
      try
      {
        using var fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        return false;
      }
      catch (IOException)
      {
        return true;
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
        Label = "Сохранить проект",
        Icon = "\uE74E",
        IsVisible = false,
        ScreenType = null,
        Command = SaveProjectCommand,
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
          case null when item.Label is "Сохранить проект" or "Итоговый документ":
            item.IsVisible = true;
            break;
        }
      }

      // Register Form 4 builder
      _formBuilders.Add(("4", BuildForm4DataAsync));

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

        _formBuilders.Add((formNumber, ct => BuildOwnFormDataAsync(formNumber, formId, ct)));
      }

      ExportReportCommand.NotifyCanExecuteChanged();
    }

    // ── Data builders (called at export time) ─────────────────────────────────

    private async Task<WordFormData> BuildForm4DataAsync(CancellationToken ct)
    {
      if (_navigation.CurrentScreen is Form4ViewModel shown)
        return shown.BuildWordFormData();

      // Form 4 has no table state of its own beyond what ProjectStore holds
      // (notes live on the project's components), so a fresh, off-screen
      // instance builds exactly what the screen would show.
      var form4 = _services.GetRequiredService<Form4ViewModel>();
      await form4.OnNavigatedToAsync(cancellationToken: ct);
      return form4.BuildWordFormData();
    }

    private async Task<WordFormData> BuildOwnFormDataAsync(string formNumber, int formId, CancellationToken ct)
    {
      var state = await _services.GetRequiredService<OwnFormStateService>().GetAsync(formId, ct);
      if (state is null)
        return new WordFormData { FormNumber = formNumber };

      return OwnFormWordDataBuilder.Build(
          state.FormNumber,
          _store.DocumentNumber ?? _store.ProjectName ?? "",
          state.Parameters,
          state.Columns);
    }

    // ── Menu bar ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task GoToWelcomeAsync() => await _navigation.NavigateAsync<WelcomeViewModel>();

    [RelayCommand]
    private void ShowAbout()
    {
      var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
      MessageBox.Show(
          $"OperationMaps{Environment.NewLine}Версия {version}{Environment.NewLine}{Environment.NewLine}" +
          "Подготовка карт эксплуатации ЭРИ по ГОСТ.",
          "О программе",
          MessageBoxButton.OK,
          MessageBoxImage.Information);
    }

    [RelayCommand]
    private void Exit() => System.Windows.Application.Current.MainWindow?.Close(); // Close, not Shutdown: lets MainWindow ask about unsaved changes

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
