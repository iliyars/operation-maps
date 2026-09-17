using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Wpf.Features.Components.WordImport;

/// <summary>
/// Reads a filled work-order Word document (Form4 + Form67/68/... possibly
/// spread across many pages) via <see cref="IWordCatalogImportService"/>,
/// shows every resolved own-form component for review, lets the user pick a
/// type manually for anything Form4 couldn't resolve, and creates the
/// selected ones in the catalog — mirroring the "Ввести данные компонента"
/// wizard, just driven from a document instead of manual entry.
/// </summary>
public sealed partial class WordImportDialogViewModel : ObservableObject
{
  private readonly IWordCatalogImportService _importService;
  private readonly CatalogDbContext _db;

  public string DocumentPath { get; }
  public string DocumentFileName => System.IO.Path.GetFileName(DocumentPath);

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

  public WordImportDialogViewModel(
      IWordCatalogImportService importService,
      CatalogDbContext db,
      string documentPath)
  {
    _importService = importService ?? throw new ArgumentNullException(nameof(importService));
    _db = db ?? throw new ArgumentNullException(nameof(db));
    DocumentPath = documentPath ?? throw new ArgumentNullException(nameof(documentPath));
  }

  public async Task InitializeAsync(CancellationToken ct = default)
  {
    IsLoading = true;
    ErrorMessage = null;
    try
    {
      var types = await _db.ComponentTypes.OrderBy(t => t.Name).ToListAsync(ct);
      ComponentTypeOptions.Clear();
      foreach (var t in types)
        ComponentTypeOptions.Add(new ComponentTypeOption(t.Id, t.Name));

      var result = await _importService.ScanAsync(DocumentPath, ct);
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

  partial void OnIsImportingChanged(bool value) => ImportSelectedCommand.NotifyCanExecuteChanged();
  partial void OnIsLoadingChanged(bool value) => ImportSelectedCommand.NotifyCanExecuteChanged();
}
