using OperationMaps.Application.Word;
using OperationMaps.Infrastructure.Word;
using Xunit;

namespace OperationMaps.Tests.Word;

public sealed class WordReportBuilderTests : IDisposable
{
  private readonly WordFormMapLoader _mapLoader = new(
      Path.Combine(AppContext.BaseDirectory, "Word", "Resources"));

  private readonly string _outPath = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid()}.docx");

  /// <summary>
  /// No cover template is shipped yet — the report must still contain every
  /// form, with the first form's document serving as the base.
  /// </summary>
  [Fact]
  public async Task BuildAsync_without_cover_contains_all_forms()
  {
    var builder = new WordReportBuilder(new WordService(_mapLoader));

    var bytes = await builder.BuildAsync(
        coverTemplatePath: null,
        [
          (new WordFormData { FormNumber = "4" }, _mapLoader.GetTemplatePath("4")),
          (new WordFormData { FormNumber = "67" }, _mapLoader.GetTemplatePath("67")),
          (new WordFormData { FormNumber = "68" }, _mapLoader.GetTemplatePath("68")),
        ]);

    await File.WriteAllBytesAsync(_outPath, bytes);

    Assert.Equal(["4", "67", "68"], WordFormReader.DetectFormNumbers(_outPath).Distinct().Order());
  }

  public void Dispose()
  {
    if (File.Exists(_outPath)) File.Delete(_outPath);
  }
}
