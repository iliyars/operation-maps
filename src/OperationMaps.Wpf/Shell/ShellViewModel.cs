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
using OperationMaps.Wpf.Features.Admin;
using OperationMaps.Wpf.Features.TitlePage;
using OperationMaps.Wpf.Services;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.ExportCheck;
using OperationMaps.Wpf.Features.Overview;

namespace OperationMaps.Wpf.Shell
{
  public enum ProjectOpenResult { Opened, Cancelled, Failed }
  public sealed partial class ShellViewModel : ObservableObject
  {

    private readonly INavigationService _navigation;
    private readonly ProjectStore _store;
    private readonly IWordService _wordService;
    private readonly WordFormMapLoader _mapLoader;
    private readonly IDialogService _dialogService;
    private readonly IFilePicker _filePicker;
    private readonly ProjectFileService _projectFile;
    private readonly SessionStore _session;
    private readonly ITitlePageService _titlePage;
    private readonly IContentsPageService _contentsPage;
    private readonly IRegistrationSheetService _registrationSheet;
    private readonly UserPreferences _preferences;
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

    /// <summary>Sidebar shows icons only — frees ~170 px for wide tables.</summary>
    [ObservableProperty] private bool _isSidebarCollapsed;

    public ObservableCollection<NavItemViewModel> NavItems { get; } = [];
    public IEnumerable<NavItemViewModel> MainNavItems => NavItems.Where(i =>
        i.ScreenType != typeof(CatalogViewModel) && i.ScreenType != typeof(AdminsViewModel) && i.Label != "СИСТЕМА"
        && (!i.IsSeparator || i.Label == "ПРОЕКТ" || HasProject));
    public IEnumerable<NavItemViewModel> SystemNavItems => NavItems.Where(i =>
        i.ScreenType == typeof(CatalogViewModel) || i.ScreenType == typeof(AdminsViewModel));
    public bool HasProject => _store.HasProject;
    public string ProjectCaption => _store.ProjectName ?? "Проект не открыт";
    public string ProjectSummary => $"Компонентов: {_store.Components.Count} · Не сопоставлено: {_store.Components.Count(c => !c.IsMatched)}";
    public string SaveStatus => _store.IsDirty ? "Есть несохранённые изменения" : _store.ProjectFilePath is null ? "Проект ещё не сохранён" : "Все изменения сохранены";
    public string ProjectLocation => _store.ProjectFilePath ?? _store.ProjectFolderPath ?? "";
    private readonly HashSet<ProjectComponentVm> _observedComponents = [];

    private void RefreshProjectStatus()
    {
      OnPropertyChanged(nameof(HasProject));
      OnPropertyChanged(nameof(ProjectCaption));
      OnPropertyChanged(nameof(ProjectSummary));
      OnPropertyChanged(nameof(SaveStatus));
      OnPropertyChanged(nameof(ProjectLocation));
      OnPropertyChanged(nameof(MainNavItems));
    }

    private void OnComponentStatusChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(ProjectComponentVm.IsMatched)) RefreshProjectStatus();
    }

    public ShellViewModel(
        INavigationService navigation,
        ProjectStore store,
        IWordService wordService,
        WordFormMapLoader mapLoader,
        IDialogService dialogService,
        IFilePicker filePicker,
        ProjectFileService projectFile,
        SessionStore session,
        ITitlePageService titlePage,
        IContentsPageService contentsPage,
        IRegistrationSheetService registrationSheet,
        UserPreferences preferences,
        IServiceProvider services)
    {
      _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
      _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
      _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
      _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
      _projectFile = projectFile ?? throw new ArgumentNullException(nameof(projectFile));
      _session = session ?? throw new ArgumentNullException(nameof(session));
      _titlePage = titlePage ?? throw new ArgumentNullException(nameof(titlePage));
      _contentsPage = contentsPage ?? throw new ArgumentNullException(nameof(contentsPage));
      _registrationSheet = registrationSheet ?? throw new ArgumentNullException(nameof(registrationSheet));
      _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
      _session.PropertyChanged += (_, e) =>
      {
        if (e.PropertyName != nameof(SessionStore.IsAdmin)) return;
        OnPropertyChanged(nameof(IsAdmin));
        foreach (var item in NavItems.Where(i => i.ScreenType == typeof(AdminsViewModel)))
          item.IsVisible = _session.IsAdmin;
      };
      _services = services ?? throw new ArgumentNullException(nameof(services));

      _store.PropertyChanged += (_, e) =>
      {
        RefreshProjectStatus();
        if (e.PropertyName == nameof(ProjectStore.HasProject))
        {
          SaveProjectCommand.NotifyCanExecuteChanged();
          SaveProjectAsCommand.NotifyCanExecuteChanged();
          CheckProjectCommand.NotifyCanExecuteChanged();
          ExportReportCommand.NotifyCanExecuteChanged();
        }
      };

      _store.Components.CollectionChanged += (_, change) =>
      {
        if (change.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        {
          foreach (var old in _observedComponents) old.PropertyChanged -= OnComponentStatusChanged;
          _observedComponents.Clear();
        }
        if (change.OldItems is not null)
          foreach (ProjectComponentVm old in change.OldItems)
          {
            old.PropertyChanged -= OnComponentStatusChanged;
            _observedComponents.Remove(old);
          }
        if (change.NewItems is not null)
          foreach (ProjectComponentVm component in change.NewItems)
            if (_observedComponents.Add(component)) component.PropertyChanged += OnComponentStatusChanged;
        RefreshProjectStatus();
      };
      NavItems.CollectionChanged += (_, _) =>
      {
        OnPropertyChanged(nameof(MainNavItems));
        OnPropertyChanged(nameof(SystemNavItems));
      };

      _navigation.CurrentScreenChanged += OnCurrentScreenChanged;

      BuildNavItems();
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void OnCurrentScreenChanged(object? sender, NavigationChangedEventArgs e)
    {
      CurrentScreen = e.CurrentScreen;
      if (e.CurrentScreen is OwnFormViewModel workingForm) _store.LastOwnFormId = workingForm.FormId;

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
      OnPropertyChanged(nameof(ActiveNextMissingCommand));
    }

    // ── Edit menu — delegates to whatever the active screen exposes; Undo/Redo
    //    history is scoped per-OwnFormViewModel (editing Form67 shouldn't let
    //    you undo something done in Form68), so there's no single global
    //    stack to bind to. Null when the active screen isn't an own-form,
    //    which WPF's Command binding renders as disabled. ─────────────────────

    public ICommand? ActiveUndoCommand => (CurrentScreen as OwnFormViewModel)?.UndoCommand;
    public ICommand? ActiveRedoCommand => (CurrentScreen as OwnFormViewModel)?.RedoCommand;
    public ICommand? ActiveNextMissingCommand => (CurrentScreen as OwnFormViewModel)?.NextMissingCommand;

    // ── Admin mode ────────────────────────────────────────────────────────────

    public bool IsAdmin => _session.IsAdmin;

    /// <summary>
    /// Password prompt; on a catalog without any admin it creates the first
    /// one instead (whoever sets the catalog up becomes its first admin).
    /// </summary>
    [RelayCommand]
    private async Task SignInAsync()
    {
      var accounts = _services.GetRequiredService<IAdminAccountService>();
      var vm = new LoginDialogViewModel(accounts, isFirstAdmin: !await accounts.AnyAdminExistsAsync());
      var dialog = new LoginDialog(vm) { Owner = System.Windows.Application.Current.MainWindow };

      if (dialog.ShowDialog() == true && vm.Result is not null)
        _session.SignIn(vm.Result);
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
      _session.SignOut();
      if (_navigation.CurrentScreen is AdminsViewModel)
        await _navigation.NavigateAsync<WelcomeViewModel>(addToHistory: false);
    }

    [RelayCommand]
    private Task OpenAdminsAsync() => _navigation.NavigateAsync<AdminsViewModel>();

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
        _preferences.RememberProject(path);
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
      var path = await _filePicker.PickAsync("Открыть проект", ProjectFileService.FileFilter);
      if (path is null) return;
      await OpenProjectPathAsync(path, ct);
    }

    public async Task<ProjectOpenResult> OpenProjectPathAsync(string path, CancellationToken ct = default)
    {
      if (!await ConfirmCloseProjectAsync()) return ProjectOpenResult.Cancelled;

      ProjectMatchResult matchResult;
      try
      {
        matchResult = await _projectFile.OpenAsync(path, ct);
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
      {
        _dialogService.ShowError($"Не удалось открыть проект:\n{path}\n\n{ex.Message}", "Открытие проекта");
        return ProjectOpenResult.Failed;
      }

      _preferences.RememberProject(path);
      OnProjectLoaded(_store.ProjectName!, matchResult);
      await _navigation.NavigateAsync<ProjectOverviewViewModel>(addToHistory: false, cancellationToken: ct);
      return ProjectOpenResult.Opened;
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
        true => await SaveProjectAsync() && !_store.IsDirty,
        false => true,
        null => false,
      };
    }

    // ── Export Report command ─────────────────────────────────────────────────

    /// <summary>
    /// "Собрать итоговый документ": the whole printed set as ONE Word file —
    /// title page, contents, every form, change registration sheet — each
    /// part a section of its own (page format, frame, sheet numbering), see
    /// <see cref="WordDocumentMerger"/>. Parts that can't be built yet
    /// (fields not filled in, no template) are left out and listed.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExportReport))]
    private async Task ExportReportAsync(CancellationToken ct = default)
    {
      var formsFolder = _store.FormsFolder;
      var outputPath = _store.ReportDocumentPath;
      if (formsFolder is null || outputPath is null) return;

      if (IsFileLocked(outputPath))
      {
        _dialogService.ShowWarning(
            $"Файл уже открыт в другой программе:\n{outputPath}\n\nЗакройте файл и повторите.",
            "Файл занят");
        return;
      }

      IsExportingReport = true;
      try
      {
        var issues = await _services.GetRequiredService<ProjectExportCheckService>().CheckAsync(ct);
        if (issues.Count > 0)
        {
          await _navigation.NavigateAsync<ExportCheckViewModel>(issues, cancellationToken: ct);
          var details = string.Join("\n• ", issues.Take(8).Select(i => i.Message));
          if (issues.Count > 8) details += $"\n… и ещё {issues.Count - 8}. Полный список — на экране проверки.";
          if (_dialogService.AskYesNoCancel(
              "Проект содержит замечания:\n• " + details + "\n\nПродолжить экспорт неполного документа?",
              "Проверка перед экспортом") != true) return;
        }

        var plan = await PlanContentsAsync(ct);
        var parts = new List<byte[]>();
        var missing = new List<string>();

        // 1. Title page
        if (TitlePageFile.IsComplete(_store))
          parts.Add(await _titlePage.BuildAsync(TitlePageFile.DataOf(_store), ct));
        else
          missing.Add("титульный лист — не заполнены реквизиты (экран «Титульный лист и содержание»)");

        // 2. Contents
        if (ContentsPageFile.CanWrite(_store))
          parts.Add(await _contentsPage.BuildAsync(ContentsPageFile.DataOf(_store, plan), ct));
        else
          missing.Add("содержание — не указано обозначение документа");

        // 3. Forms, each starting at its sheet from the contents
        var formCount = 0;
        foreach (var (formNumber, buildData) in _formBuilders)
        {
          string templatePath;
          try
          {
            templatePath = _mapLoader.GetTemplatePath(formNumber);
            _mapLoader.Load(formNumber);
          }
          catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException or System.Text.Json.JsonException or InvalidOperationException or FormatException)
          {
            missing.Add($"Форма {formNumber} — нет шаблона Word");
            continue;
          }

          var data = await buildData(ct);
          data.FirstSheetNumber = plan.FirstSheetOf(formNumber);
          parts.Add(await _wordService.ExportAsync(data, templatePath, ct));
          formCount++;
        }

        if (formCount == 0)
        {
          _dialogService.ShowWarning(
              "Ни для одной формы проекта нет шаблона Word — собирать нечего.",
              "Итоговый документ");
          return;
        }

        // 4. Change registration sheet — the set's last sheet
        if (_registrationSheet.TemplateExists)
          parts.Add(await _registrationSheet.BuildAsync(RegistrationSheetFile.DataOf(_store, plan), ct));
        else
          missing.Add($"лист регистрации изменений — нет шаблона {_registrationSheet.TemplatePath}");

        var bytes = WordDocumentMerger.Merge(parts);

        Directory.CreateDirectory(formsFolder);
        await File.WriteAllBytesAsync(outputPath, bytes, ct);

        var message = $"Итоговый документ сохранён ({plan.TotalSheets} лист.):\n{outputPath}";
        if (missing.Count > 0)
          message += "\n\nНе вошло в документ:\n• " + string.Join("\n• ", missing);
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
    /// The contents table: every form that goes into the report, in report
    /// order, with the sheets it will occupy (title page = 1, contents = 2).
    /// Forms without a Word template are left out — the report skips them too.
    /// </summary>
    public async Task<ContentsPlan> PlanContentsAsync(CancellationToken ct = default)
    {
      var numbers = _formBuilders.Select(f => f.FormNumber).ToList();
      await using var db = await _services.GetRequiredService<IDbContextFactory<CatalogDbContext>>()
          .CreateDbContextAsync(ct);
      var titles = await db.Forms
          .AsNoTracking()
          .Where(f => numbers.Contains(f.Number))
          .ToDictionaryAsync(f => f.Number, f => f.Title, ct);

      var planned = new List<PlannedForm>();
      foreach (var (formNumber, buildData) in _formBuilders)
      {
        int perPage;
        try
        {
          _mapLoader.GetTemplatePath(formNumber);
          perPage = _mapLoader.Load(formNumber).ComponentsPerPage;
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException or System.Text.Json.JsonException or InvalidOperationException or FormatException)
        {
          continue;
        }

        var data = await buildData(ct);
        planned.Add(new PlannedForm(
            titles.GetValueOrDefault(formNumber) ?? (formNumber == "3" ? Form3DataBuilder.Title : $"Форма {formNumber}"), data.Components.Count, perPage, formNumber));
      }

      return ContentsPlanner.Plan(planned);
    }

    /// <summary>
    /// Sheet number a single exported form should start at, so its frame
    /// matches the contents (null when the form isn't part of the set).
    /// </summary>
    public async Task<int?> FirstSheetOfAsync(string formNumber, CancellationToken ct = default)
        => (await PlanContentsAsync(ct)).FirstSheetOf(formNumber);

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

    [RelayCommand(CanExecute = nameof(CanExportReport))]
    private Task CheckProjectAsync() => _navigation.NavigateAsync<ExportCheckViewModel>();

    partial void OnIsExportingReportChanged(bool value)
    {
      ExportReportCommand.NotifyCanExecuteChanged();
      CheckProjectCommand.NotifyCanExecuteChanged();
    }

    // ── Nav items ─────────────────────────────────────────────────────────────

    private void BuildNavItems()
    {
      NavItems.Add(NavItemViewModel.Separator("ПРОЕКТ"));
      NavItems.Add(new NavItemViewModel
      {
        Label = "Обзор проекта", Icon = "\uE80F", IsVisible = false,
        ScreenType = typeof(ProjectOverviewViewModel),
        Command = new AsyncRelayCommand(() => _navigation.NavigateAsync<ProjectOverviewViewModel>()),
      });

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
        Label = "Реквизиты",
        Icon = "\uE7C3",
        IsVisible = false,
        ScreenType = typeof(TitlePageViewModel),
        Command = new AsyncRelayCommand(
              () => _navigation.NavigateAsync<TitlePageViewModel>()),
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

      NavItems.Add(NavItemViewModel.Separator("ПОДГОТОВКА"));

      NavItems.Add(new NavItemViewModel
      {
        Label = "Проверка проекта",
        Icon = "\uE73E",
        IsVisible = false,
        ScreenType = typeof(ExportCheckViewModel),
        Command = CheckProjectCommand,
      });

      NavItems.Add(NavItemViewModel.Separator("СИСТЕМА"));

      NavItems.Add(new NavItemViewModel
      {
        Label = "Справочник ЭРИ",
        Icon = "\uE8FD",
        IsVisible = true, // browsing is for everyone; editing will be admin-only
        ScreenType = typeof(CatalogViewModel),
        Command = new AsyncRelayCommand(
              () => _navigation.NavigateAsync<CatalogViewModel>()),
      });

      NavItems.Add(new NavItemViewModel
      {
        Label = "Администраторы",
        Icon = "\uE77B",
        IsVisible = false, // admin mode only
        ScreenType = typeof(AdminsViewModel),
        Command = new AsyncRelayCommand(
              () => _navigation.NavigateAsync<AdminsViewModel>()),
      });
    }

    // ── Project loaded ────────────────────────────────────────────────────────

    public void OnProjectLoaded(string projectName, ProjectMatchResult matchResult)
    {
      // Reset form builders from previous project
      _formBuilders.Clear();

      // The head of the organization rarely changes — offer the last one used.
      // Not an edit of the project, so it mustn't mark it as unsaved.
      var wasDirty = _store.IsDirty;
      if (string.IsNullOrWhiteSpace(_store.HeadName)) _store.HeadName = _preferences.LastHeadName;
      if (string.IsNullOrWhiteSpace(_store.HeadOccupation)) _store.HeadOccupation = _preferences.LastHeadOccupation;
      if (string.IsNullOrWhiteSpace(_store.DevelopedBy)) _store.DevelopedBy = _preferences.LastDevelopedBy;
      if (string.IsNullOrWhiteSpace(_store.CheckedBy)) _store.CheckedBy = _preferences.LastCheckedBy;
      if (string.IsNullOrWhiteSpace(_store.NormControl)) _store.NormControl = _preferences.LastNormControl;
      _store.IsDirty = wasDirty;

      // Make nav items visible
      foreach (var item in NavItems)
      {
        switch (item.ScreenType?.Name)
        {
          case nameof(ComponentsViewModel):
          case nameof(ProjectOverviewViewModel):
          case nameof(TitlePageViewModel):
          case nameof(Form4ViewModel):
          case nameof(ExportCheckViewModel):
            item.IsVisible = true;
            break;
          case null when item.Label is "Сохранить проект" or "Собрать итоговый документ":
            item.IsVisible = true;
            break;
        }
      }

      // Register Form 4 builder
      if (_store.Components.Any(c => Form3DataBuilder.IsBoard(c.Entry.Imported)))
        _formBuilders.Add(("3", ct =>
        {
          ct.ThrowIfCancellationRequested();
          return Task.FromResult(Form3DataBuilder.Build(
            _store.Components.Select(c => c.Entry.Imported), _store.DocumentNumber ?? _store.ProjectName ?? ""));
        }));
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
            .Where(f => f.Number is not ("3" or "4"))
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
      CheckProjectCommand.NotifyCanExecuteChanged();
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

    // ── Sidebar ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

    // ── Theme ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleTheme()
    {
      Shared.Themes.ThemeManager.Instance.Toggle();
      var isDark = Shared.Themes.ThemeManager.Instance.Current == Shared.Themes.AppTheme.Dark;
      ThemeIcon = isDark ? "\uE706" : "\uE708";
      ThemeLabel = isDark ? "Светлая тема" : "Тёмная тема";
    }


  }
}
