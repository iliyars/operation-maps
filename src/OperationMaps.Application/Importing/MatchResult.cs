using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;

namespace OperationMaps.Application.Importing;

public sealed class MatchResult
{
  public required bool IsMatched { get; init; }
  public ComponentType? MatchedType { get; init; }
  public Family? MatchedFamily { get; init; }
  public Component? MatchedComponent { get; init; }

  public IReadOnlyList<Form> RequiredForms { get; init; }
  public string? Warning { get; init; }
}
