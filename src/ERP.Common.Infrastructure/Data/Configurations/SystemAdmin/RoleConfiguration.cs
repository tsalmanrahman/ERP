using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.Common.Domain.Entities.SystemAdmin;

namespace ERP.Common.Infrastructure.Data.Configurations.SystemAdmin;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "sa");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RoleCode).HasMaxLength(50).IsRequired();
        builder.Property(r => r.RoleName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(255);
        builder.HasIndex(r => r.RoleCode).IsUnique();
        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", "sa");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserCompanyAccessConfiguration : IEntityTypeConfiguration<UserCompanyAccess>
{
    public void Configure(EntityTypeBuilder<UserCompanyAccess> builder)
    {
        builder.ToTable("UserCompanyAccess", "sa");
        builder.HasKey(uca => new { uca.UserId, uca.CompanyId });

        builder.HasOne(uca => uca.User)
            .WithMany(u => u.CompanyAccesses)
            .HasForeignKey(uca => uca.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uca => uca.Company)
            .WithMany()
            .HasForeignKey(uca => uca.CompanyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
