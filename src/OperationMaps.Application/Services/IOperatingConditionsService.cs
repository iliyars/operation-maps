using OperationMaps.Application.Models;

namespace OperationMaps.Application.Services;

public interface IOperatingConditionsService
{
  /// <summary>Загружает условия из JSON-файла. Возвращает пустой объект если файл не найден.</summary>
  Task<OperatingConditions> LoadAsync(string filePath, CancellationToken ct = default);

  /// <summary>Сохраняет условия в JSON-файл.</summary>
  Task SaveAsync(OperatingConditions conditions, string filePath, CancellationToken ct = default);
}
