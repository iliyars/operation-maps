using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationMaps.Domain.Entities.Projects;

namespace OperationMaps.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
  public void Configure(EntityTypeBuilder<Project> b)
  {
    b.Property(x => x.Name).HasMaxLength(300).IsRequired();
    b.Property(x => x.SourceFileName).HasMaxLength(500);
    b.HasOne(x => x.CreatedBy).WithMany()
     .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.NoAction);
  }
}

public class ProjectComponentConfiguration : IEntityTypeConfiguration<ProjectComponent>
{
  public void Configure(EntityTypeBuilder<ProjectComponent> b)
  {
    b.Property(x => x.Designation).HasMaxLength(100).IsRequired();
    b.Property(x => x.RawName).HasMaxLength(500);
    b.HasOne(x => x.Project).WithMany(p => p.Components)
     .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    // Component опционален (может быть не распознан) — при удалении компонента просто null.
    b.HasOne(x => x.Component).WithMany()
     .HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.SetNull);
  }
}

public class RegimeGroupConfiguration : IEntityTypeConfiguration<RegimeGroup>
{
  public void Configure(EntityTypeBuilder<RegimeGroup> b)
  {
    b.Property(x => x.Label).HasMaxLength(2000);
    b.Property(x => x.LoadFactorMin).HasMaxLength(100);

    b.HasOne(x => x.Project).WithMany(p => p.RegimeGroups)
     .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    b.HasOne(x => x.Form).WithMany()
     .HasForeignKey(x => x.FormId).OnDelete(DeleteBehavior.NoAction);
    b.HasOne(x => x.LoadFactorParameter).WithMany()
     .HasForeignKey(x => x.LoadFactorParameterId).OnDelete(DeleteBehavior.NoAction);
  }
}

public class RegimeGroupMemberConfiguration : IEntityTypeConfiguration<RegimeGroupMember>
{
  public void Configure(EntityTypeBuilder<RegimeGroupMember> b)
  {
    b.HasKey(x => new { x.RegimeGroupId, x.ProjectComponentId });
    b.HasOne(x => x.RegimeGroup).WithMany(g => g.Members)
     .HasForeignKey(x => x.RegimeGroupId).OnDelete(DeleteBehavior.Cascade);
    // NoAction со стороны ProjectComponent, чтобы избежать множественных каскадных путей
    // (Project уже каскадит и группы, и компоненты).
    b.HasOne(x => x.ProjectComponent).WithMany()
     .HasForeignKey(x => x.ProjectComponentId).OnDelete(DeleteBehavior.NoAction);
  }
}

public class ParameterCellValueConfiguration : IEntityTypeConfiguration<ParameterCellValue>
{
  public void Configure(EntityTypeBuilder<ParameterCellValue> b)
  {
    b.Property(x => x.Value).HasMaxLength(2000); // допускает многострочный текст
    b.HasOne(x => x.RegimeGroup).WithMany(g => g.CellValues)
     .HasForeignKey(x => x.RegimeGroupId).OnDelete(DeleteBehavior.Cascade);
    b.HasOne(x => x.FormParameter).WithMany()
     .HasForeignKey(x => x.FormParameterId).OnDelete(DeleteBehavior.NoAction);
    b.HasOne(x => x.FormValueColumn).WithMany()
     .HasForeignKey(x => x.FormValueColumnId).OnDelete(DeleteBehavior.NoAction);

    // одна ячейка = одна (группа × строка × колонка)
    b.HasIndex(x => new { x.RegimeGroupId, x.FormParameterId, x.FormValueColumnId }).IsUnique();
  }
}
