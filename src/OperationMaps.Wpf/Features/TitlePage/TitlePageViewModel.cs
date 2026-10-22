using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Services;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Wpf.Features.TitlePage;

/// <summary>Writes the filled title page next to the project's other Word files.</summary>
public static class TitlePageFile
{
  public const string FileName = "Титульный лист.docx";

  /// <summary>All four fields are needed — an empty placeholder would print as-is.</summary>
  public static bool IsComplete(ProjectStore store)
      => !string.IsNullOrWhiteSpace(store.DocumentNumber)
      && !string.IsNullOrWhiteSpace(store.DocumentTitle)
      && !string.IsNullOrWhiteSpace(store.HeadName)
      && !string.IsNullOrWhiteSpace(store.HeadOccupation);

  /// <summary>Returns the written path. Throws IOException when the file is open in Word.</summary>
  public static async Task<string> WriteAsync(ProjectStore store, ITitlePageService service, CancellationToken ct = default)
  {
    var folder = store.FormsFolder ?? throw new InvalidOperationException("Проект не открыт.");
    Directory.CreateDirectory(folder);

    var bytes = await service.BuildAsync(new TitlePageData(
        store.DocumentNumber ?? "", store.DocumentTitle ?? "", store.HeadName ?? "", store.HeadOccupation ?? ""), ct);

    var path = Path.Combine(folder, FileName);
    await File.WriteAllBytesAsync(path, bytes, ct);
    return path;
  }
}

/// <summary>
/// "Титульный лист": the document number and product name come from the
/// imported XML (and can be corrected here); the head of the organization
/// and their position are typed in by the user — remembered for the next
/// project, since they rarely change.
/// </summary>
public sealed partial class TitlePageViewModel : ScreenViewModelBase, INavigatedFrom
{
  private readonly ProjectStore _store;
  private readonly ITitlePageService _titlePage;
  private readonly IDialogService _dialogs;
  private readonly UserPreferences _preferences;

  public TitlePageViewModel(
      ProjectStore store, ITitlePageService titlePage, IDialogService dialogs, UserPreferences preferences)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    _titlePage = titlePage ?? throw new ArgumentNullException(nameof(titlePage));
    _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
    _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
  }

  public string DocumentNumber
  {
    get => _store.DocumentNumber ?? "";
    set => Set(value, v => _store.DocumentNumber = v);
  }

  public string ProductName
  {
    get => _store.DocumentTitle ?? "";
    set => Set(value, v => _store.DocumentTitle = v);
  }

  public string HeadName
  {
    get => _store.HeadName ?? "";
    set
    {
      Set(value, v => _store.HeadName = v);
      _preferences.LastHeadName = value.Trim();
    }
  }

  public string HeadOccupation
  {
    get => _store.HeadOccupation ?? "";
    set
    {
      Set(value, v => _store.HeadOccupation = v);
      _preferences.LastHeadOccupation = value.Trim();
    }
  }

  public bool IsComplete => TitlePageFile.IsComplete(_store);

  // Fields update on every keystroke; the remembered head is written once on leaving.
  public Task OnNavigatedFromAsync(CancellationToken cancellationToken = default)
  {
    _preferences.Save();
    return Task.CompletedTask;
  }

  private void Set(string value, Action<string> apply, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
  {
    apply(value);
    _store.MarkDirty();
    OnPropertyChanged(name);
    OnPropertyChanged(nameof(IsComplete));
    SaveWordCommand.NotifyCanExecuteChanged();
  }

  [RelayCommand(CanExecute = nameof(IsComplete))]
  private async Task SaveWordAsync()
  {
    try
    {
      var path = await TitlePageFile.WriteAsync(_store, _titlePage);
      _dialogs.ShowInfo($"Титульный лист сохранён:\n{path}", "Титульный лист");
    }
    catch (FileNotFoundException ex)
    {
      _dialogs.ShowError($"{ex.Message}\n{ex.FileName}", "Титульный лист");
    }
    catch (IOException ex)
    {
      _dialogs.ShowWarning(
          $"Не удалось сохранить титульный лист — возможно, файл открыт в Word.\n\n{ex.Message}",
          "Титульный лист");
    }
  }
}
