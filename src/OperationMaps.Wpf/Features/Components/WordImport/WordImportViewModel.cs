using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;

namespace OperationMaps.Wpf.Features.Components.WordImport;

/// <summary>
/// Reads a filled work-order Word document (Form4 + Form67/68/... possibly
/// spread across many pages) via <see cref="IWordCatalogImportService"/>,
/// shows every resolved own-form component for review in a master-detail
/// layout (list on the left, selected component's editable "по НТД"
/// parameters on the right), and creates the selected ones in the catalog —
/// mirroring the "Ввести данные компонента" wizard, just driven from a
/// document instead of manual entry. A screen (navigated to from Welcome),
/// not a dialog — reviewing what a document actually contains benefits from
/// the same amount of room as any other page in the app.
/// </summary>
public sealed partial class WordImportViewModel : ScreenViewModelBase, INavigatedTo, INavigatedFrom
{
  private readonly IWordCatalogImportService _importService;
  private readonly IDbContextFactory<CatalogDbContext> _dbFactory;
  private readonly INavigationService _navigation;

  private CancellationTokenSource? _operationCts;
  private CancellationTokenSource? _detailCts;
  private int _operationVersion;
  private int _detailVersion;
  private bool _leaving;
  [ObservableProperty] private string _busyText = "Открытие документа…";
  [ObservableProperty] private int _progressCompleted;
  [ObservableProperty] private int _progressTotal;
  public bool IsBusy => IsLoading || IsImporting;
  public string BusyTitle => IsImporting ? "Сохранение компонентов" : "Чтение документа Word";
  public bool IsProgressIndeterminate => ProgressTotal == 0;
  public double ProgressPercent => ProgressTotal == 0 ? 0 : 100.0 * ProgressCompleted / ProgressTotal;
  partial void OnProgressCompletedChanged(int value) => OnPropertyChanged(nameof(ProgressPercent));
  partial void OnProgressTotalChanged(int value)
  {
    OnPropertyChanged(nameof(ProgressPercent));
    OnPropertyChanged(nameof(IsProgressIndeterminate));
  }

  public string? DocumentPath { get; private set; }
  public string DocumentFileName => DocumentPath is null ? "" : System.IO.Path.GetFileName(DocumentPath);

  [ObservableProperty] private bool _isLoading = true;
  [ObservableProperty] private bool _isImporting;
  [ObservableProperty] private string? _errorMessage;
  [ObservableProperty] private string? _resultSummary;

  public ObservableCollection<WordImportRowVm> Rows { get; } = [];
  public ObservableCollection<ComponentTypeOption> ComponentTypeOptions { get; } = [];

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HasUnsupportedForms))]
  [NotifyPropertyChangedFor(nameof(UnsupportedFormsDisplay))]
  private IReadOnlyList<string> _unsupportedFormNumbers = [];

  public bool HasUnsupportedForms => UnsupportedFormNumbers.Count > 0;
  public string UnsupportedFormsDisplay => string.Join(", ", UnsupportedFormNumbers);

  // ── Detail panel (selected row) ─────────────────────────────────────────

  [ObservableProperty] private WordImportRowVm? _selectedRow;
  [ObservableProperty] private bool _selectedShowsPins;

  public ObservableCollection<WordImportParameterRowVm> SelectedParameters { get; } = [];
  public ObservableCollection<Form4SummaryRowVm> SelectedForm4Summary { get; } = [];
  public bool HasForm4Summary => SelectedForm4Summary.Count > 0;

  public WordImportViewModel(
      IWordCatalogImportService importService,
      IDbContextFactory<CatalogDbContext> dbFactory,
      INavigationService navigation)
  {
    _importService = importService ?? throw new ArgumentNullException(nameof(importService));
    _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
  }

  // ── INavigatedTo ─────────────────────────────────────────────────────────

  public async Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default)
  {
    if (parameter is not string path) return;

    _leaving = false;
    DocumentPath = path;
    OnPropertyChanged(nameof(DocumentPath));
    OnPropertyChanged(nameof(DocumentFileName));
    await InitializeAsync(cancellationToken);
  }

  public Task OnNavigatedFromAsync(CancellationToken cancellationToken = default)
  {
    _leaving = true;
    _operationCts?.Cancel();
    _detailCts?.Cancel();
    foreach (var row in Rows) row.CancelTypeResolution();
    return Task.CompletedTask;
  }

  private async Task InitializeAsync(CancellationToken ct)
  {
    _operationCts?.Cancel();
    foreach (var row in Rows) row.CancelTypeResolution();
    using var operation = CancellationTokenSource.CreateLinkedTokenSource(ct);
    _operationCts = operation;
    var token = operation.Token;
    var version = ++_operationVersion;
    var path = DocumentPath!;
    IsLoading = true;
    ErrorMessage = null;
    ResultSummary = null;
    BusyText = "Открытие документа…";
    ProgressCompleted = 0;
    ProgressTotal = 0;
    var progress = new Progress<WordImportProgress>(value =>
    {
      if (version != _operationVersion || _leaving || token.IsCancellationRequested || !IsLoading) return;
      BusyText = value.Message;
      ProgressCompleted = value.Completed;
      ProgressTotal = value.Total;
    });
    try
    {
      // SQLite async APIs can complete synchronously; create and use each context in the worker.
      var payload = await Task.Run(async () =>
      {
        await using var db = await _dbFactory.CreateDbContextAsync(token);
        var types = await db.ComponentTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync(token);
        var scan = _importService is IWordCatalogImportProgress reporting
          ? await reporting.ScanAsync(path, progress, token)
          : await _importService.ScanAsync(path, token);
        return (Types: types, Scan: scan);
      }, token);
      if (version != _operationVersion || _leaving) return;
      token.ThrowIfCancellationRequested();
      ComponentTypeOptions.Clear();
      foreach (var t in payload.Types) ComponentTypeOptions.Add(new ComponentTypeOption(t.Id, t.Name));
      UnsupportedFormNumbers = payload.Scan.UnsupportedFormNumbers;
      SelectedRow = null;
      Rows.Clear();
      foreach (var component in payload.Scan.Components)
      {
        var row = new WordImportRowVm(component, _importService) { Number = Rows.Count + 1 };
        row.PropertyChanged += (_, e) =>
        {
          if (e.PropertyName is nameof(WordImportRowVm.IsSelected) or nameof(WordImportRowVm.IsImported))
            ImportSelectedCommand.NotifyCanExecuteChanged();
        };
        Rows.Add(row);
      }
      SelectedRow = Rows.FirstOrDefault();
    }
    catch (OperationCanceledException) when (token.IsCancellationRequested)
    {
      if (version == _operationVersion && !_leaving) ResultSummary = "Чтение документа отменено.";
    }
    catch (Exception ex)
    {
      if (version == _operationVersion && !_leaving) ErrorMessage = $"Не удалось прочитать документ: {ex.Message}";
    }
    finally
    {
      if (version == _operationVersion)
      {
        _operationCts = null;
        IsLoading = false;
        ImportSelectedCommand.NotifyCanExecuteChanged();
      }
    }
  }

  // ── Detail panel ─────────────────────────────────────────────────────────

  partial void OnSelectedRowChanged(WordImportRowVm? value) => _ = LoadDetailAsync(value);

  private async Task LoadDetailAsync(WordImportRowVm? row)
  {
    _detailCts?.Cancel();
    var version = ++_detailVersion;
    SelectedParameters.Clear();
    SelectedForm4Summary.Clear();
    OnPropertyChanged(nameof(HasForm4Summary));
    if (row is null || _leaving) return;
    using var operation = new CancellationTokenSource();
    _detailCts = operation;
    var token = operation.Token;
    try
    {
      var data = await Task.Run(async () =>
      {
        await using var db = await _dbFactory.CreateDbContextAsync(token);
        var own = await db.Forms.AsNoTracking().Include(f => f.Parameters)
          .FirstOrDefaultAsync(f => f.Id == row.Model.OwnFormId, token);
        var form4 = row.Model.Form4NtdValuesByRow.Count > 0
          ? await db.Forms.AsNoTracking().Include(f => f.Parameters).FirstOrDefaultAsync(f => f.Number == "4", token) : null;
        return (Own: own, Form4: form4);
      }, token);
      if (version != _detailVersion || row != SelectedRow || _leaving || data.Own is null) return;
      SelectedShowsPins = data.Own.Number == "64";
      foreach (var param in data.Own.Parameters.Where(p => !p.IsOptional).OrderBy(p => p.RowNumber))
        SelectedParameters.Add(new WordImportParameterRowVm(param, row.Model, SelectedShowsPins));
      if (data.Form4 is not null)
      {
        var paramsByRow = data.Form4.Parameters.ToDictionary(p => p.RowNumber);
        foreach (var (rowNumber, value) in row.Model.Form4NtdValuesByRow.OrderBy(kv => kv.Key))
        {
          if (!paramsByRow.TryGetValue(rowNumber, out var param)) continue;
          var name = param.Unit is { Length: > 0 } ? $"{param.Name}, {param.Unit}" : param.Name;
          var notes = row.Model.Form4NotesByRow.TryGetValue(rowNumber, out var texts)
            ? string.Join("\n", texts.Select(t => $"* {t}")) : "";
          SelectedForm4Summary.Add(new Form4SummaryRowVm(name, value, notes));
        }
        OnPropertyChanged(nameof(HasForm4Summary));
      }
    }
    catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    catch (Exception ex)
    {
      if (version == _detailVersion && !_leaving) ErrorMessage = $"Не удалось загрузить параметры: {ex.Message}";
    }
    finally { if (version == _detailVersion) _detailCts = null; }
  }

  // ── Commands ─────────────────────────────────────────────────────────────

  public bool CanImportSelected => !IsImporting
      && !IsLoading
      && Rows.Any(r => r.IsSelected && !r.IsImported && r.CanToggleSelection);

  [RelayCommand(CanExecute = nameof(CanImportSelected))]
  private async Task ImportSelectedAsync(CancellationToken ct = default)
  {
    if (!CanImportSelected) return;
    using var operation = CancellationTokenSource.CreateLinkedTokenSource(ct);
    _operationCts = operation;
    var token = operation.Token;
    var version = ++_operationVersion;
    var selected = Rows.Where(r => r.IsSelected && !r.IsImported && r.CanToggleSelection).ToList();
    IsImporting = true;
    ResultSummary = null;
    ErrorMessage = null;
    ProgressTotal = selected.Count;
    ProgressCompleted = 0;
    int created = 0, failed = 0;
    try
    {
      for (var index = 0; index < selected.Count; index++)
      {
        token.ThrowIfCancellationRequested();
        var row = selected[index];
        BusyText = $"Компонент {index + 1} из {selected.Count}: {row.FullName}";
        try
        {
          // Keep writes sequential for SQLite; the UI remains free throughout each transaction.
          await Task.Run(() => _importService.ImportAsync(row.Model, token), token);
          created++;
          if (!_leaving && version == _operationVersion) row.MarkImported();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
          failed++;
          if (!_leaving && version == _operationVersion) row.MarkFailed(ex.Message);
        }
        if (version != _operationVersion || _leaving) return;
        ProgressCompleted = index + 1;
      }
      ResultSummary = failed == 0 ? $"Импортировано компонентов: {created}."
        : $"Импортировано: {created}. Не удалось: {failed} — см. ошибки в списке.";
    }
    catch (OperationCanceledException) when (token.IsCancellationRequested)
    {
      if (version == _operationVersion && !_leaving)
        ResultSummary = $"Импорт остановлен. Сохранено компонентов: {created}. Ошибок: {failed}.";
    }
    finally
    {
      if (version == _operationVersion)
      {
        _operationCts = null;
        IsImporting = false;
      }
    }
  }

  [RelayCommand(CanExecute = nameof(IsBusy))]
  private void CancelOperation()
  {
    BusyText = "Отмена операции…";
    _operationCts?.Cancel();
  }

  [RelayCommand]
  private async Task GoBackAsync() => await _navigation.GoBackAsync();

  partial void OnIsImportingChanged(bool value) => RefreshBusyState();
  partial void OnIsLoadingChanged(bool value) => RefreshBusyState();
  private void RefreshBusyState()
  {
    OnPropertyChanged(nameof(IsBusy));
    OnPropertyChanged(nameof(BusyTitle));
    CancelOperationCommand.NotifyCanExecuteChanged();
    ImportSelectedCommand.NotifyCanExecuteChanged();
  }
}
