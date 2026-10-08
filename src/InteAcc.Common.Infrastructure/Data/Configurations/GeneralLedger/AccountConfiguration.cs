using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteAcc.Common.Domain.Entities.GeneralLedger;

namespace InteAcc.Common.Infrastructure.Data.Configurations.GeneralLedger;

public class AccountGroupConfiguration : IEntityTypeConfiguration<AccountGroup>
{
    public void Configure(EntityTypeBuilder<AccountGroup> builder)
    {
        builder.ToTable("AccountGroups", "gl");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.GroupCode).HasMaxLength(20).IsRequired();
        builder.Property(g => g.GroupName).HasMaxLength(100).IsRequired();

        builder.HasIndex(g => new { g.CompanyId, g.GroupCode }).IsUnique();

        builder.Property(g => g.RowVersion).IsRowVersion();
        builder.HasQueryFilter(g => !g.IsDeleted);
    }
}

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts", "gl");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AccountCode).HasMaxLength(30).IsRequired();
        builder.Property(a => a.AccountName).HasMaxLength(150).IsRequired();

        builder.HasIndex(a => new { a.CompanyId, a.AccountCode }).IsUnique();

        builder.HasOne(a => a.AccountGroup)
            .WithMany()
            .HasForeignKey(a => a.AccountGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.RowVersion).IsRowVersion();
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
