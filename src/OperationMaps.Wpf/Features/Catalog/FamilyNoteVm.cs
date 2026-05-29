using CommunityToolkit.Mvvm.ComponentModel;
using OperationMaps.Domain.Entities.Forms;

namespace OperationMaps.Wpf.Features.Catalog;

/// <summary>
/// Одна строка в секции «Примечания семейства».
/// Хранит выбранный Note и выбранный FormParameter.
/// </summary>
public partial class FamilyNoteVm : ObservableObject
{
  public int NoteId { get; set; }
  public int FamilyId { get; set; }

  [ObservableProperty]
  private string _noteText = "";

  [ObservableProperty]
  private int _selectedParameterId;

  [ObservableProperty]
  private string _selectedParameterName = "";
}
