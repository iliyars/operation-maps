using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OperationMaps.Wpf.Features.Form4
{
  public sealed partial class Form4NoteVm : ObservableObject
  {
    /// <summary>DB id — null for unsaved notes.</summary>
    public int? Id { get; set; }

    public int FormParameterId { get; init; }
    public int? FamilyId { get; init; }
    public int? ComponentId { get; init; }

    [ObservableProperty]
    private string _noteText = "";

    [ObservableProperty]
    private int _order;

    public string Marker => new('*', Order);

    partial void OnOrderChanged(int value)
        => OnPropertyChanged(nameof(Marker));

    public event Action<Form4NoteVm>? DeleteRequested;

    [RelayCommand]
    private void Delete() => DeleteRequested?.Invoke(this);


  }
}
