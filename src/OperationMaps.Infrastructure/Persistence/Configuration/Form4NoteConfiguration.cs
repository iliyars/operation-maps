using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationMaps.Domain.Entities.Projects;

namespace OperationMaps.Infrastructure.Persistence.Configuration
{
  public class Form4NoteConfiguration : IEntityTypeConfiguration<Form4Note>
  {
    public void Configure(EntityTypeBuilder<Form4Note> builder)
    {
      builder.HasKey(n => n.Id);
      builder.Property(n => n.NoteText).IsRequired();

      builder.Property(n => n.FamilyId).IsRequired(false);
      builder.Property(n => n.ComponentId).IsRequired(false);

      builder.HasOne(n => n.FormParameter)
        .WithMany()
        .HasForeignKey(n => n.FormParameterId);
    }
  }
}
