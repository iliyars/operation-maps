using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OperationMaps.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tools (dotnet ef migrations) to create CatalogDbContext.
/// </summary>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
  public CatalogDbContext CreateDbContext(string[] args)
  {
    var options = new DbContextOptionsBuilder<CatalogDbContext>()
        .UseSqlite("Data Source=catalog.db")
        .Options;

    return new CatalogDbContext(options);
  }
}
