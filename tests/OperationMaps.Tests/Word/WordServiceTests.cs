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

  /// <summary>
  /// Generic round-trip check for every "simple" form (no operating
  /// conditions, no dynamic optional rows): reads each form's REAL map.json
  /// to find out which parameter rows and columns actually exist, fills
  /// every one of them with a distinct value per slot, exports against the
  /// real template.docx, imports the result back, and checks every value
  /// landed in the right slot's row.
  /// <para>
  /// Deliberately does not hardcode row/column numbers transcribed from
  /// map.json — that transcription is exactly what produced the Form4
  /// noteCell bug this suite caught. Asking the loaded map which
  /// coordinates exist means a future map.json edit that misaligns a
  /// column is caught by whichever slot's value now lands in the wrong
  /// place (or goes missing), without the test needing to be updated.
  /// </para>
  /// </summary>
  [Theory]
  [InlineData("55")]
  [InlineData("64")]
  [InlineData("67")]
  [InlineData("68")]
  [InlineData("69")]
  [InlineData("83")]
  [InlineData("86")]
  public async Task Export_then_import_round_trips_simple_forms(string formNumber)
  {
    var mapLoader = new WordFormMapLoader(ResourcesDir);
    var service = new WordService(mapLoader);
    var map = mapLoader.Load(formNumber);
    var templatePath = mapLoader.GetTemplatePath(formNumber);

    var components = Enumerable.Range(0, map.ComponentsPerPage).Select(slotIndex =>
    {
      var slot = map.ComponentSlots[slotIndex];
      var ntdValues = new Dictionary<int, string>();
      var schemeValues = new Dictionary<int, string>();
      var pinValues = new Dictionary<int, string>();

      foreach (var (rowKey, coord) in slot.ParameterCells)
      {
        var row = int.Parse(rowKey);
        ntdValues[row] = $"ntd-{slotIndex}.{row}";
        if (coord.SchemeCol.HasValue)
          schemeValues[row] = $"scheme-{slotIndex}.{row}";
        if (coord.PinsCol.HasValue)
          pinValues[row] = $"pins-{slotIndex}.{row}";
      }

      return new WordComponentData
      {
        Name = $"Компонент-{slotIndex}",
        ComponentTypeName = $"Тип-{slotIndex}",
        Quantity = (slotIndex + 1).ToString(),
        Note = slot.NoteCell.HasValue ? $"Примечание-{slotIndex}" : "",
        NtdValues = ntdValues,
        SchemeValues = schemeValues,
        PinValues = pinValues,
      };
    }).ToList();

    var data = new WordFormData
    {
      FormNumber = formNumber,
      Components = components,
    };

    var exported = await service.ExportAsync(data, templatePath);

    var tempPath = Path.Combine(Path.GetTempPath(), $"form{formNumber}-roundtrip-{Guid.NewGuid()}.docx");
    await File.WriteAllBytesAsync(tempPath, exported);
    try
    {
      var imported = await service.ImportAsync(formNumber, tempPath);

      Assert.Equal(formNumber, imported.FormNumber);
      Assert.Equal(map.ComponentsPerPage, imported.Components.Count);

      for (int slotIndex = 0; slotIndex < map.ComponentsPerPage; slotIndex++)
      {
        var slot = map.ComponentSlots[slotIndex];
        var component = imported.Components[slotIndex];

        // Not every form's metaCells include componentType/quantity —
        // Form4 does, but the "own form" style forms (55/64/67/68/69) only
        // show positionsNumber + componentName. Only assert on what the
        // map actually declares a cell for.
        if (slot.MetaCells.ContainsKey(MetaCellKey.ComponentName))
          Assert.Equal($"Компонент-{slotIndex}", component.Name);
        if (slot.MetaCells.ContainsKey(MetaCellKey.ComponentType))
          Assert.Equal($"Тип-{slotIndex}", component.ComponentTypeName);
        if (slot.MetaCells.ContainsKey(MetaCellKey.Quantity))
          Assert.Equal((slotIndex + 1).ToString(), component.Quantity);
        if (slot.NoteCell.HasValue)
          Assert.Equal($"Примечание-{slotIndex}", component.Note);

        foreach (var (rowKey, coord) in slot.ParameterCells)
        {
          var row = int.Parse(rowKey);
          Assert.Equal($"ntd-{slotIndex}.{row}", component.NtdValues[row]);
          if (coord.SchemeCol.HasValue)
            Assert.Equal($"scheme-{slotIndex}.{row}", component.SchemeValues[row]);
          if (coord.PinsCol.HasValue)
            Assert.Equal($"pins-{slotIndex}.{row}", component.PinValues[row]);
        }
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
