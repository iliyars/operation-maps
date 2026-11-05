using Microsoft.Extensions.DependencyInjection;
using OperationMaps.Application.Importing;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf;
using OperationMaps.Wpf.Features.OwnForm;
using OperationMaps.Wpf.Stores;

namespace OperationMaps.Tests.Wpf;

public sealed class FormEditingWorkflowTests : IDisposable
{
  [Theory]
  [InlineData("C1|C2")]
  [InlineData("C1|C2,C2,C3")]
  [InlineData("C1|C2,X1")]
  [InlineData("C1,C2,C3")]
  [InlineData("C1,C2,C3|")]
  public void Invalid_split_does_not_change_columns_or_history(string distribution)
  {
    var original = _state.Columns[0];
    original.Component.SetPositions(["C1", "C2", "C3"]);
    original.SetCellValue(1, "5");
    original.AddNote(1, "Сохранить");
    var before = _state.Columns.ToArray();
    var groups = distribution.Split('|').Select(g => (IReadOnlyList<string>)g.Split(',', StringSplitOptions.RemoveEmptyEntries)).ToArray();
    var command = new OperationMaps.Wpf.Features.OwnForm.Commands.MultiSplitColumnCommand(_state.Columns, original, groups);
    Assert.Throws<InvalidOperationException>(() => _state.History.Execute(command));
    Assert.Equal(before, _state.Columns);
    Assert.Equal("5", original.GetCellValue(1));
    Assert.Equal("Сохранить", Assert.Single(original.GetNotes(1)).NoteText);
    Assert.False(_state.History.CanUndo);
  }

  [Fact]
  public void Merge_rejects_overlapping_positions_without_mutating_columns()
  {
    var first = _state.Columns[0];
    var second = first.CloneWithPositions(first.Component.Entry.Imported.Positions);
    _state.Columns[1] = second;
    var command = new OperationMaps.Wpf.Features.OwnForm.Commands.MergeColumnsCommand(_state.Columns, first, second);
    Assert.Throws<InvalidOperationException>(() => _state.History.Execute(command));
    Assert.Same(first, _state.Columns[0]);
    Assert.Same(second, _state.Columns[1]);
    Assert.False(_state.History.CanUndo);
  }

  [Fact]
  public void Catalog_conflict_choice_does_not_import_missing_fields_from_rejected_side()
  {
    var first = _state.Columns[0];
    var second = first.CloneWithPositions(["C2"]);
    second.NtdValues[1] = "20";
    second.NtdValues[3] = "99";
    second.OptionalNtdValues[2] = "15";
    second.PinValues[3] = "7, 8";
    var plan = new OperationMaps.Wpf.Features.OwnForm.Commands.ColumnMergePlan(first, second);
    Assert.Single(plan.Conflicts).SelectedSide = OperationMaps.Wpf.Features.OwnForm.Commands.MergeSide.First;
    var merged = plan.Build(["C1", "C2"]);
    Assert.Equal("10", merged.GetNtdValue(1));
    Assert.False(merged.NtdValues.ContainsKey(3));
    Assert.False(merged.OptionalNtdValues.ContainsKey(2));
    Assert.Empty(merged.PinValues);
  }

  [Fact]
  public async Task Formula_details_follow_state_without_reentering_calculation_or_dirtying_on_selection()
  {
    _state.SetParameters([
      _state.Parameters[0],
      new(new FormParameter { Id = 4, RowNumber = 4, Name = "Сумма", Formula = "row1+row1" }, _state.Columns),
    ]);
    await _vm.OnNavigatedToAsync(67);
    _vm.SelectColumnCommand.Execute(_state.Columns[0]);
    _vm.ParameterDetails.Single(p => p.FormParameterId == 1).SchemeValue = "3";
    Assert.Equal("6", _vm.ParameterDetails.Single(p => p.FormParameterId == 4).SchemeValue);
    Assert.Equal("6", _state.Columns[0].GetCellValue(4));
    var edits = 0;
    _state.Edited += () => edits++;
    _vm.SelectColumnCommand.Execute(_state.Columns[1]);
    _vm.SelectColumnCommand.Execute(_state.Columns[0]);
    Assert.Equal(0, edits);
    Assert.Equal("6", _vm.ParameterDetails.Single(p => p.FormParameterId == 4).SchemeValue);
  }

  private readonly ServiceProvider _provider;
  private readonly OwnFormViewModel _vm;
  private readonly OwnFormState _state;

  public FormEditingWorkflowTests()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
    services.AddPresentation();
    services.AddSingleton(OperationMaps.Wpf.Services.UserPreferences.CreateInMemory());
    _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    var store = _provider.GetRequiredService<ProjectStore>();
    var entries = Enumerable.Range(1, 2).Select(index => new ComponentMatchEntry
    {
      Imported = new ImportedComponent
      {
        ImportIndex = index, RawName = $"Компонент {index}", DetectedCategory = "Конденсатор", Positions = [$"C{index}"],
      },
      MatchResult = new MatchResult
      {
        IsMatched = true,
        MatchedComponent = new Component { Id = index, FullName = $"Компонент {index}", OwnFormId = 67 },
      },
    }).ToList();
    store.Load("Плата", Path.GetTempPath(), new ProjectMatchResult { Matched = entries, Unresolved = [], Warnings = [] });
    _state = new OwnFormState(67, "67", "Конденсаторы", [], null, null);
    _state.SetParameters([
      new FormParameterRowVm(new FormParameter { Id = 1, RowNumber = 1, Name = "Напряжение", IsRequired = true }, _state.Columns),
      new FormParameterRowVm(new FormParameter { Id = 2, RowNumber = 2, Name = "Второе напряжение", IsOptional = true, IsRequired = true, OptionalForRowNumber = 1 }, _state.Columns),
      new FormParameterRowVm(new FormParameter { Id = 3, RowNumber = 3, Name = "Не применяется" }, _state.Columns),
    ]);
    _state.Parameters[0].OptionalCounterpartId = 2;
    foreach (var component in store.Components)
    {
      var column = new FormColumnVm(component);
      column.NtdValues[1] = "10";
      _state.Columns.Add(column);
    }
    store.AddOwnForm(_state);
    _vm = _provider.GetRequiredService<OwnFormViewModel>();
  }

  [Fact]
  public void Merge_resolves_optional_catalog_and_base_parameter_conflicts_and_sorts_columns()
  {
    var first = _state.Columns[0];
    first.Component.SetPositions(["C10"]);
    var second = first.CloneWithPositions(["C2"]);
    var other = _state.Columns[1];
    other.Component.SetPositions(["C3"]);
    _state.Columns.Insert(1, second);
    first.NtdValues[1] = "10";
    second.NtdValues[1] = "20";
    first.OptionalCellValues[2] = "4";
    second.OptionalCellValues[2] = "8";
    first.LoadFactorBaseParameterId = 1;
    second.LoadFactorBaseParameterId = 3;
    var plan = new OperationMaps.Wpf.Features.OwnForm.Commands.ColumnMergePlan(first, second);
    Assert.Equal(3, plan.Conflicts.Count);
    foreach (var conflict in plan.Conflicts) conflict.SelectedSide = OperationMaps.Wpf.Features.OwnForm.Commands.MergeSide.Second;
    var command = new OperationMaps.Wpf.Features.OwnForm.Commands.MergeColumnsCommand(_state.Columns, first, second, plan);
    command.Execute();
    Assert.Equal(["C2, C10", "C3"], _state.Columns.Select(c => c.Positions));
    var merged = _state.Columns[0];
    Assert.Equal("20", merged.GetNtdValue(1));
    Assert.Equal("8", merged.GetOptionalCellValue(2));
    Assert.Equal(3, merged.LoadFactorBaseParameterId);
    command.Undo();
    Assert.Same(first, _state.Columns[0]);
    Assert.Same(second, _state.Columns[1]);
    Assert.Same(other, _state.Columns[2]);
  }

  [Fact]
  public async Task Merge_keeps_filled_values_and_unions_notes_without_duplicates()
  {
    var first = _state.Columns[0];
    var second = first.CloneWithPositions(["C2"]);
    first.SetCellValue(1, "—");
    second.SetCellValue(1, "5");
    second.SetCellValue(3, "7");
    second.OptionalNtdValues[2] = "15";
    second.SetOptionalCellValue(2, "9");
    first.AddNote(1, "Общее примечание");
    second.AddNote(1, "Общее примечание");
    second.AddNote(1, "Второе примечание");
    first.AddCatalogNote(1, "Снятое примечание");
    first.GetAvailableNotes(1)[0].IsSelected = false;
    _state.Columns[1] = second;
    await _vm.OnNavigatedToAsync(67);
    _vm.ToggleColumnSelectionCommand.Execute(first);
    _vm.ToggleColumnSelectionCommand.Execute(second);
    _vm.MergeCommand.Execute(null);
    var merged = Assert.Single(_state.Columns);
    Assert.Equal("5", merged.GetCellValue(1));
    Assert.Equal("7", merged.GetCellValue(3));
    Assert.Equal("9", merged.GetOptionalCellValue(2));
    Assert.Equal("15", merged.OptionalNtdValues[2]);
    Assert.Equal(["Общее примечание", "Второе примечание"], merged.GetNotes(1).Select(n => n.NoteText));
    Assert.False(Assert.Single(merged.GetAvailableNotes(1)).IsSelected);
    _vm.UndoCommand.Execute(null);
    Assert.Equal("—", first.GetCellValue(1));
    Assert.Equal("5", second.GetCellValue(1));
    _vm.RedoCommand.Execute(null);
    Assert.Equal("5", Assert.Single(_state.Columns).GetCellValue(1));
  }

  [Fact]
  public async Task Merge_conflicts_require_confirmation_and_keep_originals_on_cancel()
  {
    var first = _state.Columns[0];
    var second = first.CloneWithPositions(["C2"]);
    first.SetCellValue(1, "5");
    second.SetCellValue(1, "7");
    _state.Columns[1] = second;
    await _vm.OnNavigatedToAsync(67);
    _vm.ToggleColumnSelectionCommand.Execute(first);
    _vm.ToggleColumnSelectionCommand.Execute(second);
    OperationMaps.Wpf.Features.OwnForm.Commands.ColumnMergePlan? requested = null;
    _vm.MergeConflictsRequested += plan => requested = plan;
    _vm.MergeCommand.Execute(null);
    Assert.NotNull(requested);
    Assert.Single(requested.Conflicts);
    Assert.Equal(2, _state.Columns.Count);
    Assert.False(_vm.CanUndo);
    Assert.Same(first, _state.Columns[0]);
    Assert.Same(second, _state.Columns[1]);
    _vm.MergeConflictsRequested += plan =>
    {
      foreach (var conflict in plan.Conflicts) conflict.SelectedSide = OperationMaps.Wpf.Features.OwnForm.Commands.MergeSide.Second;
      plan.Confirmed = true;
    };
    _vm.MergeCommand.Execute(null);
    Assert.Equal("7", Assert.Single(_state.Columns).GetCellValue(1));
    _vm.UndoCommand.Execute(null);
    Assert.Equal("5", _state.Columns[0].GetCellValue(1));
    Assert.Equal("7", _state.Columns[1].GetCellValue(1));
  }

  [Fact]
  public void Merge_command_rejects_unresolved_conflicts_before_mutating_collection()
  {
    var first = _state.Columns[0];
    var second = first.CloneWithPositions(["C2"]);
    first.SetCellValue(1, "5");
    second.SetCellValue(1, "7");
    _state.Columns[1] = second;
    var command = new OperationMaps.Wpf.Features.OwnForm.Commands.MergeColumnsCommand(_state.Columns, first, second);
    Assert.Throws<InvalidOperationException>(() => command.Execute());
    Assert.Equal(2, _state.Columns.Count);
    Assert.Same(first, _state.Columns[0]);
    Assert.Same(second, _state.Columns[1]);
  }

  [Fact]
  public async Task Merge_rejects_different_names_in_ui_and_commands_without_changing_columns()
  {
    await _vm.OnNavigatedToAsync(67);
    var first = _state.Columns[0];
    var second = _state.Columns[1];
    _vm.ToggleColumnSelectionCommand.Execute(first);
    _vm.ToggleColumnSelectionCommand.Execute(second);
    Assert.Same(first, Assert.Single(_vm.SelectedColumns));
    Assert.True(_vm.ColumnItems.Single(i => i.Column == first).IsMultiSelected);
    Assert.False(_vm.ColumnItems.Single(i => i.Column == second).IsMultiSelected);
    Assert.False(_vm.CanMerge);
    Assert.False(_vm.MergeCommand.CanExecute(null));
    _vm.MergeCommand.Execute(null);
    Assert.Equal(2, _state.Columns.Count);
    var merge = new OperationMaps.Wpf.Features.OwnForm.Commands.MergeColumnsCommand(_state.Columns, first, second);
    Assert.Throws<InvalidOperationException>(() => merge.Execute());
    Assert.Same(first, _state.Columns[0]);
    Assert.Same(second, _state.Columns[1]);
    _vm.ToggleColumnSelectionCommand.Execute(first);
    Assert.Empty(_vm.SelectedColumns);
    _vm.ToggleColumnSelectionCommand.Execute(second);
    Assert.Same(second, Assert.Single(_vm.SelectedColumns));
  }

  [Fact]
  public async Task Merge_accepts_identical_names_and_supports_undo_and_redo()
  {
    var first = _state.Columns[0];
    _state.Columns[1] = first.CloneWithPositions(["C2"]);
    var second = _state.Columns[1];
    await _vm.OnNavigatedToAsync(67);
    _vm.ToggleColumnSelectionCommand.Execute(first);
    _vm.ToggleColumnSelectionCommand.Execute(second);
    Assert.True(_vm.CanMerge);
    Assert.True(_vm.MergeCommand.CanExecute(null));
    _vm.MergeCommand.Execute(null);
    var merged = Assert.Single(_state.Columns);
    Assert.Equal(first.Name, merged.Name);
    Assert.Equal(["C1", "C2"], merged.Component.Entry.Imported.Positions);
    _vm.UndoCommand.Execute(null);
    Assert.Equal(2, _state.Columns.Count);
    Assert.Same(first, _state.Columns[0]);
    Assert.Same(second, _state.Columns[1]);
    _vm.RedoCommand.Execute(null);
    Assert.Single(_state.Columns);
  }

  [Fact]
  public void Split_dialog_cannot_confirm_after_removing_all_but_one_group()
  {
    var dialog = new OperationMaps.Wpf.Features.OwnForm.SplitDialog.SplitDialogViewModel(["C1", "C2"]);
    foreach (var position in dialog.SourcePositions.ToArray()) dialog.MoveToGroup(position, dialog.Groups[0]);
    Assert.False(dialog.CanConfirm);
    dialog.Groups[1].RemoveCommand.Execute(null);
    Assert.False(dialog.CanConfirm);
  }

  [Fact]
  public async Task Split_sorts_all_columns_naturally_and_undo_restores_original_order()
  {
    var original = _state.Columns[0];
    var other = _state.Columns[1];
    original.Component.SetPositions(["C10", "C2", "C1"]);
    other.Component.SetPositions(["C3"]);
    _state.Columns.Move(1, 0);
    await _vm.OnNavigatedToAsync(67);
    _vm.ExecuteMultiSplit(original, [["C10"], ["C2", "C1"]]);
    Assert.Equal(["C1, C2", "C3", "C10"], _state.Columns.Select(c => c.Positions));
    Assert.Equal(["C1", "C2"], _state.Columns[0].Component.Entry.Imported.Positions);
    _vm.UndoCommand.Execute(null);
    Assert.Same(other, _state.Columns[0]);
    Assert.Same(original, _state.Columns[1]);
    Assert.Equal(["C10", "C2", "C1"], original.Component.Entry.Imported.Positions);
    _vm.RedoCommand.Execute(null);
    Assert.Equal(["C1, C2", "C3", "C10"], _state.Columns.Select(c => c.Positions));
    _vm.UndoCommand.Execute(null);
    _vm.ExecuteSplit(original, ["C10"], ["C2", "C1"]);
    Assert.Equal(["C1, C2", "C3", "C10"], _state.Columns.Select(c => c.Positions));
  }

  [Fact]
  public async Task Split_request_distributes_positions_and_preserves_values_notes_and_undo()
  {
    var original = _state.Columns[0];
    original.Component.SetPositions(["C1", "C3", "C4"]);
    original.SetCellValue(1, "5");
    original.AddCatalogNote(1, "Сохранённое примечание");
    await _vm.OnNavigatedToAsync(67);
    _vm.SelectColumnCommand.Execute(original);
    Assert.True(_vm.SplitCommand.CanExecute(null));
    FormColumnVm? requested = null;
    _vm.SplitRequested += column => requested = column;
    _vm.SplitCommand.Execute(null);
    Assert.Same(original, requested);
    var dialog = new OperationMaps.Wpf.Features.OwnForm.SplitDialog.SplitDialogViewModel(original.Component.Entry.Imported.Positions);
    foreach (var position in dialog.SourcePositions.ToArray())
      dialog.MoveToGroup(position, position.Value == "C1" ? dialog.Groups[0] : dialog.Groups[1]);
    Assert.True(dialog.CanConfirm);
    _vm.ExecuteMultiSplit(original, dialog.GetResult());
    Assert.Equal(3, _state.Columns.Count);
    Assert.Equal(["C1"], _state.Columns[0].Component.Entry.Imported.Positions);
    Assert.Equal(["C3", "C4"], _state.Columns[2].Component.Entry.Imported.Positions);
    foreach (var column in new[] { _state.Columns[0], _state.Columns[2] })
    {
      Assert.Equal("5", column.GetCellValue(1));
      Assert.Equal("10", column.GetNtdValue(1));
      Assert.Single(column.GetNotes(1));
      Assert.True(Assert.Single(column.GetAvailableNotes(1)).IsSelected);
    }
    _vm.UndoCommand.Execute(null);
    Assert.Equal(2, _state.Columns.Count);
    Assert.Same(original, _state.Columns[0]);
    _vm.RedoCommand.Execute(null);
    Assert.Equal(3, _state.Columns.Count);
    Assert.Equal("5", _state.Columns[2].GetCellValue(1));
  }

  [Fact]
  public async Task Next_missing_cycles_between_components_and_stops_when_all_are_filled()
  {
    await _vm.OnNavigatedToAsync(67);
    var requested = new List<int>();
    _vm.MissingParameterRequested += row => requested.Add(row.FormParameterId);
    _vm.NextMissingCommand.Execute(null);
    Assert.Same(_state.Columns[0], _vm.SelectedColumn);
    Assert.Equal(1, requested.Last());
    _vm.NextMissingCommand.Execute(null);
    Assert.Same(_state.Columns[1], _vm.SelectedColumn);
    _vm.NextMissingCommand.Execute(null);
    Assert.Same(_state.Columns[0], _vm.SelectedColumn);

    _vm.ParameterDetails.Single(p => p.FormParameterId == 1).SchemeValue = "5";
    _vm.NextMissingCommand.Execute(null);
    Assert.Same(_state.Columns[1], _vm.SelectedColumn);
    _vm.ParameterDetails.Single(p => p.FormParameterId == 1).SchemeValue = "3";
    Assert.False(_vm.NextMissingCommand.CanExecute(null));
    Assert.All(_vm.ColumnItems, i => Assert.Equal(FillStatus.Complete, i.FillStatus));
    Assert.Contains("Значений: 2 из 2", _vm.ProgressSummary);
  }

  [Fact]
  public async Task Filter_updates_while_typing_without_losing_detail_selection()
  {
    await _vm.OnNavigatedToAsync(67);
    _vm.ShowOnlyIncomplete = true;
    _vm.SelectColumnCommand.Execute(_state.Columns[0]);
    var selected = _vm.SelectedColumn;
    _vm.ParameterDetails.Single(p => p.FormParameterId == 1).SchemeValue = "5";

    Assert.Single(_vm.VisibleColumnItems);
    Assert.Same(selected, _vm.SelectedColumn);
    _vm.NextMissingCommand.Execute(null);
    _vm.ParameterDetails.Single(p => p.FormParameterId == 1).SchemeValue = "3";
    Assert.Empty(_vm.VisibleColumnItems);
    Assert.True(_vm.IsFilteredListEmpty);
    _vm.ShowOnlyIncomplete = false;
    Assert.Equal(2, _vm.VisibleColumnItems.Count);
  }

  [Fact]
  public async Task Active_optional_row_uses_its_own_value_and_is_a_navigation_target()
  {
    _state.Columns[0].OptionalNtdValues[2] = "20";
    await _vm.OnNavigatedToAsync(67);
    Assert.Equal(2, _vm.ColumnItems[0].TotalCount);
    Assert.Equal(1, _vm.ColumnItems[1].TotalCount);
    var requested = new List<int>();
    _vm.MissingParameterRequested += row => requested.Add(row.FormParameterId);
    _vm.NextMissingCommand.Execute(null);
    _vm.ParameterDetails[0].SchemeValue = "5";
    _vm.NextMissingCommand.Execute(null);
    Assert.Equal(2, requested.Last());
    _vm.ParameterDetails[0].OptionalSchemeValue = "7";
    Assert.Equal(FillStatus.Complete, _vm.ColumnItems[0].FillStatus);
    Assert.Equal(2, _vm.ColumnItems[0].FilledCount);
  }

  [Fact]
  public async Task Input_fields_are_visited_before_computed_rows()
  {
    _state.SetParameters([
      new FormParameterRowVm(new FormParameter { Id = 4, RowNumber = 1, Name = "Коэффициент", IsLoadFactorResult = true }, _state.Columns),
      new FormParameterRowVm(new FormParameter { Id = 1, RowNumber = 2, Name = "Напряжение", IsRequired = true }, _state.Columns),
    ]);
    await _vm.OnNavigatedToAsync(67);
    FormParameterRowVm? requested = null;
    _vm.MissingParameterRequested += row => requested = row;
    _vm.NextMissingCommand.Execute(null);
    Assert.Equal(1, requested?.FormParameterId);
    _vm.NextMissingCommand.Execute(null);
    Assert.Equal(4, requested?.FormParameterId);
  }

  [Fact]
  public async Task Load_factor_hint_distinguishes_component_choice_from_form_default_and_follows_selection()
  {
    await _vm.OnNavigatedToAsync(67);
    _vm.LoadFactorOptions.Add(new LoadFactorOptionVm(1, 1, "Напряжение"));
    _vm.SelectColumnCommand.Execute(_state.Columns[0]);
    Assert.Contains("Выберите параметр для этого компонента", _vm.DefaultLoadFactorLabel);

    var notifications = new List<string?>();
    _vm.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);
    _state.Columns[0].LoadFactorBaseParameterId = 1;
    Assert.Contains(nameof(OwnFormViewModel.DefaultLoadFactorLabel), notifications);
    Assert.Contains("Для этого компонента параметр выбран", _vm.DefaultLoadFactorLabel);
    Assert.DoesNotContain("не рассчитывается", _vm.DefaultLoadFactorLabel);

    notifications.Clear();
    _vm.SelectColumnCommand.Execute(_state.Columns[1]);
    Assert.Contains(nameof(OwnFormViewModel.DefaultLoadFactorLabel), notifications);
    Assert.Contains("Выберите параметр для этого компонента", _vm.DefaultLoadFactorLabel);
  }

  [Fact]
  public async Task Load_factor_hint_shows_existing_form_default()
  {
    _state.SetDefaultLoadFactorParameter(1);
    await _vm.OnNavigatedToAsync(67);
    _vm.LoadFactorOptions.Add(new LoadFactorOptionVm(1, 1, "Напряжение"));
    _vm.SelectColumnCommand.Execute(_state.Columns[0]);
    Assert.Equal("По умолчанию для формы: Напряжение (стр. 1)", _vm.DefaultLoadFactorLabel);
  }

  [Fact]
  public void Group_without_applicable_fields_is_not_a_permanent_gap()
  {
    var item = new ColumnListItemVm(_state.Columns[0], [_state.Parameters[2]]);
    Assert.Equal(0, item.TotalCount);
    Assert.Equal(FillStatus.Complete, item.FillStatus);
    Assert.Equal("Не требуется", item.StatusLabel);
  }

  [Fact]
  public async Task Placeholders_do_not_count_as_entered_values()
  {
    await _vm.OnNavigatedToAsync(67);
    _state.Columns[0].SetCellValue(1, "—");
    _state.Columns[1].SetCellValue(1, "-");
    Assert.All(_vm.ColumnItems, i => Assert.Equal(0, i.FilledCount));
    Assert.True(_vm.NextMissingCommand.CanExecute(null));
  }

  public void Dispose()
  {
    _vm.OnNavigatedFromAsync().GetAwaiter().GetResult();
    _provider.Dispose();
  }
}
