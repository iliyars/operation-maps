using CommunityToolkit.Mvvm.ComponentModel;

namespace OperationMaps.Wpf.Features.Catalog;

/// <summary>Left column: a component type with its families.</summary>
public sealed partial class CatalogTypeNode : ObservableObject
{
  public required string Name { get; init; }
  public required IReadOnlyList<CatalogFamilyNode> AllFamilies { get; init; }

  [ObservableProperty] private bool _isExpanded;
  [ObservableProperty] private IReadOnlyList<CatalogFamilyNode> _families = [];

  public void ApplyFilter(string search, bool onlyUnverified)
  {
    Families = AllFamilies
        .Where(f => (search.Length == 0 || f.Matches(search)) && (!onlyUnverified || f.HasUnverified))
        .ToList();
  }
}

/// <summary>
/// A family. For passive components (R, C, L) it groups many components;
/// for e.g. microcircuits the family and its single component share a name.
/// </summary>
public sealed partial class CatalogFamilyNode : ObservableObject
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required string TypeName { get; init; }
  public required IReadOnlyList<CatalogComponentItem> Components { get; init; }

  [ObservableProperty] private bool _isSelected;

  public bool HasUnverified => Components.Any(c => c.NeedsReview);

  public void NotifyReviewChanged() => OnPropertyChanged(nameof(HasUnverified));

  public bool Matches(string search)
      => Name.Contains(search, StringComparison.OrdinalIgnoreCase)
      || Components.Any(c => c.Matches(search));
}

/// <summary>Middle column: one catalog component.</summary>
public sealed partial class CatalogComponentItem : ObservableObject
{
  public required int Id { get; init; }
  public required string FullName { get; init; }
  [ObservableProperty] private bool _needsReview;
  public string? OwnFormNumber { get; init; }

  [ObservableProperty] private bool _isSelected;

  public bool Matches(string search) => FullName.Contains(search, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Right column: what is shown for the selected family or component.</summary>
public enum CatalogCardKind { Family, Component }

public sealed class CatalogCard
{
  public required CatalogCardKind Kind { get; init; }

  /// <summary>Family.Id or Component.Id; 0 when the record no longer exists.</summary>
  public int EntityId { get; init; }

  /// <summary>Own form has a "номера выводов" column (Form 64) — pins are editable.</summary>
  public bool ShowsPins { get; init; }

  public required string Title { get; init; }
  public required string Subtitle { get; init; }
  public bool NeedsReview { get; init; }

  /// <summary>Shown instead of parameters when there's nothing to show (e.g. no own form).</summary>
  public string? EmptyMessage { get; init; }

  public required string ParametersHeader { get; init; }
  public IReadOnlyList<CatalogParamRow> Parameters { get; init; } = [];
  public IReadOnlyList<CatalogNoteRow> Notes { get; init; } = [];

  public bool HasPins => Parameters.Any(p => !string.IsNullOrEmpty(p.Pins));
  public bool HasNotes => Notes.Count > 0;
}

public sealed partial class CatalogParamRow : ObservableObject
{
  public required int FormParameterId { get; init; }
  public required int RowNumber { get; init; }
  public required string Name { get; init; }

  /// <summary>Display value ("—" when nothing is stored).</summary>
  public required string Value { get; init; }

  /// <summary>Stored value, empty when none — the starting point for editing.</summary>
  public string StoredValue { get; init; } = "";

  public string? Pins { get; init; }

  [ObservableProperty] private string _editValue = "";
  [ObservableProperty] private string _editPins = "";
}

public sealed class CatalogNoteRow
{
  public required string Parameter { get; init; }
  public required string Text { get; init; }
}
