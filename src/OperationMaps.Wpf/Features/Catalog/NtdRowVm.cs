using CommunityToolkit.Mvvm.ComponentModel;

namespace OperationMaps.Wpf.Features.Catalog
{


  /// <summary>
  /// Одна строка в таблице NTD-значений семейства.
  /// </summary>
  public partial class NtdRowVm : ObservableObject
  {
    /// <summary>Id записи FamilyNtdValue (0 = ещё не сохранена).</summary>
    public int Id { get; set; }

    public int FormParameterId { get; init; }
    public int RowNumber { get; init; }
    public string ParameterName { get; init; } = "";
    public string? Unit { get; init; }

    [ObservableProperty]
    private string _value = "";

    /// <summary>Есть несохранённые изменения.</summary>
    public bool IsDirty { get; private set; }

    partial void OnValueChanged(string value) => IsDirty = true;

    public void MarkClean() => IsDirty = false;
  }

}
