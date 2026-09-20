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
public sealed partial class WordImportViewModel : ScreenViewModelBase, INavigatedTo
{
  private readonly IWordCatalogImportService _importService;
  private readonly CatalogDbContext _db;
  private readonly INavigationService _navigation;

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
      CatalogDbContext db,
      INavigationService navigation)
  {
    _importService = importService ?? throw new ArgumentNullException(nameof(importService));
    _db = db ?? throw new ArgumentNullException(nameof(db));
    _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
  }

  // ── INavigatedTo ─────────────────────────────────────────────────────────

  public async Task OnNavigatedToAsync(object? parameter = null, CancellationToken cancellationToken = default)
  {
    if (parameter is not string path) return;

    DocumentPath = path;
    await InitializeAsync(cancellationToken);
  }

  private async Task InitializeAsync(CancellationToken ct)
  {
    IsLoading = true;
    ErrorMessage = null;
    try
    {
      var types = await _db.ComponentTypes.OrderBy(t => t.Name).ToListAsync(ct);
      ComponentTypeOptions.Clear();
      foreach (var t in types)
        ComponentTypeOptions.Add(new ComponentTypeOption(t.Id, t.Name));

      var result = await _importService.ScanAsync(DocumentPath!, ct);
      UnsupportedFormNumbers = result.UnsupportedFormNumbers;

      Rows.Clear();
      foreach (var component in result.Components)
      {
        var row = new WordImportRowVm(component, _importService);
        row.PropertyChanged += (_, e) =>
        {
          if (e.PropertyName is nameof(WordImportRowVm.IsSelected) or nameof(WordImportRowVm.IsImported))
            ImportSelectedCommand.NotifyCanExecuteChanged();
        };
        Rows.Add(row);
      }

      ImportSelectedCommand.NotifyCanExecuteChanged();
      SelectedRow = Rows.FirstOrDefault();
    }
    catch (Exception ex)
    {
      ErrorMessage = $"Не удалось прочитать документ: {ex.Message}";
    }
    finally
    {
      IsLoading = false;
    }
  }

  // ── Detail panel ─────────────────────────────────────────────────────────

  partial void OnSelectedRowChanged(WordImportRowVm? value) => _ = LoadDetailAsync(value);

  private async Task LoadDetailAsync(WordImportRowVm? row)
  {
    SelectedParameters.Clear();
    SelectedForm4Summary.Clear();
    OnPropertyChanged(nameof(HasForm4Summary));
    if (row is null) return;

    var ownForm = await _db.Forms
        .Include(f => f.Parameters)
        .FirstOrDefaultAsync(f => f.Id == row.Model.OwnFormId);

    if (ownForm is null) return;
    if (row != SelectedRow) return; // selection moved on while this was loading

    // Pin numbers are currently unique to Form 64.
    SelectedShowsPins = ownForm.Number == "64";

    foreach (var param in ownForm.Parameters.Where(p => !p.IsOptional).OrderBy(p => p.RowNumber))
      SelectedParameters.Add(new WordImportParameterRowVm(param, row.Model, SelectedShowsPins));

    if (row.Model.Form4NtdValuesByRow.Count > 0)
    {
      var form4 = await _db.Forms
          .Include(f => f.Parameters)
          .FirstOrDefaultAsync(f => f.Number == "4");

      if (form4 is not null && row == SelectedRow)
      {
        var paramsByRow = form4.Parameters.ToDictionary(p => p.RowNumber);
        foreach (var (rowNumber, value) in row.Model.Form4NtdValuesByRow.OrderBy(kv => kv.Key))
        {
          if (!paramsByRow.TryGetValue(rowNumber, out var param)) continue;
          var name = param.Unit is { Length: > 0 } ? $"{param.Name}, {param.Unit}" : param.Name;
          SelectedForm4Summary.Add(new Form4SummaryRowVm(name, value));
        }

        OnPropertyChanged(nameof(HasForm4Summary));
      }
    }
  }

  // ── Commands ─────────────────────────────────────────────────────────────

  public bool CanImportSelected => !IsImporting
      && !IsLoading
      && Rows.Any(r => r.IsSelected && !r.IsImported);

  [RelayCommand(CanExecute = nameof(CanImportSelected))]
  private async Task ImportSelectedAsync(CancellationToken ct = default)
  {
    IsImporting = true;
    ResultSummary = null;
    try
    {
      int created = 0, failed = 0;

      foreach (var row in Rows.Where(r => r.IsSelected && !r.IsImported).ToList())
      {
        try
        {
          await _importService.ImportAsync(row.Model, ct);
          row.MarkImported();
          created++;
        }
        catch (Exception ex)
        {
          row.MarkFailed(ex.Message);
          failed++;
        }
      }

      ResultSummary = failed == 0
          ? $"Импортировано компонентов: {created}."
          : $"Импортировано: {created}. Не удалось: {failed} — см. ошибки в списке.";
    }
    finally
    {
      IsImporting = false;
      ImportSelectedCommand.NotifyCanExecuteChanged();
    }
  }

  [RelayCommand]
  private async Task GoBackAsync() => await _navigation.GoBackAsync();

  partial void OnIsImportingChanged(bool value) => ImportSelectedCommand.NotifyCanExecuteChanged();
  partial void OnIsLoadingChanged(bool value) => ImportSelectedCommand.NotifyCanExecuteChanged();
}
