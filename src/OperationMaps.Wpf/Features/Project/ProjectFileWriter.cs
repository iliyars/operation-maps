using OperationMaps.Wpf.Services;

namespace OperationMaps.Wpf.Features.Project;

public interface IProjectFileWriter
{
  Task WriteAsync(string path, byte[] contents, CancellationToken ct);
}

public sealed class ProjectFileWriter : IProjectFileWriter
{
  public Task WriteAsync(string path, byte[] contents, CancellationToken ct)
      => AtomicFileWriter.WriteAsync(path, contents, ct);
}
