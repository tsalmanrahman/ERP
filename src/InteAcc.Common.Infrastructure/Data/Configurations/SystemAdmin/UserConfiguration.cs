using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteAcc.Common.Domain.Entities.SystemAdmin;

namespace InteAcc.Common.Infrastructure.Data.Configurations.SystemAdmin;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "sa");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserCode).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Username).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();

        builder.HasIndex(u => u.UserCode).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.RowVersion).IsRowVersion();
    }
}
