using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Word;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.Components.Commands;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Wpf.Features.Form4;

// <summary>
/// ViewModel for the Form 4 table.
/// Groups project components by family (RLC) or full name (others),
/// loads NTD values from the catalog, and presents a read-only table.
/// Supports export to Word via <see cref="ExportWordCommand"/>.
/// </summary>
public sealed partial class Form4ViewModel : ScreenViewModelBase, INavigatedTo
{
  private readonly ProjectStore _store;
  private readonly CatalogDbContext _db;
  private readonly IWordService _wordService;
  private readonly WordFormMapLoader _mapLoader;

  [ObservableProperty] private IReadOnlyList<Form4Group> _groups = [];
  [ObservableProperty] private bool _isLoading;
  [ObservableProperty] private bool _isExporting;
  [ObservableProperty] private Form4Group? _selectedGroup;
  [ObservableProperty] private IReadOnlyList<ParameterRowVm> _parameters = [];

  public OperatingConditionsViewModel Conditions { get; }

  public Form4ViewModel(
      ProjectStore store,
      CatalogDbContext db,
      IWordService wordService,
      WordFormMapLoader mapLoader,
      IOperatingConditionsService conditionsService)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    _db = db ?? throw new ArgumentNullException(nameof(db));
    _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
    _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
    Conditions = new OperatingConditionsViewModel(store, conditionsService);
  }

  // ── Navigation ────────────────────────────────────────────────────────────

  public async Task OnNavigatedToAsync(
      object? parameter = null,
      CancellationToken cancellationToken = default)
  {
    if (!_store.HasProject) return;

    IsLoading = true;
    try
    {
      await BuildGroupsAsync(cancellationToken);
    }
    finally
    {
      IsLoading = false;
    }
  }

  // ── Commands ──────────────────────────────────────────────────────────────

  [RelayCommand]
  private void SelectGroup(Form4Group group)
  {
    if (SelectedGroup is not null)
      SelectedGroup.IsSelected = false;

    SelectedGroup = group;
    group.IsSelected = true;
  }

  [RelayCommand(CanExecute = nameof(CanExport))]
  private async Task ExportWordAsync(CancellationToken ct = default)
  {
    var formsFolder = _store.FormsFolder;
    if (formsFolder is null) return;

    Directory.CreateDirectory(formsFolder);

    var outputPath = _store.GetFormDocumentPath("4")!;
    var templatePath = _mapLoader.GetTemplatePath("4");

    IsExporting = true;
    try
    {
      var data = BuildWordFormData();
      var bytes = await _wordService.ExportAsync(data, templatePath, ct);

      // Write to a temp file first, then replace — avoids IOException
      // if the output file is currently open in Word.
      var tmpPath = outputPath + ".tmp";
      await File.WriteAllBytesAsync(tmpPath, bytes, ct);

      if (File.Exists(outputPath))
        File.Delete(outputPath);

      File.Move(tmpPath, outputPath);
    }
    finally
    {
      IsExporting = false;
    }
  }

  private bool CanExport => Groups.Count > 0 && !IsExporting;

  // Notify CanExecute when these properties change
  partial void OnGroupsChanged(IReadOnlyList<Form4Group> value)
      => ExportWordCommand.NotifyCanExecuteChanged();

  partial void OnIsExportingChanged(bool value)
      => ExportWordCommand.NotifyCanExecuteChanged();

  // ── Word data builder ─────────────────────────────────────────────────────

  /// <summary>
  /// Converts the current <see cref="Groups"/> into a <see cref="WordFormData"/>
  /// ready for the export service.
  /// </summary>
  public WordFormData BuildWordFormData()
  {
    var components = Groups.Select(BuildComponentData).ToList();

    return new WordFormData
    {
      FormNumber = "4",
      DocumentDesignation = _store.DocumentNumber ?? _store.ProjectName ?? "",
      Components = components,
      OperatingConditions = _store.Conditions,
      HeaderFields = new Dictionary<string, string>
      {
        ["sheetNumber"] = "1",
        // totalSheets is calculated automatically by WordService
      },
    };
  }

  private static WordComponentData BuildComponentData(Form4Group group)
  {
    var ntdValues = group.NtdValues
        .ToDictionary(p => p.RowNumber, p => p.Value);

    var noteLines = group.NtdValues
        .SelectMany(p => p.Notes)
        .OrderBy(n => n.Order)
        .Select(n => $"{n.Marker} {n.NoteText.Trim()}")
        .ToList();

    return new WordComponentData
    {
      Name = group.DisplayName,
      ComponentTypeName = group.ComponentTypeName,
      Quantity = group.PositionCount.ToString(),
      NtdValues = ntdValues,
      Note = string.Join("\n", noteLines),
    };
  }

  // ── Groups builder ────────────────────────────────────────────────────────

  private async Task BuildGroupsAsync(CancellationToken ct)
  {
    var form4 = await _db.Forms
        .Include(f => f.Parameters)
        .FirstOrDefaultAsync(f => f.Number == "4", ct);

    if (form4 is null) return;

    Parameters = form4.Parameters
        .OrderBy(p => p.RowNumber)
        .Select(p => new ParameterRowVm(p))
        .ToList();

    var groups = new List<Form4Group>();

    // RLC components — group by family name
    var rlcComponents = _store.Components
        .Where(c => c.HasFamily && c.FamilyName is not null)
        .GroupBy(c => (FamilyName: c.FamilyName!, TypeName: c.TypeName));

    foreach (var group in rlcComponents)
    {
      var components = group.ToList();
      var first = components.First();
      await first.LoadNtdValuesAsync(_db, ct);

      var positions = components
          .SelectMany(c => c.Entry.Imported.Positions)
          .OrderBy(p => p, PositionComparer.Instance)
          .ToList();

      var form4Group = new Form4Group
      {
        DisplayName = group.Key.FamilyName,
        ComponentTypeName = first.ComponentTypeName,
        NtdValues = first.NtdValues,
        SourceComponents = components,
      };

      foreach (var ntdParam in form4Group.NtdValues)
        ntdParam.RecalculateGroupOrders = form4Group.RecalculateNoteOrders;

      groups.Add(form4Group);
    }

    // Non-RLC components — group by full name
    var otherComponents = _store.Components
        .Where(c => !c.HasFamily)
        .GroupBy(c => c.Name);

    foreach (var group in otherComponents)
    {
      var components = group.ToList();

      var otherGroup = new Form4Group
      {
        DisplayName = group.Key,
        ComponentTypeName = components.First().ComponentTypeName,
        NtdValues = [],
        SourceComponents = components,
      };

      foreach (var ntdParam in otherGroup.NtdValues)
        ntdParam.RecalculateGroupOrders = otherGroup.RecalculateNoteOrders;

      groups.Add(otherGroup);
    }

    Groups = groups;
  }
}
