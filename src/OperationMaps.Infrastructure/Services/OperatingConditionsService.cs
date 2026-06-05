using System.Text.Json;
using OperationMaps.Application.Models;
using OperationMaps.Application.Services;

namespace OperationMaps.Infrastructure.Services;

public sealed class OperatingConditionsService : IOperatingConditionsService
{

  private static readonly JsonSerializerOptions _json = new()
  {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  public async Task<OperatingConditions> LoadAsync(string filePath, CancellationToken ct = default)
  {
    if (!File.Exists(filePath))
      return new OperatingConditions();

    await using var stream = File.OpenRead(filePath);
    return await JsonSerializer.DeserializeAsync<OperatingConditions>(stream, _json, ct) ?? new OperatingConditions();
  }

  public async Task SaveAsync(OperatingConditions conditions, string filePath, CancellationToken ct = default)
  {
    var dir = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrEmpty(dir))
      Directory.CreateDirectory(dir);

    await using var stream = File.Create(filePath);
    await JsonSerializer.SerializeAsync(stream, conditions, _json, ct);
  }
}
