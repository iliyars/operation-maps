using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OperationMaps.Infrastructure.Persistence;

public class OperationMapsDbContextFactory : IDesignTimeDbContextFactory<OperationMapsDbContext>
{
  public OperationMapsDbContext CreateDbContext(string[] args)
  {
    var options = new DbContextOptionsBuilder<OperationMapsDbContext>()
        .UseSqlite("Data Source=operationmaps_design.db")
        .Options;
    return new OperationMapsDbContext(options);
  }
}
