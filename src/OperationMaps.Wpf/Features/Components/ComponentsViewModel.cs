
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

namespace OperationMaps.Wpf.Features.Components
{
  public sealed partial class ComponentsViewModel : ScreenViewModelBase
  {
    private readonly ProjectStore _store;
    private readonly CatalogDbContext _db;
    private readonly IComponentEntryService _entryService;
    private readonly OperationMaps.Application.Importing.IComponentNameParser _nameParser;

    public ComponentsViewModel(
        ProjectStore store,
        CatalogDbContext db,
        IComponentEntryService entryService,
        OperationMaps.Application.Importing.IComponentNameParser nameParser)
    {
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _db = db ?? throw new ArgumentNullException(nameof(db));
      _entryService = entryService ?? throw new ArgumentNullException(nameof(entryService));
      _nameParser = nameParser ?? throw new ArgumentNullException(nameof(nameParser));

      _store.Components.CollectionChanged += (_, _) => RefreshCounts();
    }

    public ObservableCollection<ProjectComponentVm> Components => _store.Components;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredComponents))]
    private ComponentFilter _activeFilter = ComponentFilter.All;

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
    public int CountMatched => Components.Count(c => c.MatchStatus != ComponentMatchStatus.Unresolved);
    public int CountUnresolved => Components.Count(c => c.MatchStatus == ComponentMatchStatus.Unresolved);

    // ── Selection ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    private ProjectComponentVm? _selectedComponent;

    partial void OnSelectedComponentChanged(ProjectComponentVm? value)
    {
      if (value is not null)
        _ = value.LoadNtdValuesAsync(_db);
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

      var dialogVm = new AddComponentDialogViewModel(_db, _entryService, _nameParser, component);
      var dialog = new AddComponentDialog(dialogVm)
      {
        Owner = System.Windows.Application.Current.MainWindow,
      };

      var result = dialog.ShowDialog();

      if (result == true)
      {
        // ProjectComponentVm already updated itself via ApplyNewMatch();
        // just refresh aggregate counts/filters in this screen.
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
