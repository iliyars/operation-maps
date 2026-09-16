using OperationMaps.Application.Importing;
using OperationMaps.Application.Word;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Wpf.Features.Components;
using OperationMaps.Wpf.Features.OwnForm;
using Xunit;

namespace OperationMaps.Tests.Wpf;

public class OwnFormWordDataBuilderTests
{
  private static FormColumnVm CreateColumn(string rawName, params string[] positions)
  {
    var entry = new ComponentMatchEntry
    {
      Imported = new ImportedComponent
      {
        ImportIndex = 0,
        RawName = rawName,
        DetectedCategory = "Резистор",
        Positions = positions,
      },
      MatchResult = new MatchResult
      {
        IsMatched = false,
        RequiredForms = [],
      },
    };
    return new FormColumnVm(new ProjectComponentVm(entry));
  }

  private static FormParameter CreatePrimary(int id, int rowNumber, string name = "Параметр") => new()
  {
    Id = id,
    RowNumber = rowNumber,
    Name = name,
  };

  private static FormParameter CreateOptional(int id, int rowNumber, int optionalForRowNumber) => new()
  {
    Id = id,
    RowNumber = rowNumber,
    Name = "Доп. параметр",
    IsOptional = true,
    OptionalForRowNumber = optionalForRowNumber,
  };

  [Fact]
  public void Build_maps_scheme_ntd_pin_and_optional_values_by_row_number()
  {
    var column = CreateColumn("К10-79-25", "R1", "R2");
    column.CellValues[101] = "5";
    column.NtdValues[101] = "10";
    column.PinValues[101] = "12";
    column.OptionalNtdValues[102] = "20";
    column.OptionalCellValues[102] = "15";
    column.PinValues[102] = "34";

    var columns = new List<FormColumnVm> { column };
    var primary = CreatePrimary(id: 101, rowNumber: 1);
    var optional = CreateOptional(id: 102, rowNumber: 99, optionalForRowNumber: 1);
    var parameters = new List<FormParameterRowVm>
    {
      new(primary, columns),
      new(optional, columns),
    };

    var data = OwnFormWordDataBuilder.Build("67", "DESIG-1", parameters, columns);

    Assert.Equal("67", data.FormNumber);
    Assert.Equal("DESIG-1", data.DocumentDesignation);
    Assert.Single(data.Components);

    var component = data.Components[0];
    Assert.Equal("К10-79-25", component.Name);
    Assert.Equal("2", component.Quantity); // 2 positions
    Assert.Equal(PositionRangeFormatter.Format(["R1", "R2"]), component.Positions);

    Assert.Equal("5", component.SchemeValues[1]);
    Assert.Equal("10", component.NtdValues[1]);
    Assert.Equal("12", component.PinValues[1]);

    var optionalRow = Assert.Single(component.OptionalRowValuesByParameter);
    Assert.Equal(1, optionalRow.Key); // primary's RowNumber, not the optional param's own RowNumber
    Assert.Equal("20", optionalRow.Value.NtdValue);
    Assert.Equal("15", optionalRow.Value.SchemeValue);
    Assert.Equal("34", optionalRow.Value.PinsValue);
  }

  [Fact]
  public void Build_concatenates_notes_in_order_with_star_markers()
  {
    var column = CreateColumn("Компонент", "C1");
    var primary = CreatePrimary(id: 1, rowNumber: 1);
    var columns = new List<FormColumnVm> { column };
    var parameters = new List<FormParameterRowVm> { new(primary, columns) };

    column.GetNotes(1).Add(new OwnFormNoteVm { FormParameterId = 1, NoteText = "первое" });
    column.GetNotes(1).Add(new OwnFormNoteVm { FormParameterId = 1, NoteText = "второе" });
    column.RecalculateNoteOrders();

    var data = OwnFormWordDataBuilder.Build("4", "", parameters, columns);

    Assert.Equal("* первое\n** второе", data.Components[0].Note);
  }

  [Fact]
  public void Build_skips_empty_scheme_ntd_pin_values()
  {
    var column = CreateColumn("Компонент", "C1");
    // Nothing set on the column — all dictionaries empty.
    var primary = CreatePrimary(id: 1, rowNumber: 1);
    var columns = new List<FormColumnVm> { column };
    var parameters = new List<FormParameterRowVm> { new(primary, columns) };

    var data = OwnFormWordDataBuilder.Build("4", "", parameters, columns);

    var component = data.Components[0];
    Assert.Empty(component.SchemeValues);
    Assert.Empty(component.NtdValues);
    Assert.Empty(component.PinValues);
    Assert.Empty(component.OptionalRowValuesByParameter);
    Assert.Equal("", component.Note);
  }
}
