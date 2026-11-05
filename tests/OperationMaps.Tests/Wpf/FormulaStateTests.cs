using OperationMaps.Application.Importing;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.OwnForm;

namespace OperationMaps.Tests.Wpf;

public sealed class FormulaStateTests
{
  [Fact]
  public void Unvisited_column_recalculates_sum_then_load_factor_and_exports_current_values()
  {
    var (state, column) = Create();
    column.SetCellValue(11, "2,5");
    column.SetCellValue(12, "3.5");
    Assert.Equal(6, double.Parse(column.GetCellValue(13)));
    Assert.Equal("0,6 (3)", column.LoadFactorValue);
    var data = OwnFormWordDataBuilder.Build("67", "Плата", state.Parameters, state.Columns);
    Assert.Equal(column.GetCellValue(13), data.Components[0].SchemeValues[3]);
    Assert.Equal("0,6 (3)", data.Components[0].SchemeValues[4]);

    column.SetCellValue(11, "");
    column.SetCellValue(12, "—");
    Assert.Equal("", column.GetCellValue(13));
    Assert.Equal("", column.LoadFactorValue);
    Assert.Equal("", column.GetCellValue(14));
  }

  [Fact]
  public void Replacing_columns_discards_stale_calculations_and_attaches_new_columns_only()
  {
    var (state, original) = Create();
    var restored = original.CloneWithPositions(["C2"]);
    restored.CellValues[11] = "4";
    restored.CellValues[12] = "3";
    restored.CellValues[13] = "999";
    state.ReplaceColumns([restored]);
    Assert.Equal("7", restored.GetCellValue(13));
    Assert.Equal("0,7 (3)", restored.LoadFactorValue);
    var notifications = 0;
    state.CalculatedValuesChanged += _ => notifications++;
    original.SetCellValue(11, "8");
    Assert.Equal(0, notifications);
    restored.SetCellValue(12, "1");
    Assert.Equal(1, notifications);
    Assert.Equal("5", restored.GetCellValue(13));
  }

  [Fact]
  public void Clearing_or_invalidating_base_clears_previous_load_factor()
  {
    var (state, column) = Create();
    column.SetCellValue(11, "5");
    Assert.Equal("0,5 (3)", column.LoadFactorValue);
    column.LoadFactorBaseParameterId = null;
    Assert.Equal("", column.LoadFactorValue);
    Assert.Equal("", column.GetCellValue(14));
    column.LoadFactorBaseParameterId = 13;
    Assert.Equal("0,5 (3)", column.LoadFactorValue);
    column.LoadFactorBaseParameterId = 999;
    Assert.Equal("", column.LoadFactorValue);
    Assert.Equal("", column.GetCellValue(14));
    column.LoadFactorBaseParameterId = 13;
    column.NtdValues[13] = "0";
    state.RecalculateCalculatedValues(column);
    Assert.Equal("", column.LoadFactorValue);
  }

  [Fact]
  public void Merge_and_undo_recalculate_derived_rows_without_screen_callbacks()
  {
    var (state, first) = Create();
    var second = first.CloneWithPositions(["C2"]);
    state.Columns.Add(second);
    first.SetCellValue(11, "3");
    second.SetCellValue(12, "5");
    var plan = new OperationMaps.Wpf.Features.OwnForm.Commands.ColumnMergePlan(first, second, calculated: [13, 14]);
    var command = new OperationMaps.Wpf.Features.OwnForm.Commands.MergeColumnsCommand(state.Columns, first, second, plan);
    state.History.Execute(command);
    Assert.Equal("8", Assert.Single(state.Columns).GetCellValue(13));
    Assert.Equal("0,8 (3)", state.Columns[0].LoadFactorValue);
    state.History.Undo();
    Assert.Equal("3", state.Columns[0].GetCellValue(13));
    Assert.Equal("5", state.Columns[1].GetCellValue(13));
    state.History.Redo();
    Assert.Equal("8", Assert.Single(state.Columns).GetCellValue(13));
  }

  [Fact]
  public void Chained_formulas_are_evaluated_by_dependencies_rather_than_display_order()
  {
    var (state, column) = Create();
    state.SetParameters([
      new(new FormParameter { Id = 15, RowNumber = 5, Name = "Итог", Formula = "row3+row2" }, state.Columns),
      ..state.Parameters,
    ]);
    column.SetCellValue(11, "2");
    column.SetCellValue(12, "3");
    Assert.Equal("8", column.GetCellValue(15));
    column.SetCellValue(12, "4");
    Assert.Equal("10", column.GetCellValue(15));
  }

  private static (OwnFormState State, FormColumnVm Column) Create()
  {
    var state = new OwnFormState(67, "67", "Форма", [], 14, 13);
    FormParameter[] parameters = [
      new() { Id = 11, RowNumber = 1, Name = "Первое" },
      new() { Id = 12, RowNumber = 2, Name = "Второе" },
      new() { Id = 13, RowNumber = 3, Name = "Суммарное", Formula = "row1+row2" },
      new() { Id = 14, RowNumber = 4, Name = "Коэффициент", IsLoadFactorResult = true },
    ];
    state.SetParameters(parameters.Select(p => new FormParameterRowVm(p, state.Columns)).ToList());
    var component = new ProjectComponentVm(new ComponentMatchEntry
    {
      Imported = new ImportedComponent { RawName = "Компонент", DetectedCategory = "Конденсатор", Positions = ["C1"] },
      MatchResult = new MatchResult { IsMatched = false },
    });
    var column = new FormColumnVm(component) { LoadFactorBaseParameterId = 13 };
    column.NtdValues[13] = "10";
    state.Columns.Add(column);
    return (state, column);
  }
}
