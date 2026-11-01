using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Services;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.Components.WordImport;
using OperationMaps.Wpf.Shell;
using OperationMaps.Wpf.Stores;
using OperationMaps.Wpf.Features.Overview;

namespace OperationMaps.Wpf.Features.Welcome
{
  public sealed partial class WelcomeViewModel : ScreenViewModelBase, INavigatedTo
  {
    private readonly IFilePicker _filePicker;
    private readonly IComponentListImporter _importer;
    private readonly IComponentMatcher _matcher;
    private readonly ProjectStore _store;
    private readonly ShellViewModel _shell;
    private readonly IDialogService _dialogService;
    private INavigationService _navigation;
    private readonly UserPreferences _preferences;
    public ObservableCollection<RecentProjectItem> RecentProjects { get; } = [];
    public bool HasRecentProjects => RecentProjects.Count > 0;

    public Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default)
    {
      RecentProjects.Clear();
      foreach (var recent in _preferences.RecentProjects) RecentProjects.Add(new(recent));
      OnPropertyChanged(nameof(HasRecentProjects));
      return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenRecentAsync(RecentProjectItem? item, CancellationToken ct)
    {
      if (item is null) return;
      try
      {
        var result = await _shell.OpenProjectPathAsync(item.Path, ct);
        if (result == ProjectOpenResult.Failed) item.OpenFailed = true;
      }
      catch (OperationCanceledException) { }
    }

    [RelayCommand]
    private void RemoveRecent(RecentProjectItem? item)
    {
      if (item is null) return;
      _preferences.ForgetProject(item.Path);
      RecentProjects.Remove(item);
      OnPropertyChanged(nameof(HasRecentProjects));
    }

    public WelcomeViewModel(
      IFilePicker filePicker,
      IComponentListImporter importer,
      IComponentMatcher matcher,
      ProjectStore store,
      ShellViewModel shell,
      IDialogService dialogService,
      INavigationService navigation,
      UserPreferences preferences)
    {
      _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
      _importer = importer ?? throw new ArgumentNullException(nameof(importer));
      _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _shell = shell ?? throw new ArgumentNullException(nameof(shell));
      _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
      _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
      _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
    }
    [RelayCommand]
    private async Task OpenXmlAsync(CancellationToken cancellationToken)
    {
      var path = await _filePicker.PickAsync(
        title: "Открыть перечень элементов (Pe3 XML)",
        filter: "Pe3 XML файлы (*.xml)|*.xml|Все файлы (*.*)|*.*");

      if (path is null) return;

      if (!await _shell.ConfirmCloseProjectAsync()) return;

      if (!_importer.CanImport(path))
      {
        _dialogService.ShowError($"Файл не является поддерживаемым перечнем элементов:\n{path}");
        return;
      }

      ImportResult importResult;
      try
      {
        await using var stream = File.OpenRead(path);
        importResult = await _importer.ImportAsync(stream, cancellationToken);
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                or InvalidDataException or OperationCanceledException)
      {
        _dialogService.ShowError($"Не удалось открыть файл: {ex.Message}");
        return;
      }

      ProjectMatchResult matchResult;
      try
      {
        matchResult = await _matcher.MatchAllAsync(importResult.Components, cancellationToken);
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        _dialogService.ShowError($"Ошибка при сопоставлении компонентов: {ex.Message}");
        return;
      }

      var projectName = Path.GetFileNameWithoutExtension(path);
      var projectFolderPath = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory;

      _store.Load(projectName, projectFolderPath, matchResult, importResult);

      _shell.OnProjectLoaded(projectName, matchResult);

      await _navigation.NavigateAsync<ProjectOverviewViewModel>(
            addToHistory: false,
            cancellationToken: cancellationToken);
    }

    [RelayCommand]
    private Task OpenProjectAsync()
        => _shell.OpenProjectCommand.ExecuteAsync(null);

    /// <summary>
    /// Opens the "Импорт из Word" flow: picks a filled work-order document
    /// (Form4 + Form67/68/... possibly spanning many pages, bundled in one
    /// file) and navigates to the review screen — a catalog-level action,
    /// independent of any currently open project.
    /// </summary>
    [RelayCommand]
    private async Task ImportFromWordAsync(CancellationToken cancellationToken)
    {
      var path = await _filePicker.PickAsync(
          title: "Импорт из Word",
          filter: "Word документы (*.docx)|*.docx|Все файлы (*.*)|*.*");

      if (string.IsNullOrEmpty(path)) return;

      await _navigation.NavigateAsync<WordImportViewModel>(
          parameter: path,
          addToHistory: true,
          cancellationToken);
    }
  }

  public sealed partial class RecentProjectItem(RecentProject project) : ObservableObject
  {
    public string Path => project.FilePath;
    public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);
    public string Folder => System.IO.Path.GetDirectoryName(Path) ?? "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Status))]
    private bool _openFailed;
    public string Status => OpenFailed ? "Не удалось открыть — проверьте расположение файла"
        : $"Последняя работа: {project.LastOpened.ToLocalTime():dd.MM.yyyy, HH:mm}";
  }
}
