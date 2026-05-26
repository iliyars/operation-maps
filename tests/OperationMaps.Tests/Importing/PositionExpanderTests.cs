using OperationMaps.Infrastructure.Importing;
using Xunit;

namespace OperationMaps.Tests.Importing;

public class PositionExpanderTests
{
  [Theory]
  [InlineData("C1, C2", new[] { "C1", "C2" })]
  [InlineData("C4 - C6", new[] { "C4", "C5", "C6" })]
  [InlineData("R25 -  R27", new[] { "R25", "R26", "R27" })] // двойные пробелы
  [InlineData("R18 , R19", new[] { "R18", "R19" })]          // пробел перед запятой
  [InlineData("R8", new[] { "R8" })]
  public void Expand_handles_real_formats(string input, string[] expected)
  {
    Assert.Equal(expected, PositionExpander.Expand(input));
  }

  [Fact]
  public void Expand_large_range()
  {
    var result = PositionExpander.Expand("C26 - C54");
    Assert.Equal(29, result.Count);
    Assert.Equal("C26", result[0]);
    Assert.Equal("C54", result[^1]);
  }
}
