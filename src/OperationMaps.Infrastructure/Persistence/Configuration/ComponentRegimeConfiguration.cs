using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationMaps.Domain.Entities.Catalog;

namespace OperationMaps.Infrastructure.Persistence.Configurations;

public sealed class ComponentRegimeConfiguration : IEntityTypeConfiguration<ComponentRegime>
{
  public void Configure(EntityTypeBuilder<ComponentRegime> b)
  {
    b.HasKey(r => r.Id);
    b.Property(r => r.Name).IsRequired().HasMaxLength(200);
    b.Property(r => r.Condition).IsRequired();
    b.HasIndex(r => new { r.ComponentId, r.Name }).IsUnique();
    b.HasOne(r => r.Component).WithMany().HasForeignKey(r => r.ComponentId).OnDelete(DeleteBehavior.Cascade);
  }
}
public sealed class ComponentRegimeValueConfiguration : IEntityTypeConfiguration<ComponentRegimeValue>
{
  public void Configure(EntityTypeBuilder<ComponentRegimeValue> b)
  {
    b.HasKey(v => new { v.ComponentRegimeId, v.FormParameterId });
    b.Property(v => v.Value).IsRequired();
    b.HasOne(v => v.ComponentRegime).WithMany(r => r.Values).HasForeignKey(v => v.ComponentRegimeId).OnDelete(DeleteBehavior.Cascade);
    b.HasOne(v => v.FormParameter).WithMany().HasForeignKey(v => v.FormParameterId).OnDelete(DeleteBehavior.Cascade);
  }
}
