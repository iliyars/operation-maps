using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace OperationMaps.Wpf.Features.OwnForm;

public sealed partial class ColumnListItemVm : ObservableObject
{
  public FormColumnVm Column { get; }

  private readonly IReadOnlyList<FormParameterRowVm> _parameters;

  public string Name => Column.Name;
  public string Positions => Column.Positions;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(StatusColor))]
  [NotifyPropertyChangedFor(nameof(StatusLabel))]
  [NotifyPropertyChangedFor(nameof(ProgressFraction))]
  [NotifyPropertyChangedFor(nameof(ProgressText))]
  private FillStatus _fillStatus = FillStatus.Empty;

  [ObservableProperty] private bool _isSelected;

  [ObservableProperty] private bool _isMultiSelected;

  public int FilledCount { get; private set; }
  public int TotalCount { get; private set; }

  public string StatusLabel => FillStatus switch
  {
    FillStatus.Complete => "Заполнено",
    FillStatus.Partial => "Не полностью",
    _ => "Не заполнено",
  };

  public string StatusColor => FillStatus switch
  {
    FillStatus.Complete => "#34A853",
    FillStatus.Partial => "#F9A825",
    _ => "#CCCCCC",
  };

  public double ProgressFraction => TotalCount == 0 ? 0 :
        (double)FilledCount / TotalCount;

  public string ProgressText => TotalCount == 0 ? "" : $"{FilledCount}/{TotalCount}";

  public ColumnListItemVm(
        FormColumnVm column,
        IReadOnlyList<FormParameterRowVm> parameters)
  {
    Column = column;
    _parameters = parameters;
    Refresh();
  }
  // ── Refresh ───────────────────────────────────────────────────────────────

  /// <summary>Recalculate fill status — call after any SchemeValue change.</summary>
  public void Refresh()
  {
    FilledCount = Column.GetFilledCount(_parameters);
    TotalCount = Column.GetTotalCount(_parameters);
    FillStatus = Column.GetFillStatus(_parameters);

    OnPropertyChanged(nameof(FilledCount));
    OnPropertyChanged(nameof(TotalCount));
    OnPropertyChanged(nameof(ProgressText));
    OnPropertyChanged(nameof(ProgressFraction));
  }


}
