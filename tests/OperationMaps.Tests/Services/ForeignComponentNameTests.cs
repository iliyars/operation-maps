using OperationMaps.Infrastructure.Services;

namespace OperationMaps.Tests.Services;

public class ForeignComponentNameTests
{
  [Theory]
  [InlineData("Микросхема A3PE600-2PQ208I Microsemi", "A3PE600-2PQ208I")]
  [InlineData("Микросхема A3PE600-2PQ208I microsemi", "A3PE600-2PQ208I")]
  [InlineData("Микросхема A3PE600-2PQ208I", "A3PE600-2PQ208I")]
  [InlineData("A3PE600-2PQ208I Microsemi", "A3PE600-2PQ208I")]
  [InlineData("Диод 2Д510А/ББ ОС", "2Д510А/ББ ОС")]
  [InlineData("Диод 2ДС627А/ББ ОСМ", "2ДС627А/ББ ОСМ")]
  [InlineData("Микросхема A3PE600-2PQ208I Unknown", "A3PE600-2PQ208I Unknown")]
  public void Manufacturer_is_not_part_of_designation(string raw, string expected)
  {
    Assert.Equal(expected, new ComponentNameParser().Parse(raw).Name);
  }
}
