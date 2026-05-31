namespace OperationMaps.Wpf.Features.Components;

/// <summary>
/// UI-level match status for a project component.
/// More granular than the domain MatchStatus.
/// </summary>
public enum ComponentMatchStatus
{
  /// <summary>Component found in catalog (Family + Component exist).</summary>
  Matched,

  /// <summary>Family found in catalog but specific component is not registered.</summary>
  FamilyFound,

  /// <summary>Neither family nor component found in catalog.</summary>
  Unresolved,
}
