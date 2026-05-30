using Microsoft.EntityFrameworkCore;
using OperationMaps.Domain.Entities.Catalog;
using OperationMaps.Domain.Entities.Forms;
using OperationMaps.Domain.Entities.Projects;
using OperationMaps.Domain.Entities.Users;

namespace OperationMaps.Infrastructure.Persistence;

public class OperationMapsDbContext : DbContext
{
  public OperationMapsDbContext(DbContextOptions<OperationMapsDbContext> options) : base(options) { }

  // Слой 1 — Формы
  public DbSet<Form> Froms => Set<Form>();
  public DbSet<FormSection> FormSections => Set<FormSection>();
  public DbSet<FormParameter> FormParameters => Set<FormParameter>();
  public DbSet<FormValueColumn> FormValueColumns => Set<FormValueColumn>();

  // Слой 2 — Каталог
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

  // Слой 3 — Проекты
  public DbSet<Project> Projects => Set<Project>();
  public DbSet<ProjectComponent> ProjectComponents => Set<ProjectComponent>();
  public DbSet<RegimeGroup> RegimeGroups => Set<RegimeGroup>();
  public DbSet<RegimeGroupMember> RegimeGroupMembers => Set<RegimeGroupMember>();
  public DbSet<ParameterCellValue> ParameterCellValues => Set<ParameterCellValue>();
  public DbSet<ParameterCellNote> ParameterCellNotes => Set<ParameterCellNote>(); // ★

  public DbSet<AppUser> Users => Set<AppUser>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperationMapsDbContext).Assembly);
  }
}
