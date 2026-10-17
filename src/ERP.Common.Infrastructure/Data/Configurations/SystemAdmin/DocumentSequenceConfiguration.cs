using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.Common.Domain.Entities.SystemAdmin;

namespace ERP.Common.Infrastructure.Data.Configurations.SystemAdmin;

public class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.ToTable("DocumentSequences", "sa");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.SysGuid)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.HasIndex(d => d.SysGuid)
            .IsUnique();

        builder.Property(d => d.DocumentType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Prefix)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.FiscalYear)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(d => d.Suffix)
            .HasMaxLength(10);

        builder.Property(d => d.CreatedBy).HasMaxLength(50);
        builder.Property(d => d.ModifiedBy).HasMaxLength(50);
        builder.Property(d => d.DeletedBy).HasMaxLength(50);

        builder.HasIndex(d => new { d.CompanyId, d.DocumentType, d.Prefix, d.FiscalYear })
            .IsUnique();
    }
}
