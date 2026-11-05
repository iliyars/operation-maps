using Moq;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Wpf.Features.TitlePage;
using OperationMaps.Wpf.Services;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Tests.Wpf;

public sealed class AtomicFileWriterTests : IDisposable
{
  private readonly string _folder = Path.Combine(Path.GetTempPath(), $"atomic-export-{Guid.NewGuid():N}");
  private string Output => Path.Combine(_folder, "document.docx");
  private static readonly byte[] Previous = [1, 2, 3, 4];
  private static readonly byte[] Updated = [5, 6, 7, 8, 9];

  public AtomicFileWriterTests() => Directory.CreateDirectory(_folder);

  [Fact]
  public async Task Creates_and_replaces_complete_file_without_touching_other_temporary_files()
  {
    var unrelated = Output + ".tmp";
    await File.WriteAllBytesAsync(unrelated, Previous);
    await AtomicFileWriter.WriteAsync(Output, Previous);
    Assert.Equal(Previous, await File.ReadAllBytesAsync(Output));
    await AtomicFileWriter.WriteAsync(Output, Updated);
    Assert.Equal(Updated, await File.ReadAllBytesAsync(Output));
    Assert.Equal(Previous, await File.ReadAllBytesAsync(unrelated));
    Assert.Equal([unrelated], Directory.GetFiles(_folder, "*.tmp"));
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task Cancellation_preserves_previous_result_and_does_not_leave_a_temporary_file(bool exists)
  {
    if (exists) await File.WriteAllBytesAsync(Output, Previous);
    using var cancelled = new CancellationTokenSource();
    cancelled.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AtomicFileWriter.WriteAsync(Output, Updated, cancelled.Token));
    if (exists) Assert.Equal(Previous, await File.ReadAllBytesAsync(Output));
    else Assert.False(File.Exists(Output));
    Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
  }

  [Fact]
  public async Task Failed_replacement_of_open_document_keeps_previous_bytes_and_cleans_temporary_file()
  {
    await File.WriteAllBytesAsync(Output, Previous);
    using (File.Open(Output, FileMode.Open, FileAccess.Read, FileShare.Read))
    {
      var error = await Record.ExceptionAsync(() => AtomicFileWriter.WriteAsync(Output, Updated));
      Assert.True(error is IOException or UnauthorizedAccessException, error?.ToString() ?? "Replacement unexpectedly succeeded.");
    }
    Assert.Equal(Previous, await File.ReadAllBytesAsync(Output));
    Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    await AtomicFileWriter.WriteAsync(Output, Updated);
    Assert.Equal(Updated, await File.ReadAllBytesAsync(Output));
  }

  [Fact]
  public async Task Competing_writers_leave_one_complete_result_and_clean_their_own_temporary_files()
  {
    var payloads = Enumerable.Range(1, 4).Select(i => Enumerable.Repeat((byte)i, 512 * 1024).ToArray()).ToArray();
    var succeeded = await Task.WhenAll(payloads.Select(async bytes =>
    {
      try { await AtomicFileWriter.WriteAsync(Output, bytes); return true; }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }));
    Assert.Contains(true, succeeded);
    var actual = await File.ReadAllBytesAsync(Output);
    Assert.Contains(payloads, bytes => bytes.SequenceEqual(actual));
    Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
  }

  [Theory]
  [InlineData("title")]
  [InlineData("contents")]
  [InlineData("registration")]
  public async Task Auxiliary_word_exports_preserve_open_document_and_allow_retry(string kind)
  {
    var store = new ProjectStore();
    store.Load("Плата", _folder, new ProjectMatchResult { Matched = [], Unresolved = [], Warnings = [] });
    var plan = ContentsPlanner.Plan([]);
    Func<Task<string>> export;
    string filename;
    if (kind == "title")
    {
      var service = new Mock<ITitlePageService>();
      service.Setup(s => s.BuildAsync(It.IsAny<TitlePageData>(), It.IsAny<CancellationToken>())).ReturnsAsync(Updated);
      export = () => TitlePageFile.WriteAsync(store, service.Object);
      filename = TitlePageFile.FileName;
    }
    else if (kind == "contents")
    {
      var service = new Mock<IContentsPageService>();
      service.Setup(s => s.BuildAsync(It.IsAny<ContentsPageData>(), It.IsAny<CancellationToken>())).ReturnsAsync(Updated);
      export = () => ContentsPageFile.WriteAsync(store, service.Object, plan);
      filename = ContentsPageFile.FileName;
    }
    else
    {
      var service = new Mock<IRegistrationSheetService>();
      service.Setup(s => s.BuildAsync(It.IsAny<RegistrationSheetData>(), It.IsAny<CancellationToken>())).ReturnsAsync(Updated);
      export = () => RegistrationSheetFile.WriteAsync(store, service.Object, plan);
      filename = RegistrationSheetFile.FileName;
    }
    Directory.CreateDirectory(store.FormsFolder!);
    var path = Path.Combine(store.FormsFolder!, filename);
    await File.WriteAllBytesAsync(path, Previous);
    using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    {
      var error = await Record.ExceptionAsync(async () => { await export(); });
      Assert.True(error is IOException or UnauthorizedAccessException, error?.ToString() ?? "Replacement unexpectedly succeeded.");
    }
    Assert.Equal(Previous, await File.ReadAllBytesAsync(path));
    Assert.Empty(Directory.GetFiles(store.FormsFolder!, "*.tmp"));
    Assert.Equal(path, await export());
    Assert.Equal(Updated, await File.ReadAllBytesAsync(path));
  }

  public void Dispose() => Directory.Delete(_folder, recursive: true);
}
