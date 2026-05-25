using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationMaps.Domain.Entities.Forms;

namespace OperationMaps.Infrastructure.Persistence.Configurations;

public class FormConfiguration : IEntityTypeConfiguration<Form>
{
  public void Configure(EntityTypeBuilder<Form> b)
  {
    b.Property(x => x.Number).HasMaxLength(20).IsRequired();
    b.Property(x => x.Title).HasMaxLength(500);
    b.HasIndex(x => x.Number).IsUnique();

    b.HasOne(x => x.DefaultLoadFactorParameter)
     .WithMany()
     .HasForeignKey(x => x.DefaultLoadFactorParameterId)
     .OnDelete(DeleteBehavior.NoAction);
  }
}

public class FormSectionConfiguration : IEntityTypeConfiguration<FormSection>
{
  public void Configure(EntityTypeBuilder<FormSection> b)
  {
    b.Property(x => x.Title).HasMaxLength(300).IsRequired();
    b.HasOne(x => x.Form).WithMany(f => f.Sections)
     .HasForeignKey(x => x.FormId).OnDelete(DeleteBehavior.Cascade);
  }
}

public class FormParameterConfiguration : IEntityTypeConfiguration<FormParameter>
{
  public void Configure(EntityTypeBuilder<FormParameter> b)
  {
    b.Property(x => x.Name).HasMaxLength(500).IsRequired();
    b.Property(x => x.Unit).HasMaxLength(50);

    b.HasOne(x => x.Form).WithMany(f => f.Parameters)
     .HasForeignKey(x => x.FormId).OnDelete(DeleteBehavior.Cascade);

    // Section опциональна; удаление секции не должно сносить параметры — SetNull.
    b.HasOne(x => x.Section).WithMany(s => s.Parameters)
     .HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.SetNull);

    // в пределах формы номер пункта уникален
    b.HasIndex(x => new { x.FormId, x.RowNumber }).IsUnique();
  }
}

public class FormValueColumnConfiguration : IEntityTypeConfiguration<FormValueColumn>
{
  public void Configure(EntityTypeBuilder<FormValueColumn> b)
  {
    b.Property(x => x.Key).HasMaxLength(50).IsRequired();
    b.Property(x => x.Title).HasMaxLength(200).IsRequired();

    b.HasOne(x => x.Form).WithMany(f => f.ValueColumns)
     .HasForeignKey(x => x.FormId).OnDelete(DeleteBehavior.Cascade);

    b.HasIndex(x => new { x.FormId, x.Key }).IsUnique();
  }
}
