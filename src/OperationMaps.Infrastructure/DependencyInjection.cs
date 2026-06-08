using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OperationMaps.Application.Importing;
using OperationMaps.Application.Services;
using OperationMaps.Infrastructure.Importing;
using OperationMaps.Infrastructure.Persistence;
using OperationMaps.Infrastructure.Services;
using OperationMaps.Infrastructure.Word;

namespace OperationMaps.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(
      this IServiceCollection services, string catalogConnectionString)
  {
    // ── Catalog DB (shared, permanent) ────────────────────────────────────
    services.AddDbContext<CatalogDbContext>(options =>
        options.UseSqlite(catalogConnectionString));

    // ── Project DB (per-file, opened on demand) ───────────────────────────
    // ProjectDbContext is NOT registered in DI — it's created by factory
    services.AddSingleton<ProjectDbContextFactory>();

    // ── Application services ──────────────────────────────────────────────
    services.AddSingleton<IComponentListImporter, Pe3XmlImporter>();
    services.AddScoped<IComponentNameParser, ComponentNameParser>();
    services.AddScoped<IComponentMatcher, ComponentMatcher>();

    // ── Word ──────────────────────────────────────────────────────────────
    var templatesPath = Path.Combine(
        AppContext.BaseDirectory, "Word", "Resources");

    // Singleton: map files are read once and cached for the app lifetime
    services.AddSingleton(new WordFormMapLoader(templatesPath));

    // Singleton: stateless, safe to share across all view models
    services.AddSingleton<IWordService, WordService>();
    services.AddSingleton<IOperatingConditionsService, OperatingConditionsService>();

    // Singleton: orchestrates export + merge, no mutable state
    services.AddSingleton<WordReportBuilder>();

    return services;

  }
}
