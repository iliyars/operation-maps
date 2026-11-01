using System.IO;

namespace OperationMaps.Wpf.Features.Project;

public interface IProjectFileWriter
{
  Task WriteAsync(string path, byte[] contents, CancellationToken ct);
}

public sealed class ProjectFileWriter : IProjectFileWriter
{
  public async Task WriteAsync(string path, byte[] contents, CancellationToken ct)
  {
    var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
    try
    {
      await File.WriteAllBytesAsync(temporary, contents, ct);
      ct.ThrowIfCancellationRequested();
      File.Move(temporary, path, overwrite: true);
    }
    finally
    {
      try { File.Delete(temporary); }
      catch (IOException) { }
      catch (UnauthorizedAccessException) { }
    }
  }
}
