using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Features.TitlePage;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;

namespace OperationMaps.Wpf.Features.ExportCheck;

public sealed partial class ExportCheckViewModel(
    ProjectExportCheckService checks, INavigationService navigation, IDialogService dialogs)
    : ScreenViewModelBase, INavigatedTo
{
  [ObservableProperty] private IReadOnlyList<ExportIssue> _issues = [];
  [ObservableProperty] private string _summary = "Проверка проекта…";

  public Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default)
  {
    if (parameter is not IReadOnlyList<ExportIssue> checkedIssues) return RefreshAsync(cancellationToken);
    ShowResults(checkedIssues);
    return Task.CompletedTask;
  }

  private void ShowResults(IReadOnlyList<ExportIssue> issues)
  {
    Issues = issues;
    Summary = Issues.Count == 0 ? "Проект готов к экспорту. Проверки пройдены." : $"Найдено замечаний: {Issues.Count}. Исправьте их и повторите проверку.";
  }

  [RelayCommand]
  private async Task RefreshAsync(CancellationToken ct = default)
  {
    try
    {
      ShowResults(await checks.CheckAsync(ct));
    }
    catch (OperationCanceledException) { }
    catch (Exception ex)
    {
      Summary = "Не удалось завершить проверку проекта.";
      dialogs.ShowError(ex.Message, "Проверка проекта");
    }
  }

  [RelayCommand]
  private async Task OpenIssueAsync(ExportIssue? issue)
  {
    switch (issue?.Target)
    {
      case ExportIssueTarget.Components:
        await navigation.NavigateAsync<ComponentsViewModel>();
        break;
      case ExportIssueTarget.TitlePage:
        await navigation.NavigateAsync<TitlePageViewModel>();
        break;
      case ExportIssueTarget.OwnForm when issue.FormId is { } formId:
        await navigation.NavigateAsync<OwnFormViewModel>(formId);
        break;
    }
  }
}
