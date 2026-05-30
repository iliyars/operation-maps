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
  }
}

public class ProjectComponentConfiguration : IEntityTypeConfiguration<ProjectComponent>
{
  public void Configure(EntityTypeBuilder<ProjectComponent> b)
  {
    b.Property(x => x.Designation).HasMaxLength(200).IsRequired();
    b.Property(x => x.RawName).HasMaxLength(500);
    b.Property(x => x.DetectedCategory).HasMaxLength(200);

    b.HasOne(x => x.Project)
     .WithMany(p => p.Components)
     .HasForeignKey(x => x.ProjectId)
     .OnDelete(DeleteBehavior.Cascade);
  }
}

public class RegimeGroupMemberConfiguration : IEntityTypeConfiguration<RegimeGroupMember>
{
  public void Configure(EntityTypeBuilder<RegimeGroupMember> b)
  {
    b.HasKey(x => new { x.RegimeGroupId, x.ProjectComponentId });

    b.HasOne(x => x.RegimeGroup)
     .WithMany(g => g.Members)
     .HasForeignKey(x => x.RegimeGroupId)
     .OnDelete(DeleteBehavior.Cascade);
  }
}

public class ParameterCellValueConfiguration : IEntityTypeConfiguration<ParameterCellValue>
{
  public void Configure(EntityTypeBuilder<ParameterCellValue> b)
  {
    b.Property(x => x.Value).HasMaxLength(500).IsRequired();

    b.HasOne(x => x.RegimeGroup)
     .WithMany(g => g.CellValues)
     .HasForeignKey(x => x.RegimeGroupId)
     .OnDelete(DeleteBehavior.Cascade);

    b.HasOne(x => x.FormParameter)
     .WithMany()
     .HasForeignKey(x => x.FormParameterId)
     .OnDelete(DeleteBehavior.NoAction);

    b.HasOne(x => x.FormValueColumn)
     .WithMany()
     .HasForeignKey(x => x.FormValueColumnId)
     .OnDelete(DeleteBehavior.NoAction);

    // уникальность: в одной группе на одну ячейку (параметр × колонка) — одно значение
    b.HasIndex(x => new { x.RegimeGroupId, x.FormParameterId, x.FormValueColumnId }).IsUnique();
  }
}

public class ParameterCellNoteConfiguration : IEntityTypeConfiguration<ParameterCellNote>
{
  public void Configure(EntityTypeBuilder<ParameterCellNote> b)
  {
    b.HasOne(x => x.ParameterCellValue)
     .WithMany(c => c.Notes)
     .HasForeignKey(x => x.ParameterCellValueId)
     .OnDelete(DeleteBehavior.Cascade);

    b.HasOne(x => x.Note)
     .WithMany()
     .HasForeignKey(x => x.NoteId)
     .OnDelete(DeleteBehavior.Restrict); // не удаляем Note если она используется в карте

    // в одной ячейке порядковый номер уникален
    b.HasIndex(x => new { x.ParameterCellValueId, x.Order }).IsUnique();
  }
}
