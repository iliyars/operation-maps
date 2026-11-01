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
