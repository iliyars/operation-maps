using CommunityToolkit.Mvvm.ComponentModel;

namespace OperationMaps.Wpf.Features.Components;

public sealed class CatalogNoteChoice(string text, Func<bool> selected, Action<bool> select) : ObservableObject
{
  public string Text { get; } = text;
  public bool IsSelected
  {
    get => selected();
    set { if (value != IsSelected) select(value); OnPropertyChanged(); }
  }
  public void Refresh() => OnPropertyChanged(nameof(IsSelected));
}
