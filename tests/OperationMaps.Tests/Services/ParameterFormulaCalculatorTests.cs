using OperationMaps.Application.Services;

namespace OperationMaps.Tests.Services;

public sealed class ParameterFormulaCalculatorTests
{
  [Theory]
  [InlineData("row1+row2", "2,5", "3.5", "6")]
  [InlineData(" ROW1 + row2 ", "2", "3", "5")]
  [InlineData("row1+row2", "", "3", "3")]
  [InlineData("row1+row2", "—", "-", null)]
  [InlineData("row1+row2", "NaN", "3", null)]
  [InlineData("row1+row2", "Infinity", "3", null)]
  [InlineData("row1+row2", "1e308", "1e308", null)]
  [InlineData("row1+other", "2", "3", null)]
  [InlineData("row1++row2", "2", "3", null)]
  public void Supported_sums_reject_invalid_formulas_and_non_finite_results(
    string formula, string first, string second, string? expected)
    => Assert.Equal(expected, ParameterFormulaCalculator.Calculate(formula, row => row == 1 ? first : second));

  [Theory]
  [InlineData("NaN", "10")]
  [InlineData("Infinity", "10")]
  [InlineData("5", "NaN")]
  [InlineData("5", "Infinity")]
  [InlineData("1e308", "1e-308")]
  public void Load_factor_does_not_produce_non_finite_results(string scheme, string ntd)
    => Assert.Null(LoadFactorCalculator.Calculate(scheme, ntd, 1));
}
