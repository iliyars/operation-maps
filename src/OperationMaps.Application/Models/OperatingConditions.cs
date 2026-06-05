namespace OperationMaps.Application.Models;

/// <summary>
/// Условия эксплуатации изделия — колонка "В аппаратуре" Формы 4.
/// Значения одинаковы для всех компонентов одного проекта.
/// Сохраняются в отдельный JSON-файл и могут переиспользоваться между проектами.
/// </summary>
public sealed class OperatingConditions
{
  // ── Показатели надёжности (rows 1–2 в Форме 4 относятся к НТД, строки 3–5) ──

  /// <summary>Показатель ресурса, ч (row 3)</summary>
  public string? Resource { get; set; }

  /// <summary>Показатель срока службы, лет (row 4)</summary>
  public string? ServiceLife { get; set; }

  /// <summary>Показатель сохраняемости, лет (row 5)</summary>
  public string? StorageLife { get; set; }

  // ── Акустический шум ─────────────────────────────────────────────────────

  /// <summary>Акустический шум: диапазон частот, Гц (row 6)</summary>
  public string? AcousticNoiseFrequency { get; set; }

  /// <summary>Акустический шум: уровень звукового давления, дБ (row 7)</summary>
  public string? AcousticNoisePressure { get; set; }

  // ── Механические воздействия ─────────────────────────────────────────────

  /// <summary>Линейное ускорение, М.С.Е-2.(G) (row 8)</summary>
  public string? LinearAcceleration { get; set; }

  // ── Давление ─────────────────────────────────────────────────────────────

  /// <summary>Давление окружающей среды: пониженное, мм рт.ст. (row 9)</summary>
  public string? PressureLow { get; set; }

  /// <summary>Давление окружающей среды: повышенное, атм (row 10)</summary>
  public string? PressureHigh { get; set; }

  // ── Температура ──────────────────────────────────────────────────────────

  /// <summary>Предельная (рабочая) температура: пониженная, °С (row 11)</summary>
  public string? TemperatureLow { get; set; }

  /// <summary>Предельная (рабочая) температура: повышенная, °С (row 12)</summary>
  public string? TemperatureHigh { get; set; }

  // ── Влажность ────────────────────────────────────────────────────────────

  /// <summary>Относительная влажность, % (row 13)</summary>
  public string? Humidity { get; set; }

  /// <summary>Относительная влажность: температура, °С (row 14)</summary>
  public string? HumidityTemperature { get; set; }
}
