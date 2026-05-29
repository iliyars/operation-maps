using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OperationMaps.Application.Importing
{
  public sealed class ProjectMatchResult
  {
    public required IReadOnlyList<ComponentMatchEntry> Matched { get; init; }
    public required IReadOnlyList<ComponentMatchEntry> Unresolved { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }
  }

  public sealed class ComponentMatchEntry
  {
    public required ImportedComponent Imported { get; init; }
    public required MatchResult MatchResult { get; init; }
  }
}
