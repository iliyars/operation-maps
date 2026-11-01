using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OperationMaps.Application.Services;
using OperationMaps.Infrastructure;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Wpf;
using OperationMaps.Wpf.Main;

namespace OperationMaps.Tests.Persistence;

public sealed class DependencyInjectionTests
{
  [Fact]
  public async Task Presentation_resolves_from_root_with_scope_validation_and_independent_contexts()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructure("Data Source=:memory:", new CatalogDatabaseOptions());
    services.AddPresentation();
    services.AddSingleton(OperationMaps.Wpf.Services.UserPreferences.CreateInMemory());

    await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
    {
      ValidateScopes = true,
      ValidateOnBuild = true,
    });

    Assert.NotNull(provider.GetRequiredService<MainViewModel>());
    Assert.NotNull(provider.GetRequiredService<IAdminAccountService>());

    var factory = provider.GetRequiredService<IDbContextFactory<CatalogDbContext>>();
    await using var first = await factory.CreateDbContextAsync();
    await using var second = await factory.CreateDbContextAsync();
    Assert.NotSame(first, second);
    await first.DisposeAsync();
    Assert.True(await second.Database.CanConnectAsync());
  }
}
