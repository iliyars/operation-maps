using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Services;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.Components.Commands;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Wpf.Features.Form4;

/// <summary>
/// ViewModel for the Form 4 table.
/// Groups project components by family (RLC) or full name (others),
/// loads NTD values from the catalog, and presents a read-only table.
/// </summary>
public sealed partial class Form4ViewModel : ScreenViewModelBase, INavigatedTo
{
  private readonly ProjectStore _store;
  private readonly CatalogDbContext _db;
  private readonly IFilePicker _filePicker;

  [ObservableProperty] private IReadOnlyList<Form4Group> _groups = [];
  [ObservableProperty] private bool _isLoading;
  [ObservableProperty] private Form4Group? _selectedGroup;
  [ObservableProperty] private IReadOnlyList<ParameterRowVm> _parameters = [];

  [RelayCommand]
  private void SelectGroup(Form4Group group)
  {
    if (SelectedGroup is not null)
      SelectedGroup.IsSelected = false;

    SelectedGroup = group;
    group.IsSelected = true;
  }

  public Form4ViewModel(
    ProjectStore store,
    CatalogDbContext db,
    IFilePicker filePicker)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    _db = db ?? throw new ArgumentNullException(nameof(db));
    _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
  }

  [ObservableProperty]
  private bool _isExporting;



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

  // ── Private ───────────────────────────────────────────────────────────────

  private async Task BuildGroupsAsync(CancellationToken ct)
  {
    // Load Form 4 parameters (row headers)
    var form4 = await _db.Forms
        .Include(f => f.Parameters)
        .FirstOrDefaultAsync(f => f.Number == "4", ct);

    if (form4 is null) return;

    Parameters = form4.Parameters
        .OrderBy(p => p.RowNumber)
        .Select(p => new ParameterRowVm(p))
        .ToList();

    // Group components
    var groups = new List<Form4Group>();

    // RLC components — group by family name
    var rlcComponents = _store.Components
        .Where(c => c.HasFamily && c.FamilyName is not null)
        .GroupBy(c => (FamilyName: c.FamilyName!, TypeName: c.TypeName));

    foreach (var group in rlcComponents)
    {
      var components = group.ToList();

      // Load NTD values for the first component's family
      var first = components.First();
      await first.LoadNtdValuesAsync(_db, ct);

      var positions = components
          .SelectMany(c => c.Entry.Imported.Positions)
          .OrderBy(p => p, PositionComparer.Instance)
          .ToList();

      var form4Group = new Form4Group
      {
        DisplayName = group.Key.FamilyName,
        Positions = string.Join(", ", positions),
        NtdValues = first.NtdValues,
        SourceComponents = components,
      };

      // Wire up cross-parameter order recalculation
      foreach (var ntdParam in form4Group.NtdValues)
        ntdParam.RecalculateGroupOrders = form4Group.RecalculateNoteOrders;

      groups.Add(form4Group);
    }

    // Non-RLC components (no family) — group by full name
    var otherComponents = _store.Components
        .Where(c => !c.HasFamily)
        .GroupBy(c => c.Name);

    foreach (var group in otherComponents)
    {
      var components = group.ToList();

      var positions = components
          .SelectMany(c => c.Entry.Imported.Positions)
          .OrderBy(p => p, PositionComparer.Instance)
          .ToList();

      var otherGroup = new Form4Group
      {
        DisplayName = group.Key,
        Positions = string.Join(", ", positions),
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
