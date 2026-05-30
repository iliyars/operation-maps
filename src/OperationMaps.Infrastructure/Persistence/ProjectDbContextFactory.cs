using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OperationMaps.Infrastructure.Persistence;

/// <summary>
/// Creates and migrates a <see cref="ProjectDbContext"/> for a given .omaps file path.
/// </summary>
public sealed class ProjectDbContextFactory : IDesignTimeDbContextFactory<ProjectDbContext>
{
  public ProjectDbContext CreateDbContext(string[] args)
  {
    var options = new DbContextOptionsBuilder<ProjectDbContext>()
        .UseSqlite("Data Source=project_design.omaps")
        .Options;

    return new ProjectDbContext(options);
  }

}
