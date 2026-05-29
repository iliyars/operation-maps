namespace OperationMaps.Application.Importing;

public interface IComponentMatcher
{
  Task<MatchResult> MatchAsync(
      ImportedComponent imported,
      CancellationToken ct = default);

  Task<ProjectMatchResult> MatchAllAsync(
    IReadOnlyList<ImportedComponent> components,
    CancellationToken ct = default);
}
