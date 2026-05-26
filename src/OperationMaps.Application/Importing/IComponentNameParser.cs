namespace OperationMaps.Application.Importing;

public interface IComponentNameParser
{
  ParsedComponentName Parse(string rawName);
}

public class ParsedComponentName
{
  public required string Raw { get; init; }
  public required string Type { get; init; }
  public required string Family { get; init; }
  public required string Name { get; init; }
}
