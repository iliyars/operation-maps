using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationMaps.Domain.Entities.Catalog;

namespace OperationMaps.Infrastructure.Persistence.Configurations;

public class ComponentTypeConfiguration : IEntityTypeConfiguration<ComponentType>
{
  public void Configure(EntityTypeBuilder<ComponentType> b)
  {
    b.HasKey(t => t.Id);
    b.Property(x => x.Name).HasMaxLength(200).IsRequired();
    b.HasIndex(x => x.Name).IsUnique();
  }
}

public class FamilyFormConfiguration : IEntityTypeConfiguration<FamilyForm>
{
  public void Configure(EntityTypeBuilder<FamilyForm> builder)
  {
    builder.HasKey(ff => new { ff.FamilyId, ff.FormId });

    builder.HasOne(ff => ff.Family)
           .WithMany(f => f.FamilyForms)
           .HasForeignKey(ff => ff.FamilyId);

    builder.HasOne(ff => ff.Form)
           .WithMany()
           .HasForeignKey(ff => ff.FormId);
  }
}

public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
  public void Configure(EntityTypeBuilder<Family> b)
  {
    b.HasKey(t => t.Id);
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
  public void Configure(EntityTypeBuilder<FamilyParsingRule> builder)
  {
    builder.HasKey(r => r.Id);
    builder.Property(r => r.Pattern).IsRequired();

    builder.HasOne(r => r.ComponentType)
           .WithMany(t => t.ParsingRules)
           .HasForeignKey(r => r.ComponentTypeId);
  }
}


public class ComponentConfiguration : IEntityTypeConfiguration<Component>
{
  public void Configure(EntityTypeBuilder<Component> builder)
  {
    builder.HasKey(c => c.Id);
    builder.Property(c => c.FullName).IsRequired().HasMaxLength(500);
    builder.HasIndex(c => c.FullName).IsUnique();
    builder.Property(c => c.NeedsAdminReview).HasDefaultValue(false);

    builder.HasOne(c => c.Family)
           .WithMany(f => f.Components)
           .HasForeignKey(c => c.FamilyId);

    builder.HasOne(c => c.OwnForm)
           .WithMany()
           .HasForeignKey(c => c.OwnFormId)
           .IsRequired(false);
  }
}


public class FamilyNtdValueConfiguration : IEntityTypeConfiguration<FamilyNtdValue>
{
  public void Configure(EntityTypeBuilder<FamilyNtdValue> builder)
  {
    builder.HasKey(v => v.Id);

    builder.HasOne(v => v.Family)
           .WithMany(f => f.NtdValues)
           .HasForeignKey(v => v.FamilyId);

    builder.HasOne(v => v.FormParameter)
           .WithMany()
           .HasForeignKey(v => v.FormParameterId);
  }
}


public class ComponentNtdValueConfiguration : IEntityTypeConfiguration<ComponentNtdValue>
{
  public void Configure(EntityTypeBuilder<ComponentNtdValue> builder)
  {
    builder.HasKey(v => v.Id);

    builder.HasOne(v => v.Component)
           .WithMany(c => c.NtdValues)
           .HasForeignKey(v => v.ComponentId);

    builder.HasOne(v => v.FormParameter)
           .WithMany()
           .HasForeignKey(v => v.FormParameterId);
  }
}


public class ComponentPinValueConfiguration : IEntityTypeConfiguration<ComponentPinValue>
{
  public void Configure(EntityTypeBuilder<ComponentPinValue> builder)
  {
    builder.HasKey(v => v.Id);

    builder.HasOne(v => v.Component)
           .WithMany(c => c.PinValues)
           .HasForeignKey(v => v.ComponentId);

    builder.HasOne(v => v.FormParameter)
           .WithMany()
           .HasForeignKey(v => v.FormParameterId);
  }
}


// ── Примечания ───────────────────────────────────────────────────────────────

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
  public void Configure(EntityTypeBuilder<Note> builder)
  {
    builder.HasKey(n => n.Id);
    builder.Property(n => n.Text).IsRequired();
  }
}


public class FamilyNoteConfiguration : IEntityTypeConfiguration<FamilyNote>
{
  public void Configure(EntityTypeBuilder<FamilyNote> builder)
  {
    builder.HasKey(fn => new { fn.FamilyId, fn.FormParameterId, fn.NoteId });

    builder.HasOne(fn => fn.Family)
           .WithMany(f => f.FamilyNotes)
           .HasForeignKey(fn => fn.FamilyId);

    builder.HasOne(fn => fn.FormParameter)
           .WithMany()
           .HasForeignKey(fn => fn.FormParameterId);

    builder.HasOne(fn => fn.Note)
           .WithMany(n => n.FamilyNotes)
           .HasForeignKey(fn => fn.NoteId);
  }
}


public class ComponentNoteConfiguration : IEntityTypeConfiguration<ComponentNote>
{
  public void Configure(EntityTypeBuilder<ComponentNote> builder)
  {
    builder.HasKey(cn => new { cn.ComponentId, cn.FormParameterId, cn.NoteId });

    builder.HasOne(cn => cn.Component)
           .WithMany(c => c.ComponentNotes)
           .HasForeignKey(cn => cn.ComponentId);

    builder.HasOne(cn => cn.FormParameter)
           .WithMany()
           .HasForeignKey(cn => cn.FormParameterId);

    builder.HasOne(cn => cn.Note)
           .WithMany(n => n.ComponentNotes)
           .HasForeignKey(cn => cn.NoteId);
  }
}

