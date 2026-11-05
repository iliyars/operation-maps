using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OperationMaps.Application.Models;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Wpf.Features.Components;

public sealed record RegimeChoice(int Id, string Name, ComponentRegimeSelection? Selection);
public sealed record ParameterFormSection(string Title, IReadOnlyList<RegimeValueEditor> Parameters);
public sealed partial class RegimeValueEditor : ObservableObject
{
  public required int ParameterId { get; init; }
  public required string FormNumber { get; init; }
  public required int RowNumber { get; init; }
  public required string Label { get; init; }
  public string DefaultValue { get; init; } = "";
  [ObservableProperty] private string _value = "";
  public string EffectiveValue => string.IsNullOrWhiteSpace(Value) ? DefaultValue : Value;
  partial void OnValueChanged(string value) => OnPropertyChanged(nameof(EffectiveValue));
  public string Pins { get; init; } = "";
  public bool HasPins => !string.IsNullOrWhiteSpace(Pins);
  public IReadOnlyList<string> Notes { get; init; } = [];
  public IReadOnlyList<CatalogNoteChoice> Conditions { get; set; } = [];
}

public sealed partial class ComponentRegimesViewModel(IDbContextFactory<CatalogDbContext> factory) : ObservableObject
{
  private ProjectComponentVm? _component;
  private int _editingId;
  private int _generation;
  private bool _loading;
  public ObservableCollection<RegimeChoice> Choices { get; } = [];
  public ObservableCollection<CatalogNoteChoice> ConditionChoices { get; } = [];
  public ObservableCollection<RegimeValueEditor> Values { get; } = [];
  public ObservableCollection<ParameterFormSection> FormSections { get; } = [];
  public ObservableCollection<RegimeValueEditor> OwnFormNotes { get; } = [];
  [ObservableProperty] private RegimeChoice? _selected;
  [ObservableProperty] private string _name = "";
  [ObservableProperty] private string _condition = "";
  [ObservableProperty] private string _error = "";
  [ObservableProperty] private bool _isAvailable;
  [ObservableProperty] private bool _isBusy;
  [ObservableProperty] private bool _canEdit = true;
  public bool IsCatalogPreview { get; init; }
  public string ResetLabel => IsCatalogPreview ? "Показать основной режим" : "Вернуться к основному режиму";
  public string SaveLabel => IsCatalogPreview ? "Сохранить режим" : "Сохранить и применить режим";
  partial void OnCanEditChanged(bool value)
  {
    SaveCommand.NotifyCanExecuteChanged();
    NewRegimeCommand.NotifyCanExecuteChanged();
  }

  private CancellationTokenSource? _loadCts;
  public void CancelLoad()
  {
    ++_generation;
    _loadCts?.Cancel();
    _loading = false;
  }
  public async Task LoadAsync(ProjectComponentVm? component, CancellationToken cancellationToken = default)
  {
    _loadCts?.Cancel();
    using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    _loadCts = operation;
    var ct = operation.Token;
    var generation = ++_generation;
    _component = component;
    IsAvailable = false;
    Choices.Clear(); ConditionChoices.Clear(); Values.Clear(); FormSections.Clear(); OwnFormNotes.Clear(); Error = "";
    if (component is null || component.IsBoard || component.Entry.MatchResult.MatchedComponent is not { } catalogComponent)
    { _loadCts = null; return; }
    try
    {
      await using var db = await factory.CreateDbContextAsync(ct);
      var regimes = await db.ComponentRegimes.Include(r => r.Values).ThenInclude(v => v.FormParameter).ThenInclude(p => p.Form)
        .Where(r => r.ComponentId == catalogComponent.Id).OrderBy(r => r.Name).ToListAsync(ct);
      var parameters = await db.FormParameters.Include(p => p.Form)
        .Where(p => p.Form.Number == "4" || p.FormId == catalogComponent.OwnFormId).OrderBy(p => p.Form.Number).ThenBy(p => p.RowNumber).ToListAsync(ct);
      var familyValues = await db.FamilyNtdValues.Where(v => v.FamilyId == catalogComponent.FamilyId).ToDictionaryAsync(v => v.FormParameterId, v => v.Value, ct);
      var ownValues = await db.ComponentNtdValues.Where(v => v.ComponentId == catalogComponent.Id).ToDictionaryAsync(v => v.FormParameterId, v => v.Value, ct);
      foreach (var ordinary in regimes.Where(r => !ReliefNote.IsMatch(r.Condition)))
        foreach (var value in ordinary.Values)
          if (value.FormParameter.Form.Number == "4") familyValues.TryAdd(value.FormParameterId, value.Value);
          else ownValues.TryAdd(value.FormParameterId, value.Value);
      var pins = await db.ComponentPinValues.Where(v => v.ComponentId == catalogComponent.Id).ToDictionaryAsync(v => v.FormParameterId, v => v.Pins, ct);
      var familyNotes = await db.FamilyNotes.Where(n => n.FamilyId == catalogComponent.FamilyId)
        .Select(n => new { n.FormParameterId, n.Note.Text }).ToListAsync(ct);
      var componentNotes = await db.ComponentNotes.Where(n => n.ComponentId == catalogComponent.Id)
        .Select(n => new { n.FormParameterId, n.Note.Text }).ToListAsync(ct);
      var notes = familyNotes.Concat(componentNotes).Concat(regimes.Where(r => !ReliefNote.IsMatch(r.Condition))
        .SelectMany(r => r.Values.Select(v => new { v.FormParameterId, Text = r.Condition }))).GroupBy(n => n.FormParameterId)
        .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(n => n.Text).Distinct().ToArray());
      ct.ThrowIfCancellationRequested();
      if (generation != _generation) return;
      _loading = true;
      Choices.Add(new RegimeChoice(0, "Основной", null));
      foreach (var regime in regimes)
        Choices.Add(new(regime.Id, regime.Name, new(regime.Name, regime.Condition,
          regime.Values.Select(v => new RegimeParameterValue(v.FormParameter.Form.Number, v.FormParameter.RowNumber, v.Value)).ToArray())));
      var snapshots = component.ActiveRegime is { } active
        ? (active.Conditions.Count > 0 ? active.Conditions : new[] { active }) : [];
      foreach (var snapshot in snapshots)
        if (!Choices.Any(c => c.Selection?.Name == snapshot.Name && c.Selection?.Condition == snapshot.Condition))
          Choices.Add(new(-1, snapshot.Name, snapshot));
      foreach (var choice in Choices.Where(c => c.Selection is not null))
      {
        var selection = snapshots.FirstOrDefault(s => s.Name == choice.Selection!.Name && s.Condition == choice.Selection.Condition) ?? choice.Selection!;
        var item = new CatalogNoteChoice(selection.Condition, () => SelectedConditions().Any(s => s.Name == selection.Name && s.Condition == selection.Condition), enabled =>
        {
          var selected = SelectedConditions().Where(s => s.Name != selection.Name || s.Condition != selection.Condition).ToList();
          if (enabled) selected.Add(selection);
          try
          {
            var combined = ComponentRegimeSelection.Combine(selected);
            component.SelectRegime(combined);
            Populate(combined);
            Error = "";
          }
          catch (InvalidOperationException ex) { Error = ex.Message; }
          foreach (var option in ConditionChoices) option.Refresh();
        });
        ConditionChoices.Add(item);
      }
      foreach (var parameter in parameters)
        Values.Add(new RegimeValueEditor { ParameterId = parameter.Id, FormNumber = parameter.Form.Number, RowNumber = parameter.RowNumber,
          Label = $"{parameter.Name}{(string.IsNullOrEmpty(parameter.Unit) ? "" : $", {parameter.Unit}")}",
          Pins = pins.GetValueOrDefault(parameter.Id, ""),
          Notes = notes.GetValueOrDefault(parameter.Id, []),
          DefaultValue = ownValues.GetValueOrDefault(parameter.Id, familyValues.GetValueOrDefault(parameter.Id, "—")) });
      foreach (var row in Values.Where(v => v.FormNumber != "4" && v.Notes.Count > 0)) OwnFormNotes.Add(row);
      foreach (var row in Values)
        row.Conditions = ConditionChoices.Zip(Choices.Where(c => c.Selection is not null))
          .Where(pair => pair.Second.Selection!.Values.Any(v => v.FormNumber == row.FormNumber && v.RowNumber == row.RowNumber))
          .Where(pair => ReliefNote.IsMatch(pair.Second.Selection!.Condition))
          .Select(pair => pair.First).ToArray();
      foreach (var group in Values.GroupBy(v => v.FormNumber).OrderBy(g => g.Key == "4" ? 0 : 1))
        FormSections.Add(new($"Форма {group.Key}", group.OrderBy(v => v.RowNumber).ToArray()));
      Selected = Choices.FirstOrDefault(c => c.Name == component.ActiveRegime?.Name) ?? Choices[0];
      Populate(component.ActiveRegime);
      IsAvailable = true;
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    catch (Exception ex) { if (generation == _generation) Error = $"Не удалось загрузить режимы: {ex.Message}"; }
    finally
    {
      if (generation == _generation) _loading = false;
      if (ReferenceEquals(_loadCts, operation)) _loadCts = null;
    }
  }

  private IReadOnlyList<ComponentRegimeSelection> SelectedConditions() => _component?.ActiveRegime is { } active
    ? active.Conditions.Count > 0 ? active.Conditions : new[] { active } : [];

  partial void OnSelectedChanged(RegimeChoice? value)
  {
    if (_loading || value is null) return;
    _component?.SelectRegime(value.Selection);
    Populate(value.Selection);
    foreach (var choice in ConditionChoices) choice.Refresh();
  }

  private void Populate(ComponentRegimeSelection? selection)
  {
    _editingId = Selected?.Id ?? 0;
    Name = selection?.Name ?? ""; Condition = selection?.Condition ?? "";
    foreach (var row in Values)
      row.Value = selection?.Values.FirstOrDefault(v => v.FormNumber == row.FormNumber && v.RowNumber == row.RowNumber)?.Value ?? "";
  }

  [RelayCommand(CanExecute = nameof(CanEdit))] private void NewRegime() { Populate(null); _editingId = 0; }
  [RelayCommand] private void Reset() { Selected = Choices.FirstOrDefault(); _component?.SelectRegime(null); }

  [RelayCommand(CanExecute = nameof(CanEdit))]
  private async Task SaveAsync()
  {
    if (!CanEdit || IsBusy || _component?.Entry.MatchResult.MatchedComponent is not { } component) return;
    Error = "";
    if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Condition) || !Values.Any(v => !string.IsNullOrWhiteSpace(v.Value)))
    { Error = "Укажите название, условие и хотя бы одно значение режима."; return; }
    if (Name.Trim() == "Основной") { Error = "Название «Основной» зарезервировано."; return; }
    if (Values.Any(v => v.Value.Contains('*'))) { Error = "Введите значения без звёздочек: условие хранится отдельно."; return; }
    IsBusy = true;
    var target = _component;
    var name = Name.Trim(); var condition = Condition.Trim(); var editingId = _editingId;
    var changed = Values.Where(v => !string.IsNullOrWhiteSpace(v.Value)).Select(v => new
      { v.ParameterId, v.FormNumber, v.RowNumber, Value = v.Value.Trim() }).ToDictionary(v => v.ParameterId);
    try
    {
      await using var db = await factory.CreateDbContextAsync();
      if (await db.ComponentRegimes.AnyAsync(r => r.ComponentId == component.Id && r.Name == name && r.Id != editingId))
      { Error = "Режим с таким названием уже существует."; return; }
      var regime = editingId == 0 ? new ComponentRegime { ComponentId = component.Id }
        : await db.ComponentRegimes.Include(r => r.Values).SingleAsync(r => r.Id == editingId && r.ComponentId == component.Id);
      regime.Name = name; regime.Condition = condition;
      if (editingId == 0) db.ComponentRegimes.Add(regime);
      foreach (var old in regime.Values.ToArray())
        if (!changed.ContainsKey(old.FormParameterId)) db.ComponentRegimeValues.Remove(old);
      foreach (var row in changed.Values)
      {
        var entry = regime.Values.FirstOrDefault(v => v.FormParameterId == row.ParameterId);
        if (entry is null) regime.Values.Add(new ComponentRegimeValue { FormParameterId = row.ParameterId, Value = row.Value.Trim() });
        else entry.Value = row.Value.Trim();
      }
      await db.SaveChangesAsync();
      var snapshot = new ComponentRegimeSelection(regime.Name, regime.Condition,
        changed.Values.Select(v => new RegimeParameterValue(v.FormNumber, v.RowNumber, v.Value.Trim())).ToArray());
      target.SelectRegime(snapshot);
      if (_component == target) await LoadAsync(target);
    }
    catch (Exception ex) { Error = $"Не удалось сохранить режим: {ex.Message}"; }
    finally { IsBusy = false; }
  }
}
