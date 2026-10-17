using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.Common.Domain.Entities.SystemAdmin;

namespace ERP.Common.Infrastructure.Data.Configurations.SystemAdmin;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "sa");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.SysGuid)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.HasIndex(a => a.SysGuid)
            .IsUnique();

        builder.Property(a => a.EntityName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.RecordIdentifier)
            .HasMaxLength(100);

        builder.Property(a => a.Action)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.UserId)
            .HasMaxLength(50);

        builder.Property(a => a.UserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Source)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(a => new { a.EntityName, a.EntitySysGuid });
        builder.HasIndex(a => new { a.CompanyId, a.TimestampUtc });
    }
}
