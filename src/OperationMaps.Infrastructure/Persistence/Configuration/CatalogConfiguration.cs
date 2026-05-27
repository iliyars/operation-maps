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
    b.HasKey(x => new { x.ComponentTypeId, x.FormId });
  }
}

public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
  public void Configure(EntityTypeBuilder<Family> b)
  {
    b.Property(x => x.Name).HasMaxLength(200).IsRequired();
    b.HasIndex(x => new { x.ComponentTypeId, x.Name }).IsUnique();

    b.HasOne(x => x.ComponentType)
     .WithMany(t => t.Families)
     .HasForeignKey(x => x.ComponentTypeId)
     .OnDelete(DeleteBehavior.Cascade);
  }
}

public class FamilyParsingRuleConfiguration : IEntityTypeConfiguration<FamilyParsingRule>
{
  public void Configure(EntityTypeBuilder<FamilyParsingRule> b)
  {
    b.Property(x => x.Pattern).HasMaxLength(500).IsRequired();
    b.Property(x => x.Example).HasMaxLength(200);
  }
}

public class ComponentConfiguration : IEntityTypeConfiguration<Component>
{
  public void Configure(EntityTypeBuilder<Component> b)
  {
    b.Property(x => x.FullName).HasMaxLength(500).IsRequired();
    b.Property(x => x.Designation).HasMaxLength(200);

    b.HasOne(x => x.Family)
     .WithMany(f => f.Components)
     .HasForeignKey(x => x.FamilyId)
     .OnDelete(DeleteBehavior.Cascade);
  }
}

public class FamilyNtdValueConfiguration : IEntityTypeConfiguration<FamilyNtdValue>
{
  public void Configure(EntityTypeBuilder<FamilyNtdValue> b)
  {
    b.Property(x => x.Value).HasMaxLength(500).IsRequired();

    b.HasIndex(x => new { x.FamilyId, x.FormParameterId }).IsUnique();
  }
}

public class ComponentNtdValueConfiguration : IEntityTypeConfiguration<ComponentNtdValue>
{
  public void Configure(EntityTypeBuilder<ComponentNtdValue> b)
  {
    b.Property(x => x.Value).HasMaxLength(500).IsRequired();

    b.HasIndex(x => new { x.ComponentId, x.FormParameterId }).IsUnique();
  }
}

public class ComponentPinValueConfiguration : IEntityTypeConfiguration<ComponentPinValue>
{
  public void Configure(EntityTypeBuilder<ComponentPinValue> b)
  {
    b.Property(x => x.Pins).HasMaxLength(200).IsRequired();

    b.HasIndex(x => new { x.ComponentId, x.FormParameterId }).IsUnique();
  }
}

// ── Примечания ───────────────────────────────────────────────────────────────

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
  public void Configure(EntityTypeBuilder<Note> b)
  {
    b.Property(x => x.Text).HasMaxLength(2000).IsRequired();
    // Уникальность по тексту: один и тот же текст не дублируется в справочнике
    b.HasIndex(x => x.Text).IsUnique();
  }
}

public class FamilyNoteConfiguration : IEntityTypeConfiguration<FamilyNote>
{
  public void Configure(EntityTypeBuilder<FamilyNote> b)
  {
    b.HasKey(x => new { x.FamilyId, x.NoteId });

    b.HasOne(x => x.Family)
     .WithMany(f => f.FamilyNotes)
     .HasForeignKey(x => x.FamilyId)
     .OnDelete(DeleteBehavior.Cascade);

    b.HasOne(x => x.Note)
     .WithMany(n => n.FamilyNotes)
     .HasForeignKey(x => x.NoteId)
     .OnDelete(DeleteBehavior.Cascade);
  }
}

public class ComponentNoteConfiguration : IEntityTypeConfiguration<ComponentNote>
{
  public void Configure(EntityTypeBuilder<ComponentNote> b)
  {
    b.HasKey(x => new { x.ComponentId, x.NoteId });

    b.HasOne(x => x.Component)
     .WithMany(c => c.ComponentNotes)
     .HasForeignKey(x => x.ComponentId)
     .OnDelete(DeleteBehavior.Cascade);

    b.HasOne(x => x.Note)
     .WithMany(n => n.ComponentNotes)
     .HasForeignKey(x => x.NoteId)
     .OnDelete(DeleteBehavior.Cascade);
  }
}
