using Microsoft.EntityFrameworkCore;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Domain.Entities.Projects;
using OperationMaps.Domain.Entities.Users;

namespace OperationMaps.Infrastructure.Persistence;

public class OperationMapsDbContext : DbContext
{
  public OperationMapsDbContext(DbContextOptions<OperationMapsDbContext> options) : base(options) { }

  // Слой 1
  public DbSet<Form> Froms => Set<Form>();
  public DbSet<FormSection> FormSections => Set<FormSection>();
  public DbSet<FormParameter> FormParameters => Set<FormParameter>();
  public DbSet<FormValueColumn> FormValueColumns => Set<FormValueColumn>();

  // Слой 2
  public DbSet<ComponentType> ComponentTypes => Set<ComponentType>();
  public DbSet<TypeForm> TypeForms => Set<TypeForm>();
  public DbSet<Family> Families => Set<Family>();
  public DbSet<FamilyParsingRule> FamilyParsingRules => Set<FamilyParsingRule>();
  public DbSet<Component> Components => Set<Component>();
  public DbSet<FamilyNtdValue> FamilyNtdValues => Set<FamilyNtdValue>();
  public DbSet<ComponentNtdValue> ComponentNtdValues => Set<ComponentNtdValue>();
  public DbSet<ComponentPinValue> ComponentPinValues => Set<ComponentPinValue>();

  // Слой 3
  public DbSet<Project> Projects => Set<Project>();
  public DbSet<ProjectComponent> ProjectComponents => Set<ProjectComponent>();
  public DbSet<RegimeGroup> RegimeGroups => Set<RegimeGroup>();
  public DbSet<RegimeGroupMember> RegimeGroupMembers => Set<RegimeGroupMember>();
  public DbSet<ParameterCellValue> ParameterCellValues => Set<ParameterCellValue>();

  public DbSet<AppUser> Users => Set<AppUser>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    // подхватывает все IEntityTypeConfiguration из этой сборки
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperationMapsDbContext).Assembly);
  }
}
