using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.ExportCheck;
using OperationMaps.Wpf.Features.Form4;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Features.TitlePage;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Shell;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Wpf.Features.Overview;

public sealed record OverviewForm(int Id, string Number, string Title, int FilledGroups, int Groups,
    int FilledValues, int Values, bool HasTemplate)
{
  public string Caption => $"Форма {Number}";
  public string ProgressText => $"Групп: {FilledGroups} из {Groups} · Значений: {FilledValues} из {Values}";
  public double Progress => Values == 0 ? (Groups > 0 ? 100 : 0) : 100d * FilledValues / Values;
  public string TemplateStatus => HasTemplate ? "Шаблон доступен" : "Нет шаблона или карты заполнения";
}

public sealed record OverviewTask(string Title, string Detail, int Count, ExportIssue Issue)
{
  public string CountLabel => $"Замечаний: {Count}";
}

public sealed partial class ProjectOverviewViewModel(
    ProjectStore store, ProjectExportCheckService checks, INavigationService navigation,
    IDialogService dialogs, ShellViewModel shell) : ScreenViewModelBase, INavigatedTo
{
  [ObservableProperty] private IReadOnlyList<OverviewForm> _forms = [];
  [ObservableProperty] private IReadOnlyList<OverviewTask> _tasks = [];
  [ObservableProperty] private string _readiness = "Проверяем проект…";
  [ObservableProperty] private string _fieldProgress = "—";
  [ObservableProperty] private string _continueLabel = "Продолжить работу";
  [ObservableProperty] private bool _isLoading;
  [ObservableProperty] private bool _hasIssues;
  public string ProductTitle => store.DocumentTitle ?? store.ProjectName ?? "Проект не открыт";
  public string Designation => string.IsNullOrWhiteSpace(store.DocumentNumber) ? "Обозначение не указано" : store.DocumentNumber;
  public int ComponentCount => store.Components.Count;
  public string MatchProgress => $"{store.Components.Count(c => c.IsMatched)} из {ComponentCount}";
  public string FormCountLabel => $"Рабочие формы · {Forms.Count}";
  public bool HasForms => Forms.Count > 0;
  public IAsyncRelayCommand ExportCommand => shell.ExportReportCommand;
  private int? _continueFormId;
  private bool CanContinue => store.HasProject && !IsLoading;

  public Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default)
      => RefreshAsync(cancellationToken);

  partial void OnIsLoadingChanged(bool value) => ContinueCommand.NotifyCanExecuteChanged();

  [RelayCommand]
  private async Task RefreshAsync(CancellationToken ct = default)
  {
    if (IsLoading) return;
    IsLoading = true;
    var generation = store.Generation;
    try
    {
      var issues = await checks.CheckAsync(ct);
      if (store.Generation != generation) return;
      var requiredForms = store.Components.SelectMany(c => c.Entry.MatchResult.RequiredForms)
          .Where(f => f.Number != "4").DistinctBy(f => f.Id)
          .OrderBy(f => int.TryParse(f.Number, out var n) ? n : int.MaxValue).ThenBy(f => f.Number);
      Forms = requiredForms.Select(form =>
      {
        store.OwnForms.TryGetValue(form.Id, out var state);
        var columns = state?.Columns.ToList() ?? [];
        var parameters = state?.Parameters ?? [];
        return new OverviewForm(form.Id, form.Number, form.Title,
            columns.Count(c => c.GetFillStatus(parameters) == FillStatus.Complete), columns.Count,
            columns.Sum(c => c.GetFilledCount(parameters)), columns.Sum(c => c.GetTotalCount(parameters)),
            checks.CheckTemplate(form.Number) is null);
      }).ToList();
      Tasks = issues.GroupBy(i => (i.Target, i.FormId)).Select(group => new OverviewTask(
          group.Key.Target switch
          {
            ExportIssueTarget.Components => "Проверить компоненты и данные каталога",
            ExportIssueTarget.TitlePage => "Заполнить реквизиты документа",
            ExportIssueTarget.OwnForm => $"Завершить форму {Forms.FirstOrDefault(f => f.Id == group.Key.FormId)?.Number}",
            _ => "Проверить шаблоны приложения",
          }, group.First().Message, group.Count(), group.First())).ToList();
      HasIssues = issues.Count > 0;
      Readiness = !store.HasProject ? "Откройте или импортируйте проект."
          : HasIssues ? $"До полного комплекта осталось замечаний: {issues.Count}" : "Проект готов к экспорту";
      FieldProgress = Forms.Count == 0 ? "Формы ещё не назначены" : $"{Forms.Sum(f => f.FilledValues)} из {Forms.Sum(f => f.Values)}";
      _continueFormId = Forms.Any(f => f.Id == store.LastOwnFormId) ? store.LastOwnFormId
          : store.Components.Any(c => !c.IsMatched) ? null
          : Forms.FirstOrDefault(f => f.FilledValues < f.Values)?.Id ?? Forms.FirstOrDefault()?.Id;
      ContinueLabel = _continueFormId is { } id ? $"Продолжить форму {Forms.First(f => f.Id == id).Number}"
          : store.Components.Any(c => !c.IsMatched) ? "Перейти к компонентам" : "Открыть форму 4";
      OnPropertyChanged(nameof(ProductTitle));
      OnPropertyChanged(nameof(Designation));
      OnPropertyChanged(nameof(ComponentCount));
      OnPropertyChanged(nameof(MatchProgress));
      OnPropertyChanged(nameof(FormCountLabel));
      OnPropertyChanged(nameof(HasForms));
    }
    catch (OperationCanceledException) { }
    catch (Exception ex)
    {
      Readiness = "Не удалось проверить готовность проекта. Повторите обновление.";
      dialogs.ShowError(ex.Message, "Обзор проекта");
    }
    finally { IsLoading = false; }
  }

  [RelayCommand(CanExecute = nameof(CanContinue))]
  private Task ContinueAsync() => _continueFormId is { } id
      ? navigation.NavigateAsync<OwnFormViewModel>(id)
      : store.Components.Any(c => !c.IsMatched) ? OpenComponentsAsync() : OpenForm4Async();

  [RelayCommand] private Task OpenComponentsAsync() => navigation.NavigateAsync<ComponentsViewModel>();
  [RelayCommand] private Task OpenForm4Async() => navigation.NavigateAsync<Form4ViewModel>();
  [RelayCommand] private Task OpenChecksAsync() => navigation.NavigateAsync<ExportCheckViewModel>();
  [RelayCommand] private Task OpenFormAsync(OverviewForm? form) => form is null ? Task.CompletedTask : navigation.NavigateAsync<OwnFormViewModel>(form.Id);

  [RelayCommand]
  private Task OpenTaskAsync(OverviewTask? task) => task?.Issue.Target switch
  {
    ExportIssueTarget.Components => OpenComponentsAsync(),
    ExportIssueTarget.TitlePage => navigation.NavigateAsync<TitlePageViewModel>(),
    ExportIssueTarget.OwnForm when task.Issue.FormId is { } id => navigation.NavigateAsync<OwnFormViewModel>(id),
    _ => OpenChecksAsync(),
  };
}
