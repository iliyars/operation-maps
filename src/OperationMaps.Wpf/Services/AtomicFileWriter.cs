using System.IO;

namespace OperationMaps.Wpf.Services;

/// <summary>Publishes complete bytes with a same-directory rename, preserving the previous file on failure.</summary>
public static class AtomicFileWriter
{
  public static async Task WriteAsync(string path, byte[] contents, CancellationToken ct = default)
  {
    ct.ThrowIfCancellationRequested();
    var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
    try
    {
      await File.WriteAllBytesAsync(temporary, contents, ct);
      ct.ThrowIfCancellationRequested();
      // The temporary file is closed and on the same volume. Never delete
      // the destination first: a failed rename must leave it intact.
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
