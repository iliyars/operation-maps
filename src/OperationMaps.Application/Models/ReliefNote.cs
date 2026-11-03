namespace OperationMaps.Application.Models;

public static class ReliefNote
{
  public static bool IsMatch(string text)
  {
    var normalized = text.ToLowerInvariant().Replace('ё', 'е');
    return normalized.Contains("облегчен") && normalized.Contains("режим");
  }
}
