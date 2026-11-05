using OperationMaps.Application.Services;
using OperationMaps.Application.Word;

namespace OperationMaps.Infrastructure.Word
{
  /// <summary>
  /// Unified Word service: fills templates (export) and reads existing
  /// documents (import) using the same <c>map.json</c> coordinates.
  /// Delegates the actual work to <see cref="WordFormWriter"/> and
  /// <see cref="WordFormReader"/>.
  /// </summary>
  public sealed class WordService : IWordService
  {
    private readonly WordFormMapLoader _mapLoader;

    public WordService(WordFormMapLoader mapLoader)
    {
      _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
    }

    public Task<byte[]> ExportAsync(
        WordFormData data,
        string templatePath,
        CancellationToken ct = default)
    {
      return Task.Run(() =>
      {
        ct.ThrowIfCancellationRequested();
        var map = _mapLoader.Load(data.FormNumber);
        var bytes = WordFormWriter.Fill(data, map, templatePath);
        ct.ThrowIfCancellationRequested();
        return bytes;
      }, ct);
    }

    public Task<WordFormData> ImportAsync(
        string formNumber,
        string documentPath,
        CancellationToken ct = default)
    {
      return Task.Run(() =>
      {
        ct.ThrowIfCancellationRequested();
        if (!File.Exists(documentPath))
          throw new FileNotFoundException($"Word document not found: {documentPath}");
        var map = _mapLoader.Load(formNumber);
        var data = WordFormReader.Read(formNumber, documentPath, map);
        ct.ThrowIfCancellationRequested();
        return data;
      }, ct);
    }
  }
}
