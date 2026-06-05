using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Wpf.Features.Components;

namespace OperationMaps.Wpf.Features.Form4;

public sealed partial class Form4Group : ObservableObject
{
  public string DisplayName { get; init; } = "";
  public string Positions { get; init; } = "";
  public string Designation { get; init; } = "";
  public IReadOnlyList<NtdParameterVm> NtdValues { get; init; } = [];
  public IReadOnlyList<ProjectComponentVm> SourceComponents { get; init; } = [];

  /// <summary>
  /// Тип компонента (обозначение) для записи в Word.
  /// Берётся из TypeName первого компонента группы.
  /// </summary>
  public string ComponentTypeName { get; init; } = "";

  public int PositionCount => SourceComponents
    .Sum(c => c.Entry.Imported.Positions.Count);

  public bool HasNtdValues => NtdValues.Count > 0;

  public int PositionCount => SourceComponents
    .Sum(c => c.Entry.Imported.Positions.Count);

  [ObservableProperty] private bool _isSelected;

  /// <summary>
  /// Recalculates Order for all notes across all parameters sequentially.
  /// First note in the group = *, second = **, etc. regardless of which parameter.
  /// Call this after any note is added or deleted.
  /// </summary>
  public void RecalculateNoteOrders()
  {
    int order = 1;
    foreach (var param in NtdValues)
      foreach (var note in param.Notes)
        note.Order = order++;
  }

}
