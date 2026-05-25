using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OperationMaps.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(
      this IServiceCollection services, string connectionString)
  {
    services.AddDbContext<Persistence.OperationMapsDbContext>(options =>
        options.UseSqlite(connectionString));
    // при миграции на SQL Server: options.UseSqlServer(connectionString)

    return services;
  }
}
