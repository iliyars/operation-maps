using Microsoft.EntityFrameworkCore;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Domain.Entities.Users;

namespace OperationMaps.Infrastructure.Persistence;

/// <summary>
/// Shared catalog database (catalog.db).
/// Contains forms, component types, families, NTD values.
/// Managed by Admin, shared across all projects.
/// </summary>
public class CatalogDbContext : DbContext
{
  public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

  // ── Forms ─────────────────────────────────────────────────────────────────
  public DbSet<Form> Froms => Set<Form>();
  public DbSet<FormSection> FormSections => Set<FormSection>();
  public DbSet<FormParameter> FormParameters => Set<FormParameter>();
  public DbSet<FormValueColumn> FormValueColumns => Set<FormValueColumn>();

  // ── Catalog ───────────────────────────────────────────────────────────────
  public DbSet<ComponentType> ComponentTypes => Set<ComponentType>();
  public DbSet<Family> Families => Set<Family>();
  public DbSet<FamilyForm> FamilyForms => Set<FamilyForm>();
  public DbSet<FamilyParsingRule> FamilyParsingRules => Set<FamilyParsingRule>();
  public DbSet<Component> Components => Set<Component>();
  public DbSet<FamilyNtdValue> FamilyNtdValues => Set<FamilyNtdValue>();
  public DbSet<ComponentNtdValue> ComponentNtdValues => Set<ComponentNtdValue>();
  public DbSet<ComponentPinValue> ComponentPinValues => Set<ComponentPinValue>();
  public DbSet<Note> Notes => Set<Note>();
  public DbSet<FamilyNote> FamilyNotes => Set<FamilyNote>();
  public DbSet<ComponentNote> ComponentNotes => Set<ComponentNote>();

  // ── Users ─────────────────────────────────────────────────────────────────
  public DbSet<AppUser> Users => Set<AppUser>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
  }
}
