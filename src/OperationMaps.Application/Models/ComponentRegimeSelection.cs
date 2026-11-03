namespace OperationMaps.Application.Models;

// A project keeps its own snapshot: editing the catalog cannot silently change a saved project.
public sealed record RegimeParameterValue(string FormNumber, int RowNumber, string Value)
{
  public string? Condition { get; init; }
}
public sealed record ComponentRegimeSelection(string Name, string Condition, IReadOnlyList<RegimeParameterValue> Values)
{
  public IReadOnlyList<ComponentRegimeSelection> Conditions { get; init; } = [];

  public static ComponentRegimeSelection? Combine(IReadOnlyList<ComponentRegimeSelection> conditions)
  {
    if (conditions.Count == 0) return null;
    var values = new List<RegimeParameterValue>();
    foreach (var group in conditions.SelectMany(c => c.Values.Select(v => (Value: v, c.Condition)))
      .GroupBy(x => (x.Value.FormNumber, x.Value.RowNumber)))
    {
      if (group.Select(x => x.Value.Value).Distinct().Count() > 1)
        throw new InvalidOperationException($"Выбранные условия задают разные значения: форма {group.Key.FormNumber}, параметр {group.Key.RowNumber}.");
      values.Add(group.First().Value with { Condition = string.Join("\n", group.Select(x => x.Condition).Distinct()) });
    }
    return new(string.Join("; ", conditions.Select(c => c.Name)), string.Join("\n", conditions.Select(c => c.Condition).Distinct()), values)
      { Conditions = conditions.ToArray() };
  }
}
