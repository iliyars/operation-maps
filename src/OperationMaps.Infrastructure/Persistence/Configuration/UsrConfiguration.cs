using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationMaps.Domain.Entities.Users;

namespace OperationMaps.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
  public void Configure(EntityTypeBuilder<AppUser> b)
  {
    b.Property(x => x.Login).HasMaxLength(100).IsRequired();
    b.Property(x => x.DisplayName).HasMaxLength(200);
    b.Property(x => x.PasswordHash).HasMaxLength(500);
    b.HasIndex(x => x.Login).IsUnique();
  }
}
