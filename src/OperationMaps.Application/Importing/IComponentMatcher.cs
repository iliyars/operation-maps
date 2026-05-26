namespace OperationMaps.Application.Importing;

public interface IComponentMatcher
{
  Task<MatchResult> MatchAsync(ImportedComponent imported, CancellationToken ct = default);
}
