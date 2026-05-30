
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OperationMaps.Application.Importing;
using OperationMaps.Wpf.Features.Components.Commands;
using OperationMaps.Wpf.Infrastructure.Commands;
using OperationMaps.Wpf.Infrastructure.Navigation;
using OperationMaps.Wpf.Infrastructure.ViewModels;

namespace OperationMaps.Wpf.Features.Components
{
  public sealed partial class ComponentsViewModel : ScreenViewModelBase, INavigatedTo
  {
    public ObservableCollection<ProjectComponentVm> Components { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredComponents))]
    private ComponentFilter _activeFilter = ComponentFilter.All;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredComponents))]
    private string _searchText = string.Empty;

    public IEnumerable<ProjectComponentVm> FilteredComponents => Components
        .Where(c => _activeFilter switch
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

    public IList<ProjectComponentVm> SelectedComponents { get; } =
        new ObservableCollection<ProjectComponentVm>();

    public bool CanSplit => SelectedComponent?.Entry.Imported.Positions.Count > 1;
    public bool CanMerge => SelectedComponents.Count == 2
                         && SelectedComponents[0].IsMatched == SelectedComponents[1].IsMatched;

    // ── Undo/Redo ─────────────────────────────────────────────────────────────

    public UndoRedoStack History { get; } = new();

    public bool CanUndo => History.CanUndo;
    public bool CanRedo => History.CanRedo;
    public Task OnNavigatedToAsync(
        object? parameter = null,
        CancellationToken cancellationToken = default)
    {
      if (parameter is not ProjectMatchResult result)
        return Task.CompletedTask;

      Components.Clear();
      History.Clear();

      foreach (var entry in result.Matched.Concat(result.Unresolved))
        Components.Add(new ProjectComponentVm(entry));

      RefreshCounts();
      return Task.CompletedTask;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SetFilter(ComponentFilter filter)
    {
      ActiveFilter = filter;
    }

    [RelayCommand(CanExecute = nameof(History.CanUndo))]
    private void Undo() => History.Undo();

    [RelayCommand(CanExecute = nameof(History.CanRedo))]
    private void Redo() => History.Redo();

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
