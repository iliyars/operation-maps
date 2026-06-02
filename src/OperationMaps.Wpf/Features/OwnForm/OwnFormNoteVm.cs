using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OperationMaps.Wpf.Features.OwnForm
{
  /// <summary>
  /// A user note attached to a specific parameter within a form column.
  /// </summary>
  public sealed partial class OwnFormNoteVm : ObservableObject
  {
    public int? Id { get; set; }
    public int FormParameterId { get; init; }

    [ObservableProperty] private string _noteText = "";
    [ObservableProperty] private int _order;

    public string Marker => new('*', Order);

    partial void OnOrderChanged(int value)
        => OnPropertyChanged(nameof(Marker));

    public event Action<OwnFormNoteVm>? DeleteRequested;

    [RelayCommand]
    private void Delete() => DeleteRequested?.Invoke(this);
  }
}
