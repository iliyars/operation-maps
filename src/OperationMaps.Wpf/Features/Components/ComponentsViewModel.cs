using Microsoft.EntityFrameworkCore;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Features.Components.AddComponent;
using OperationMaps.Wpf.Features.Components.Commands;
using OperationMaps.Wpf.Infrastructure.Commands;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Stores;
using OperationMaps.Wpf.Services;

namespace OperationMaps.Wpf.Features.Components
{
  public sealed partial class ComponentsViewModel : ScreenViewModelBase
  {
    private readonly ProjectStore _store;
    private readonly IDbContextFactory<CatalogDbContext> _dbFactory;
    private readonly IComponentEntryService _entryService;
    private readonly OperationMaps.Application.Importing.IComponentNameParser _nameParser;
    private readonly UserPreferences _preferences;
    internal UserPreferences WorkspacePreferences => _preferences;

    public ComponentsViewModel(
        ProjectStore store,
        IDbContextFactory<CatalogDbContext> dbFactory,
        IComponentEntryService entryService,
        OperationMaps.Application.Importing.IComponentNameParser nameParser,
        UserPreferences preferences)
    {
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
      _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
      _nameParser = nameParser ?? throw new ArgumentNullException(nameof(nameParser));
      _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
      _activeFilter = Enum.IsDefined(preferences.ComponentsFilter) ? preferences.ComponentsFilter : ComponentFilter.All;

      _store.Components.CollectionChanged += (_, _) => RefreshCounts();
    }

    public ObservableCollection<ProjectComponentVm> Components => _store.Components;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredComponents))]
    private ComponentFilter _activeFilter = ComponentFilter.All;

    partial void OnActiveFilterChanged(ComponentFilter value)
    {
      _preferences.ComponentsFilter = value;
      _preferences.Save();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredComponents))]
    private string _searchText = string.Empty;

    public IEnumerable<ProjectComponentVm> FilteredComponents => Components
        .Where(c => ActiveFilter switch
        {
          ComponentFilter.Matched => c.IsMatched,
          ComponentFilter.Unresolved => !c.IsMatched,
          _ => true,
        })
        .Where(c => string.IsNullOrWhiteSpace(SearchText)
                 || c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                 || c.Positions.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    // ── Counts (for filter tabs) ──────────────────────────────────────────────

    public int CountAll => Components.Count;

    // Must mirror FilteredComponents' predicates above exactly (IsMatched /
    // !IsMatched) — these used to count by MatchStatus instead (treating
    // "family found, component missing" as matched), so the tab label
    // showed a different count than the number of rows the same click
    // actually filtered down to.
    public int CountMatched => Components.Count(c => c.IsMatched);
    public int CountUnresolved => Components.Count(c => !c.IsMatched);

    // ── Selection ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    private ProjectComponentVm? _selectedComponent;

    partial void OnSelectedComponentChanged(ProjectComponentVm? value)
    {
      if (value is not null)
        _ = LoadSelectedComponentAsync(value);
    }

    private async Task LoadSelectedComponentAsync(ProjectComponentVm component)
    {
      await using var db = await _dbFactory.CreateDbContextAsync(default);
      await component.LoadNtdValuesAsync(db);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SetFilter(ComponentFilter filter)
    {
      ActiveFilter = filter;
    }

    /// <summary>
    /// Opens the two-step "add component" wizard for an unresolved row.
    /// Any user can do this — the resulting Component is marked
    /// NeedsAdminReview = true but is usable immediately (matched, exported).
    /// </summary>
    [RelayCommand]
    private void AddComponentData(ProjectComponentVm? component)
    {
      if (component is null) return;

      var dialogVm = new AddComponentDialogViewModel(
          _dbFactory, _entryService, _nameParser, AddComponentTarget.FromProject(component));
      var dialog = new AddComponentDialog(dialogVm)
      {
        Owner = System.Windows.Application.Current.MainWindow,
      };

      var result = dialog.ShowDialog();

      if (result == true && dialogVm.CreatedMatch is not null)
      {
        // Row moves from "Не найден" to "Найден" right away.
        component.ApplyNewMatch(dialogVm.CreatedMatch);
        RefreshCounts();
      }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void RefreshCounts()
    {
      OnPropertyChanged(nameof(CountAll));
      OnPropertyChanged(nameof(CountMatched));
      OnPropertyChanged(nameof(CountUnresolved));
      OnPropertyChanged(nameof(FilteredComponents));
    }
  }

  public enum ComponentFilter { All, Matched, Unresolved }
}
