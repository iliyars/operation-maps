using OperationMaps.Application.Services;
using OperationMaps.Domain.Entities.Forms;
using Xunit;

namespace OperationMaps.Tests.Services;

public class LoadFactorSetupTests
{
  private static FormParameter P(int id, int row, string name, bool baseFlag = false, bool resultFlag = false,
      bool optional = false, string? formula = null)
      => new() { Id = id, RowNumber = row, Name = name, CanBeLoadFactorBase = baseFlag, IsLoadFactorResult = resultFlag,
                 IsOptional = optional, Formula = formula };

  [Fact]
  public void Explicit_flags_win_and_a_single_flagged_candidate_is_the_default()
  {
    var ps = new[] { P(1, 1, "Напряжение", baseFlag: true), P(2, 2, "Ток"), P(3, 3, "Кн", resultFlag: true) };

    var setup = LoadFactorSetup.For(new Form(), ps);

    Assert.Equal(3, setup.Result!.Id);
    Assert.Equal([1], setup.Candidates.Select(c => c.Id));
    Assert.Equal(1, setup.DefaultBaseId);
  }

  [Fact]
  public void Unconfigured_form_finds_result_by_name_and_offers_every_other_parameter_without_default()
  {
    var ps = new[]
    {
      P(1, 1, "Средняя мощность"), P(2, 2, "Импульсная мощность"),
      P(3, 3, "Второе напряжение", optional: true), P(4, 4, "Суммарное", formula: "row1+row2"),
      P(5, 5, "  коэффициент нагрузки "),
    };

    var setup = LoadFactorSetup.For(new Form(), ps);

    Assert.Equal(5, setup.Result!.Id);
    // Derived rows (Form 67's "Суммарное") are valid bases; optional/result rows aren't.
    Assert.Equal([1, 2, 4], setup.Candidates.Select(c => c.Id));
    Assert.Null(setup.DefaultBaseId); // not known yet — user chooses
  }

  [Fact]
  public void Admin_default_is_used_only_when_it_is_one_of_the_choices()
  {
    var ps = new[] { P(1, 1, "Мощность"), P(2, 2, "Ток"), P(9, 9, "Коэффициент нагрузки") };

    Assert.Equal(2, LoadFactorSetup.For(new Form { DefaultLoadFactorParameterId = 2 }, ps).DefaultBaseId);
    Assert.Null(LoadFactorSetup.For(new Form { DefaultLoadFactorParameterId = 9 }, ps).DefaultBaseId);
  }

  [Fact]
  public void Form_without_result_row_has_nothing_to_calculate()
  {
    var setup = LoadFactorSetup.For(new Form(), [P(1, 1, "Мощность")]);

    Assert.Null(setup.Result);
    Assert.Empty(setup.Candidates);
  }
}
