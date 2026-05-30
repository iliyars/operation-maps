using Microsoft.EntityFrameworkCore;
using OperationMaps.Domain.Entities.Projects;

namespace OperationMaps.Infrastructure.Persistence;

/// <summary>
/// Per-project database (*.omaps file).
/// Created when the user saves a new project or opens an existing one.
/// Each .omaps file is a self-contained SQLite database.
/// </summary>
public class ProjectDbContext : DbContext
{
  public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options) { }

  // ── Project data ──────────────────────────────────────────────────────────
  public DbSet<ProjectComponent> ProjectComponents => Set<ProjectComponent>();
  public DbSet<ParameterCellValue> ParameterCellValues => Set<ParameterCellValue>();
  public DbSet<ParameterCellNote> ParameterCellNotes => Set<ParameterCellNote>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectDbContext).Assembly);
  }
}
