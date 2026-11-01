using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OperationMaps.Infrastructure.Persistence;

namespace OperationMaps.Tests;

internal sealed class TestCatalogDbContextFactory(DbContextOptions<CatalogDbContext> options)
    : IDbContextFactory<CatalogDbContext>
{
  public static TestCatalogDbContextFactory From(CatalogDbContext context)
      => new((DbContextOptions<CatalogDbContext>)context.GetService<IDbContextOptions>());

  public CatalogDbContext CreateDbContext() => new(options);
}
