using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Services;
using OperationMaps.Wpf.Features.Components;
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
    private INavigationService _navigation;

    public WelcomeViewModel(
      IFilePicker filePicker,
      IComponentListImporter importer,
      IComponentMatcher matcher,
      ProjectStore store,
      ShellViewModel shell,
      INavigationService navigation)
    {
      _filePicker = filePicker ?? throw new ArgumentNullException(nameof(FilePicker));
      _importer = importer ?? throw new ArgumentNullException(nameof(importer));
      _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _shell = shell ?? throw new ArgumentNullException(nameof(shell));
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
        // TODO: IDialogService.ShowError(...)
        return;
      }

      ImportResult importResult;
      await using (var stream = File.OpenRead(path))
      {
        importResult = await _importer.ImportAsync(stream, cancellationToken);
      }

      var matchResult = await _matcher.MatchAllAsync(
            importResult.Components, cancellationToken);

      var projectName = Path.GetFileNameWithoutExtension(path);

      _store.Load(projectName, matchResult);

      _shell.OnProjectLoaded(projectName, matchResult);

      await _navigation.NavigateAsync<ComponentsViewModel>(
            parameter: matchResult,
            addToHistory: false, cancellationToken);
    }
  }
}
