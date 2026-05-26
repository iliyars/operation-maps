using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OperationMaps.Application.Importing;
using OperationMaps.Infrastructure.Importing;
using OperationMaps.Infrastructure.Services;

namespace OperationMaps.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(
      this IServiceCollection services, string connectionString)
  {
    services.AddDbContext<Persistence.OperationMapsDbContext>(options =>
        options.UseSqlite(connectionString));
    // при миграции на SQL Server: options.UseSqlServer(connectionString)

    services.AddSingleton<IComponentListImporter, Pe3XmlImporter>();

    services.AddScoped<IComponentMatcher, ComponentMatcher>();
    return services;
  }
}
