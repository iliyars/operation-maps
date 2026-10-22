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
      this IServiceCollection services,
      string catalogConnectionString,
      CatalogDatabaseOptions catalogOptions)
  {
    // ── Catalog DB (possibly a file in a shared network folder) ───────────
    var connectionString = CatalogConnection.Build(
        catalogConnectionString, allowCreate: catalogOptions.UpdateSchemaOnStartup);

    services.AddDbContext<CatalogDbContext>(options =>
        options.UseSqlite(connectionString));
    services.AddSingleton(catalogOptions);
    services.AddScoped<CatalogDatabaseInitializer>();

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
    services.AddSingleton<ITitlePageService>(
        new TitlePageService(Path.Combine(templatesPath, "Title", "template.docx")));
    services.AddSingleton<IContentsPageService>(
        new ContentsPageService(Path.Combine(templatesPath, "Content", "template.docx")));
    services.AddSingleton<IRegistrationSheetService>(
        new RegistrationSheetService(Path.Combine(templatesPath, "Registration", "template.docx")));
    services.AddSingleton<IOperatingConditionsService, OperatingConditionsService>();
    services.AddScoped<IComponentEntryService, ComponentEntryService>();
    services.AddScoped<IAdminAccountService, AdminAccountService>();
    services.AddScoped<ICatalogEditService, CatalogEditService>();
    services.AddScoped<IWordCatalogImportService, WordCatalogImportService>();

    // Singleton: orchestrates export + merge, no mutable state
    services.AddSingleton<WordReportBuilder>();

    return services;

  }
}
