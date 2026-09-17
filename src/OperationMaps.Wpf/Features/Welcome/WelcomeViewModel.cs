using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Services;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.Components.WordImport;
using OperationMaps.Wpf.Shell;
using OperationMaps.Wpf.Stores;
using DocumentFormat.OpenXml.Wordprocessing;

namespace OperationMaps.Wpf.Features.Welcome
{
  public sealed partial class WelcomeViewModel : ScreenViewModelBase
  {
    private readonly IFilePicker _filePicker;
    private readonly IComponentListImporter _importer;
    private readonly IComponentMatcher _matcher;
    private readonly ProjectStore _store;
    private readonly ShellViewModel _shell;
    private readonly IDialogService _dialogService;
    private readonly IWordCatalogImportService _wordImportService;
    private readonly CatalogDbContext _db;
    private INavigationService _navigation;

    public WelcomeViewModel(
      IFilePicker filePicker,
      IComponentListImporter importer,
      IComponentMatcher matcher,
      ProjectStore store,
      ShellViewModel shell,
      IDialogService dialogService,
      IWordCatalogImportService wordImportService,
      CatalogDbContext db,
      INavigationService navigation)
    {
      _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
      _importer = importer ?? throw new ArgumentNullException(nameof(importer));
      _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _shell = shell ?? throw new ArgumentNullException(nameof(shell));
      _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
      _wordImportService = wordImportService ?? throw new ArgumentNullException(nameof(wordImportService));
      _db = db ?? throw new ArgumentNullException(nameof(db));
      _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }
    [RelayCommand]
    private async Task OpenXmlAsync(CancellationToken cancellationToken)
    {
      var path = await _filePicker.PickAsync(
        title: "Открыть перечень элементов (Pe3 XML)",
        filter: "Pe3 XML файлы (*.xml)|*.xml|Все файлы (*.*)|*.*");

      if (path is null) return;

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

      await _navigation.NavigateAsync<ComponentsViewModel>(
            parameter: matchResult,
            addToHistory: false,
            cancellationToken);
    }

    /// <summary>
    /// Opens the "Импорт из Word" flow: picks a filled work-order document
    /// (Form4 + Form67/68/... possibly spanning many pages, bundled in one
    /// file), lets the user review the resolved components, and creates the
    /// ones they confirm — a catalog-level action, independent of any
    /// currently open project.
    /// </summary>
    [RelayCommand]
    private async Task ImportFromWordAsync()
    {
      var path = await _filePicker.PickAsync(
          title: "Импорт из Word",
          filter: "Word документы (*.docx)|*.docx|Все файлы (*.*)|*.*");

      if (string.IsNullOrEmpty(path)) return;

      var dialogVm = new WordImportDialogViewModel(_wordImportService, _db, path);
      var dialog = new WordImportDialog(dialogVm)
      {
        Owner = System.Windows.Application.Current.MainWindow,
      };

      dialog.ShowDialog();
    }
  }
}
