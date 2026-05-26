namespace OperationMaps.Application.Importing;

public interface IComponentListImporter
{
  /// <summary>Можно ли импортировать файл с таким расширением/форматом.</summary>
  bool CanImport(string filePath);

  /// <summary>Импорт из потока (поток — чтобы не зависеть от файловой системы в тестах).</summary>
  ImportResult Import(Stream stream);
}
