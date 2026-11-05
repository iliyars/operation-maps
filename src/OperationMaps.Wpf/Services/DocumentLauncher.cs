using System.Diagnostics;
using System.IO;

namespace OperationMaps.Wpf.Services;

public interface IDocumentLauncher
{
  void Open(string path);
}

public sealed class DocumentLauncher : IDocumentLauncher
{
  public void Open(string path)
  {
    var fullPath = Path.GetFullPath(path);
    if (!File.Exists(fullPath)) throw new FileNotFoundException("Документ не найден.", fullPath);
    // Use the registered .docx application (normally Word); do not depend on an Office installation path.
    using var process = Process.Start(new ProcessStartInfo { FileName = fullPath, UseShellExecute = true });
  }
}
