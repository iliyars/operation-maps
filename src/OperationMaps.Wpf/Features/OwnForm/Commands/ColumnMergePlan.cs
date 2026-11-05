using CommunityToolkit.Mvvm.ComponentModel;

namespace OperationMaps.Wpf.Features.OwnForm.Commands;

public enum MergeSide { First, Second }
public sealed record MergeOption(MergeSide Side, string Label);

public sealed partial class MergeConflict : ObservableObject
{
  public required string Key { get; init; }
  public required string Label { get; init; }
  public required string FirstValue { get; init; }
  public required string SecondValue { get; init; }
  [ObservableProperty] private MergeSide? _selectedSide;
  public IReadOnlyList<MergeOption> Options { get; } = [new(MergeSide.First, "Из первого столбца"), new(MergeSide.Second, "Из второго столбца")];
}

public sealed class ColumnMergePlan
{
  private readonly FormColumnVm _first;
  private readonly FormColumnVm _second;
  private readonly HashSet<int> _calculated;
  public List<MergeConflict> Conflicts { get; } = [];
  public bool Confirmed { get; set; }
  public string FirstPositions => _first.Positions;
  public string SecondPositions => _second.Positions;
  public bool IsResolved => Conflicts.All(c => c.SelectedSide.HasValue);

  public ColumnMergePlan(FormColumnVm first, FormColumnVm second,
    IReadOnlyDictionary<int, string>? labels = null, IEnumerable<int>? calculated = null)
  {
    _first = first;
    _second = second;
    _calculated = calculated?.ToHashSet() ?? [];
    FindConflicts("scheme", "В схеме", first.CellValues, second.CellValues);
    FindConflicts("optional", "В схеме (доп.)", first.OptionalCellValues, second.OptionalCellValues);
    if (first.LoadFactorBaseParameterId is { } firstBase && second.LoadFactorBaseParameterId is { } secondBase && firstBase != secondBase)
      Conflicts.Add(new() { Key = "base", Label = "Базовый параметр коэффициента нагрузки",
        FirstValue = Label(firstBase), SecondValue = Label(secondBase) });
    if (Different(first.NtdValues, second.NtdValues) || Different(first.OptionalNtdValues, second.OptionalNtdValues)
      || Different(first.PinValues, second.PinValues) || !SameSelection(first.Component.ActiveRegime, second.Component.ActiveRegime))
      Conflicts.Add(new() { Key = "catalog", Label = "Данные по НТД, выводы и связанное примечание — выбрать вместе",
        FirstValue = Describe(first), SecondValue = Describe(second) });

    string Label(int id) => labels?.GetValueOrDefault(id) ?? $"Параметр {id}";
    void FindConflicts(string prefix, string title, Dictionary<int, string> a, Dictionary<int, string> b)
    {
      foreach (var id in a.Keys.Intersect(b.Keys).Where(id => !_calculated.Contains(id)))
        if (Filled(a[id]) && Filled(b[id]) && a[id].Trim() != b[id].Trim())
          Conflicts.Add(new() { Key = $"{prefix}:{id}", Label = $"{title} · {Label(id)}", FirstValue = a[id], SecondValue = b[id] });
    }
    string Describe(FormColumnVm column) => string.Join("\n", column.NtdValues.Concat(column.OptionalNtdValues)
      .Select(v => $"{Label(v.Key)}: {v.Value}")) + (column.Component.ActiveRegime is { } selection ? $"\n{selection.Condition}" : "")
      + string.Join("", column.PinValues.Select(v => $"\n{Label(v.Key)} · выводы: {v.Value}"));
  }

  public FormColumnVm Build(IReadOnlyList<string> positions)
  {
    if (!IsResolved) throw new InvalidOperationException("Выберите значения для всех конфликтующих параметров перед объединением.");
    var catalogSide = Side("catalog");
    var source = catalogSide == MergeSide.Second ? _second : _first;
    var other = catalogSide == MergeSide.Second ? _first : _second;
    var merged = source.CloneWithPositions(positions);
    if (_first.HasRegimeOverride || _second.HasRegimeOverride
        || !SameSelection(_first.Component.ActiveRegime, _second.Component.ActiveRegime))
      merged.SelectRegimeOverride(source.Component.ActiveRegime);
    MergeValues("scheme", merged.CellValues, _first.CellValues, _second.CellValues);
    MergeValues("optional", merged.OptionalCellValues, _first.OptionalCellValues, _second.OptionalCellValues);
    if (!Conflicts.Any(c => c.Key == "catalog")) merged.FillMissingCatalogValuesFrom(other);
    merged.LoadFactorBaseParameterId = Side("base") == MergeSide.Second
      ? _second.LoadFactorBaseParameterId : _first.LoadFactorBaseParameterId ?? _second.LoadFactorBaseParameterId;
    merged.MergeNotesFrom(_first, _second);
    return merged;
  }

  private MergeSide Side(string key) => Conflicts.FirstOrDefault(c => c.Key == key)?.SelectedSide ?? MergeSide.First;

  private void MergeValues(string prefix, Dictionary<int, string> target, Dictionary<int, string> first, Dictionary<int, string> second)
  {
    target.Clear();
    foreach (var id in first.Keys.Union(second.Keys))
    {
      if (_calculated.Contains(id)) continue;
      var a = first.GetValueOrDefault(id, "");
      var b = second.GetValueOrDefault(id, "");
      target[id] = Side($"{prefix}:{id}") == MergeSide.Second ? b : Filled(a) ? a : b;
    }
  }

  private static bool Filled(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim() is not "-" and not "—";
  private static bool Different(Dictionary<int, string> a, Dictionary<int, string> b)
    => a.Keys.Intersect(b.Keys).Any(id => Filled(a[id]) && Filled(b[id]) && a[id].Trim() != b[id].Trim());
  private static bool SameSelection(OperationMaps.Application.Models.ComponentRegimeSelection? a, OperationMaps.Application.Models.ComponentRegimeSelection? b)
    => ReferenceEquals(a, b) || a is not null && b is not null && a.Name == b.Name && a.Condition == b.Condition
      && a.Values.OrderBy(v => v.FormNumber).ThenBy(v => v.RowNumber).SequenceEqual(b.Values.OrderBy(v => v.FormNumber).ThenBy(v => v.RowNumber));
}
