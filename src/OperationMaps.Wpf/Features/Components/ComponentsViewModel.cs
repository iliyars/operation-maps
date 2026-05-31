
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Application.Importing;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf.Features.Components.Commands;
using OperationMaps.Wpf.Infrastructure.Commands;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Wpf.Features.Components
{
  public sealed partial class ComponentsViewModel : ScreenViewModelBase
  {
    private readonly ProjectStore _store;
    private readonly CatalogDbContext _db;

    public ComponentsViewModel(ProjectStore store, CatalogDbContext db)
    {
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _db = db ?? throw new ArgumentNullException(nameof(db));

      _store.Components.CollectionChanged += (_, _) => RefreshCounts();
    }

    public ObservableCollection<ProjectComponentVm> Components => _store.Components;
    public UndoRedoStack History => _store.History;

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
    public int CountMatched => Components.Count(c => c.IsMatched);
    public int CountUnresolved => Components.Count(c => !c.IsMatched);

    // ── Selection ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSplit))]
    [NotifyPropertyChangedFor(nameof(CanMerge))]
    private ProjectComponentVm? _selectedComponent;

    partial void OnSelectedComponentChanged(ProjectComponentVm? value)
    {
      if(value is not null)
        _ = value.LoadNtdValuesAsync(_db);
    }

    public ObservableCollection<ProjectComponentVm> SelectedComponents { get; } = [];

    public bool CanSplit => SelectedComponent?.Entry.Imported.Positions.Count > 1;
    public bool CanMerge => SelectedComponents.Count == 2
                         && SelectedComponents[0].IsMatched == SelectedComponents[1].IsMatched;


    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SetFilter(ComponentFilter filter)
    {
      ActiveFilter = filter;
    }


    public bool CanUndo => _store.History.CanUndo;
    public bool CanRedo => _store.History.CanRedo;


    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
      History.Undo();
      RefreshCounts();
    }
    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
      History.Redo();
      RefreshCounts();
    }
    [RelayCommand(CanExecute = nameof(CanMerge))]
    private void Merge()
    {
      var first = SelectedComponents[0];
      var second = SelectedComponents[1];

      // Check for parameter conflicts (placeholder — full dialog in next step)
      var command = new MergeComponentsCommand(Components, first, second);
      History.Execute(command);
      RefreshCounts();
    }

    [RelayCommand(CanExecute = nameof(CanSplit))]
    private void Split()
    {
      if (SelectedComponent is null) return;

      // SplitDialog will be shown from View code-behind, which calls ExecuteSplit()
      // This command just triggers the dialog opening via an event
      SplitRequested?.Invoke(SelectedComponent);
    }

    /// <summary>
    /// Called by the View after the user confirms the split dialog.
    /// </summary>
    public void ExecuteSplit(
        ProjectComponentVm original,
        IReadOnlyList<string> leftPositions,
        IReadOnlyList<string> rightPositions)
    {
      var command = new SplitComponentCommand(
          Components, original, leftPositions, rightPositions);

      History.Execute(command);
      RefreshCounts();
    }

    // ── Events (View subscribes to show dialogs) ──────────────────────────────

    public event Action<ProjectComponentVm>? SplitRequested;

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
