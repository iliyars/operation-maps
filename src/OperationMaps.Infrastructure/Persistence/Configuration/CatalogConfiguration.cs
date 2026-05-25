using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationMaps.Domain.Entities.Catalog;

namespace OperationMaps.Infrastructure.Persistence.Configurations;

public class ComponentTypeConfiguration : IEntityTypeConfiguration<ComponentType>
{
  public void Configure(EntityTypeBuilder<ComponentType> b)
  {
    b.Property(x => x.Name).HasMaxLength(200).IsRequired();
    b.HasIndex(x => x.Name).IsUnique();
  }
}

public class TypeFormConfiguration : IEntityTypeConfiguration<TypeForm>
{
  public void Configure(EntityTypeBuilder<TypeForm> b)
  {
    // составной ключ для M:N
    b.HasKey(x => new { x.ComponentTypeId, x.FormId });

    b.HasOne(x => x.ComponentType).WithMany(t => t.TypeForms)
     .HasForeignKey(x => x.ComponentTypeId).OnDelete(DeleteBehavior.Cascade);

    b.HasOne(x => x.Form).WithMany()
     .HasForeignKey(x => x.FormId).OnDelete(DeleteBehavior.Cascade);
  }
}

public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
  public void Configure(EntityTypeBuilder<Family> b)
  {
    b.Property(x => x.Name).HasMaxLength(200).IsRequired();
    b.HasOne(x => x.ComponentType).WithMany(t => t.Families)
     .HasForeignKey(x => x.ComponentTypeId).OnDelete(DeleteBehavior.Cascade);
    b.HasIndex(x => new { x.ComponentTypeId, x.Name }).IsUnique();
  }
}

public class FamilyParsingRuleConfiguration : IEntityTypeConfiguration<FamilyParsingRule>
{
  public void Configure(EntityTypeBuilder<FamilyParsingRule> b)
  {
    b.Property(x => x.Pattern).HasMaxLength(1000).IsRequired();
    b.Property(x => x.Example).HasMaxLength(500);
    b.HasOne(x => x.ComponentType).WithMany(t => t.ParsingRules)
     .HasForeignKey(x => x.ComponentTypeId).OnDelete(DeleteBehavior.Cascade);
  }
}

public class ComponentConfiguration : IEntityTypeConfiguration<Component>
{
  public void Configure(EntityTypeBuilder<Component> b)
  {
    b.Property(x => x.FullName).HasMaxLength(500).IsRequired();
    b.Property(x => x.Designation).HasMaxLength(500);
    b.HasOne(x => x.Family).WithMany(f => f.Components)
     .HasForeignKey(x => x.FamilyId).OnDelete(DeleteBehavior.Cascade);
    b.HasIndex(x => x.FullName);
  }
}

public class FamilyNtdValueConfiguration : IEntityTypeConfiguration<FamilyNtdValue>
{
  public void Configure(EntityTypeBuilder<FamilyNtdValue> b)
  {
    b.Property(x => x.Value).HasMaxLength(1000);
    b.HasOne(x => x.Family).WithMany(f => f.NtdValues)
     .HasForeignKey(x => x.FamilyId).OnDelete(DeleteBehavior.Cascade);
    b.HasOne(x => x.FormParameter).WithMany()
     .HasForeignKey(x => x.FormParameterId).OnDelete(DeleteBehavior.NoAction);
    b.HasIndex(x => new { x.FamilyId, x.FormParameterId }).IsUnique();
  }
}

public class ComponentNtdValueConfiguration : IEntityTypeConfiguration<ComponentNtdValue>
{
  public void Configure(EntityTypeBuilder<ComponentNtdValue> b)
  {
    b.Property(x => x.Value).HasMaxLength(1000);
    b.HasOne(x => x.Component).WithMany(c => c.NtdValues)
     .HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.Cascade);
    b.HasOne(x => x.FormParameter).WithMany()
     .HasForeignKey(x => x.FormParameterId).OnDelete(DeleteBehavior.NoAction);
    b.HasIndex(x => new { x.ComponentId, x.FormParameterId }).IsUnique();
  }
}

public class ComponentPinValueConfiguration : IEntityTypeConfiguration<ComponentPinValue>
{
  public void Configure(EntityTypeBuilder<ComponentPinValue> b)
  {
    b.Property(x => x.Pins).HasMaxLength(2000);
    b.HasOne(x => x.Component).WithMany(c => c.PinValues)
     .HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.Cascade);
    b.HasOne(x => x.FormParameter).WithMany()
     .HasForeignKey(x => x.FormParameterId).OnDelete(DeleteBehavior.NoAction);
    b.HasIndex(x => new { x.ComponentId, x.FormParameterId }).IsUnique();
  }
}
