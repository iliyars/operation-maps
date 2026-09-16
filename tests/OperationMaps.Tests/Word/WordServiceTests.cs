using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Word;
using Xunit;

namespace OperationMaps.Tests.Word;

public class WordServiceTests
{
  private static string ResourcesDir =>
      Path.Combine(AppContext.BaseDirectory, "Word", "Resources");

  [Fact]
  public async Task Export_then_import_round_trips_form4_data()
  {
    var mapLoader = new WordFormMapLoader(ResourcesDir);
    var service = new WordService(mapLoader);
    var templatePath = mapLoader.GetTemplatePath("4");

    // Form4's map.json defines componentsPerPage=4 and parameter rows 1-16
    // (old { row, col } format => NTD-only, no scheme/pins columns).
    var components = Enumerable.Range(0, 4).Select(slot => new WordComponentData
    {
      Name = $"Компонент-{slot}",
      ComponentTypeName = $"Тип-{slot}",
      Quantity = (slot + 1).ToString(),
      Note = $"Примечание-{slot}",
      NtdValues = Enumerable.Range(1, 16)
          .ToDictionary(row => row, row => $"v{slot}.{row}"),
    }).ToList();

    var data = new WordFormData
    {
      FormNumber = "4",
      Components = components,
    };

    var exported = await service.ExportAsync(data, templatePath);

    var tempPath = Path.Combine(Path.GetTempPath(), $"form4-roundtrip-{Guid.NewGuid()}.docx");
    await File.WriteAllBytesAsync(tempPath, exported);
    try
    {
      var imported = await service.ImportAsync("4", tempPath);

      Assert.Equal("4", imported.FormNumber);
      Assert.Equal(4, imported.Components.Count);

      for (int slot = 0; slot < 4; slot++)
      {
        var component = imported.Components[slot];
        Assert.Equal($"Компонент-{slot}", component.Name);
        Assert.Equal($"Тип-{slot}", component.ComponentTypeName);
        Assert.Equal((slot + 1).ToString(), component.Quantity);
        Assert.Equal($"Примечание-{slot}", component.Note);

        for (int row = 1; row <= 16; row++)
          Assert.Equal($"v{slot}.{row}", component.NtdValues[row]);
      }
    }
    finally
    {
      File.Delete(tempPath);
    }
  }

  [Fact]
  public async Task ImportAsync_throws_when_document_missing()
  {
    var mapLoader = new WordFormMapLoader(ResourcesDir);
    var service = new WordService(mapLoader);

    await Assert.ThrowsAsync<FileNotFoundException>(
        () => service.ImportAsync("4", Path.Combine(Path.GetTempPath(), "no-such-" + Guid.NewGuid() + ".docx")));
  }
}
