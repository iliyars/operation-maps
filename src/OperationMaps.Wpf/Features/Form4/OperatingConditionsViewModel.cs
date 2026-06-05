using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OperationMaps.Application.Models;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Wpf.Features.Form4;


//// <summary>
/// ViewModel для боковой панели условий эксплуатации изделия (Форма 4, колонка "В аппаратуре").
/// </summary>
public sealed partial class OperatingConditionsViewModel : ObservableObject
{
  private readonly ProjectStore _store;
  private readonly IOperatingConditionsService _service;

  // ── Panel state ───────────────────────────────────────────────────────────

  [ObservableProperty] private bool _isSaved = false;

  // ── Fields (bound directly to store.Conditions) ───────────────────────────

  public string? Resource
  {
    get => _store.Conditions.Resource;
    set { _store.Conditions.Resource = value; OnPropertyChanged(); }
  }
  public string? ServiceLife
  {
    get => _store.Conditions.ServiceLife;
    set { _store.Conditions.ServiceLife = value; OnPropertyChanged(); }
  }
  public string? StorageLife
  {
    get => _store.Conditions.StorageLife;
    set { _store.Conditions.StorageLife = value; OnPropertyChanged(); }
  }
  public string? AcousticNoiseFrequency
  {
    get => _store.Conditions.AcousticNoiseFrequency;
    set { _store.Conditions.AcousticNoiseFrequency = value; OnPropertyChanged(); }
  }
  public string? AcousticNoisePressure
  {
    get => _store.Conditions.AcousticNoisePressure;
    set { _store.Conditions.AcousticNoisePressure = value; OnPropertyChanged(); }
  }
  public string? LinearAcceleration
  {
    get => _store.Conditions.LinearAcceleration;
    set { _store.Conditions.LinearAcceleration = value; OnPropertyChanged(); }
  }
  public string? PressureLow
  {
    get => _store.Conditions.PressureLow;
    set { _store.Conditions.PressureLow = value; OnPropertyChanged(); }
  }
  public string? PressureHigh
  {
    get => _store.Conditions.PressureHigh;
    set { _store.Conditions.PressureHigh = value; OnPropertyChanged(); }
  }
  public string? TemperatureLow
  {
    get => _store.Conditions.TemperatureLow;
    set { _store.Conditions.TemperatureLow = value; OnPropertyChanged(); }
  }
  public string? TemperatureHigh
  {
    get => _store.Conditions.TemperatureHigh;
    set { _store.Conditions.TemperatureHigh = value; OnPropertyChanged(); }
  }
  public string? Humidity
  {
    get => _store.Conditions.Humidity;
    set { _store.Conditions.Humidity = value; OnPropertyChanged(); }
  }
  public string? HumidityTemperature
  {
    get => _store.Conditions.HumidityTemperature;
    set { _store.Conditions.HumidityTemperature = value; OnPropertyChanged(); }
  }

  public OperatingConditionsViewModel(ProjectStore store, IOperatingConditionsService service)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    _service = service ?? throw new ArgumentNullException(nameof(service));
  }

  // ── Commands ──────────────────────────────────────────────────────────────



  [RelayCommand]
  private async Task SaveAsync(CancellationToken ct)
  {
    if (_store.ConditionsFilePath is null) return;
    await _service.SaveAsync(_store.Conditions, _store.ConditionsFilePath, ct);
    IsSaved = true;
    await Task.Delay(3000, CancellationToken.None);
    IsSaved = false;
  }

  [RelayCommand]
  private void MouseLeavePanel()
  {
    IsSaved = false;
  }

  [RelayCommand]
  private async Task LoadFromFileAsync(CancellationToken ct)
  {
    var dlg = new OpenFileDialog
    {
      Title = "Загрузить условия эксплуатации",
      Filter = "Файлы условий (*.json)|*.json|Все файлы (*.*)|*.*",
    };

    if (dlg.ShowDialog() != true) return;

    var loaded = await _service.LoadAsync(dlg.FileName, ct);
    _store.Conditions = loaded;
    RefreshAllProperties();
  }

  [RelayCommand]
  private async Task LoadFromProjectAsync(CancellationToken ct)
  {
    if (_store.ConditionsFilePath is null) return;
    var loaded = await _service.LoadAsync(_store.ConditionsFilePath, ct);
    _store.Conditions = loaded;
    RefreshAllProperties();
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private void RefreshAllProperties()
  {
    OnPropertyChanged(nameof(Resource));
    OnPropertyChanged(nameof(ServiceLife));
    OnPropertyChanged(nameof(StorageLife));
    OnPropertyChanged(nameof(AcousticNoiseFrequency));
    OnPropertyChanged(nameof(AcousticNoisePressure));
    OnPropertyChanged(nameof(LinearAcceleration));
    OnPropertyChanged(nameof(PressureLow));
    OnPropertyChanged(nameof(PressureHigh));
    OnPropertyChanged(nameof(TemperatureLow));
    OnPropertyChanged(nameof(TemperatureHigh));
    OnPropertyChanged(nameof(Humidity));
    OnPropertyChanged(nameof(HumidityTemperature));
  }
}
