using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
public sealed partial class Form4ViewModel : ScreenViewModelBase, INavigatedTo, INavigatedFrom
{
  private readonly ProjectStore _store;
  private readonly IDbContextFactory<CatalogDbContext> _dbFactory;
  private readonly IWordService _wordService;
  private readonly WordFormMapLoader _mapLoader;
  private readonly OperationMaps.Wpf.Shell.ShellViewModel _shell;
  private CancellationTokenSource? _buildCts;
  private CancellationTokenSource? _reliefCts;
  private int _buildVersion;
  private long _publishedGeneration = -1;

  [ObservableProperty] private IReadOnlyList<Form4Group> _groups = [];
  [ObservableProperty] private bool _isLoading;
  [ObservableProperty] private bool _isExporting;
  [ObservableProperty] private bool _isApplyingRelief;
  [ObservableProperty] private string _reliefStatus = "";
  [ObservableProperty] private Form4Group? _selectedGroup;
  [ObservableProperty] private IReadOnlyList<ParameterRowVm> _parameters = [];

  public OperatingConditionsViewModel Conditions { get; }

  public Form4ViewModel(
      ProjectStore store,
      IDbContextFactory<CatalogDbContext> dbFactory,
      IWordService wordService,
      WordFormMapLoader mapLoader,
      IOperatingConditionsService conditionsService,
      OperationMaps.Wpf.Shell.ShellViewModel shell)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
    _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
    _shell = shell ?? throw new ArgumentNullException(nameof(shell));
    Conditions = new OperatingConditionsViewModel(store, conditionsService);
  }

  // ── Navigation ────────────────────────────────────────────────────────────

  public async Task OnNavigatedToAsync(
      object? parameter = null,
      CancellationToken cancellationToken = default)
  {
    if (!_store.HasProject)
    {
      await OnNavigatedFromAsync();
      Groups = []; Parameters = []; SelectedGroup = null;
      return;
    }

    await BuildGroupsAsync(cancellationToken);
  }

  public Task OnNavigatedFromAsync(CancellationToken cancellationToken = default)
  {
    ++_buildVersion;
    _buildCts?.Cancel();
    _reliefCts?.Cancel();
    IsLoading = false;
    return Task.CompletedTask;
  }

  // ── Commands ──────────────────────────────────────────────────────────────

  [RelayCommand(CanExecute = nameof(CanApplyRelief))]
  private async Task ApplyReliefToAllAsync(CancellationToken ct)
  {
    if (!CanApplyRelief) return;
    using var operation = CancellationTokenSource.CreateLinkedTokenSource(ct);
    _reliefCts = operation;
    ct = operation.Token;
    var generation = _store.Generation;
    var components = _store.Components.Where(c => !c.IsBoard).ToArray();
    var position = SelectedGroup?.SourceComponents.FirstOrDefault()?.Entry.Imported.Positions.FirstOrDefault();
    IsApplyingRelief = true;
    ReliefStatus = "";
    try
    {
      await using var db = await _dbFactory.CreateDbContextAsync(ct);
      var applied = 0;
      var conflicts = new List<string>();
      foreach (var component in components)
      {
        ct.ThrowIfCancellationRequested();
        await component.LoadNtdValuesAsync(db, ct);
        ct.ThrowIfCancellationRequested();
        if (_store.Generation != generation) return;
        try { if (component.ApplyAllReliefNotes()) applied++; }
        catch (InvalidOperationException) { conflicts.Add($"{component.Name} ({component.Positions})"); }
      }
      await BuildGroupsAsync(ct);
      ct.ThrowIfCancellationRequested();
      if (_store.Generation != generation) return;
      var selection = Groups.FirstOrDefault(g => g.SourceComponents.Any(c => c.Entry.Imported.Positions.Contains(position ?? "")));
      if (selection is not null) SelectGroup(selection);
      ReliefStatus = applied > 0 ? $"Облегчённый режим включён у компонентов: {applied}."
        : "Нет компонентов с доступным примечанием облегчённого режима.";
      if (conflicts.Count > 0)
        ReliefStatus += " Не изменены из-за разных значений облегчённого режима: " + string.Join("; ", conflicts) + ". Выберите нужное примечание вручную.";
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    catch (Exception) when (ct.IsCancellationRequested || generation != _store.Generation) { }
    finally
    {
      if (ReferenceEquals(_reliefCts, operation)) _reliefCts = null;
      IsApplyingRelief = false;
    }
  }

  private bool CanApplyRelief => _store.HasProject && _publishedGeneration == _store.Generation
      && Groups.Count > 0 && !IsLoading && !IsExporting && !IsApplyingRelief;
  partial void OnIsLoadingChanged(bool value)
  {
    ApplyReliefToAllCommand.NotifyCanExecuteChanged();
    ExportWordCommand.NotifyCanExecuteChanged();
  }
  partial void OnIsApplyingReliefChanged(bool value)
  {
    ApplyReliefToAllCommand.NotifyCanExecuteChanged();
    ExportWordCommand.NotifyCanExecuteChanged();
  }

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
      // "Лист" in the frame continues the set's numbering, as in the contents.
      data.FirstSheetNumber = await _shell.FirstSheetOfAsync("4", ct);
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

  private bool CanExport => _publishedGeneration == _store.Generation
      && Groups.Count > 0 && !IsLoading && !IsExporting && !IsApplyingRelief;

  // Notify CanExecute when these properties change
  partial void OnGroupsChanged(IReadOnlyList<Form4Group> value)
  {
    ExportWordCommand.NotifyCanExecuteChanged();
    ApplyReliefToAllCommand.NotifyCanExecuteChanged();
  }

  partial void OnIsExportingChanged(bool value)
  {
    ExportWordCommand.NotifyCanExecuteChanged();
    ApplyReliefToAllCommand.NotifyCanExecuteChanged();
  }

  // ── Word data builder ─────────────────────────────────────────────────────

  /// <summary>
  /// Converts the current <see cref="Groups"/> into a <see cref="WordFormData"/>
  /// ready for the export service.
  /// </summary>
  public WordFormData BuildWordFormData()
  {
    if (IsLoading || _publishedGeneration != _store.Generation)
      throw new InvalidOperationException("Форма 4 ещё не загружена для текущего проекта. Откройте форму повторно.");
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
    var rowOffset = group.NtdValues.Any(p => p.RowNumber == 1 && p.Name == "Показатель ресурса") ? 2 : 0;
    var allNotes = group.NtdValues.SelectMany(p => p.Notes).OrderBy(n => n.Order).ToList();
    var regimeRows = group.NtdValues.Where(p => p.RegimeCondition.Length > 0).ToList();
    var conditions = regimeRows.Select(p => p.RegimeCondition).Distinct().ToList();
    var ntdValues = group.NtdValues.ToDictionary(p => p.RowNumber + rowOffset, p =>
    {
      var markers = p.Notes.OrderBy(n => n.Order).Select(n => n.Marker).ToList();
      if (p.RegimeCondition.Length > 0) markers.Add(new string('*', allNotes.Count + conditions.IndexOf(p.RegimeCondition) + 1));
      return markers.Count == 0 ? p.Value : $"{p.Value} {string.Join(", ", markers)}";
    });

    var noteLines = group.NtdValues
        .SelectMany(p => p.Notes)
        .OrderBy(n => n.Order)
        .Select(n => $"{n.Marker} {n.NoteText.Trim()}")
        .ToList();
    noteLines.AddRange(conditions.Select((text, index) => $"{new string('*', allNotes.Count + index + 1)} {text}"));

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
    _buildCts?.Cancel();
    using var operation = CancellationTokenSource.CreateLinkedTokenSource(ct);
    _buildCts = operation;
    var version = ++_buildVersion;
    var generation = _store.Generation;
    var components = _store.Components.ToArray();
    var sources = components.Select(c => (Component: c, Entry: c.Entry, Regime: c.ActiveRegime)).ToArray();
    IsLoading = true;
    _publishedGeneration = -1;
    Groups = [];
    Parameters = [];
    SelectedGroup = null;
    try { await BuildGroupsCoreAsync(operation.Token, version, generation, sources); }
    catch (OperationCanceledException) when (operation.IsCancellationRequested) { }
    catch (Exception) when (operation.IsCancellationRequested || version != _buildVersion || generation != _store.Generation) { }
    finally
    {
      if (version == _buildVersion) IsLoading = false;
      if (ReferenceEquals(_buildCts, operation)) _buildCts = null;
    }
  }

  private async Task BuildGroupsCoreAsync(CancellationToken ct, int version, long generation,
      (ProjectComponentVm Component, OperationMaps.Application.Importing.ComponentMatchEntry Entry,
        OperationMaps.Application.Models.ComponentRegimeSelection? Regime)[] sources)
  {
    await using var db = await _dbFactory.CreateDbContextAsync(ct);
    var form4 = await db.Forms
        .Include(f => f.Parameters)
        .FirstOrDefaultAsync(f => f.Number == "4", ct);

    if (form4 is null)
    {
      ct.ThrowIfCancellationRequested();
      // Empty catalogs still support planning an empty Form 4 (e.g. a board-only project).
      if (version == _buildVersion && generation == _store.Generation)
        _publishedGeneration = generation;
      return;
    }

    var parameters = form4.Parameters
        .OrderBy(p => p.RowNumber)
        .Select(p => new ParameterRowVm(p))
        .ToList();

    var groups = new List<Form4Group>();

    // RLC components — group by family name
    var rlcComponents = sources.Select(s => s.Component)
        .Where(c => !Form3DataBuilder.IsBoard(c.Entry.Imported) && c.HasFamily && c.FamilyName is not null)
        .GroupBy(c => (FamilyName: c.FamilyName!, TypeName: c.TypeName, Regime: c.ActiveRegime));

    foreach (var group in rlcComponents)
    {
      var components = group.ToList();
      var first = components.First();
      await first.LoadNtdValuesAsync(db, ct);

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

      groups.Add(form4Group);
    }

    // Non-RLC components — group by full name
    var otherComponents = sources.Select(s => s.Component)
        .Where(c => !Form3DataBuilder.IsBoard(c.Entry.Imported) && !c.HasFamily)
        .GroupBy(c => (c.Name, c.ActiveRegime));

    foreach (var group in otherComponents)
    {
      var components = group.ToList();
      await components[0].LoadNtdValuesAsync(db, ct);

      var otherGroup = new Form4Group
      {
        DisplayName = group.Key.Name,
        ComponentTypeName = components.First().ComponentTypeName,
        NtdValues = components[0].NtdValues,
        SourceComponents = components,
      };

      groups.Add(otherGroup);
    }

    ct.ThrowIfCancellationRequested();
    if (version != _buildVersion || generation != _store.Generation
        || !sources.Select(s => s.Component).SequenceEqual(_store.Components)
        || sources.Any(s => !ReferenceEquals(s.Entry, s.Component.Entry) || !Equals(s.Regime, s.Component.ActiveRegime))) return;
    Parameters = parameters;
    foreach (var group in groups)
    {
      foreach (var row in group.NtdValues) row.RecalculateGroupOrders = group.RecalculateNoteOrders;
      group.RecalculateNoteOrders();
    }
    _publishedGeneration = generation;
    Groups = groups;
  }
}
