using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.Common.Domain.Entities.FixedAssets;

namespace ERP.Common.Infrastructure.Data.Configurations.FixedAssets;

public class AssetGroupConfiguration : IEntityTypeConfiguration<AssetGroup>
{
    public void Configure(EntityTypeBuilder<AssetGroup> builder)
    {
        builder.ToTable("AssetGroups", "fa");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.GroupCode).HasMaxLength(20).IsRequired();
        builder.Property(g => g.GroupName).HasMaxLength(100).IsRequired();
        builder.Property(g => g.DepreciationRate).HasPrecision(5, 2);
        builder.HasIndex(g => new { g.CompanyId, g.GroupCode }).IsUnique();
        builder.Property(g => g.RowVersion).IsRowVersion();
    }
}

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets", "fa");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AssetCode).HasMaxLength(30).IsRequired();
        builder.Property(a => a.AssetName).HasMaxLength(150).IsRequired();
        builder.Property(a => a.PurchaseCost).HasPrecision(18, 4);
        builder.Property(a => a.CurrentBookValue).HasPrecision(18, 4);
        builder.Property(a => a.AccumulatedDepreciation).HasPrecision(18, 4);
        builder.Property(a => a.SalvageValue).HasPrecision(18, 4);
        builder.Property(a => a.DepreciationMethod).HasMaxLength(20).IsRequired();
        builder.Property(a => a.DisposalAmount).HasPrecision(18, 4);
        builder.Property(a => a.GainLossAmount).HasPrecision(18, 4);

        builder.HasOne(a => a.AssetGroup)
            .WithMany(g => g.Assets)
            .HasForeignKey(a => a.AssetGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.CompanyId, a.AssetCode }).IsUnique();
        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}

public class DepreciationEntryConfiguration : IEntityTypeConfiguration<DepreciationEntry>
{
    public void Configure(EntityTypeBuilder<DepreciationEntry> builder)
    {
        builder.ToTable("DepreciationEntries", "fa");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DepreciationAmount).HasPrecision(18, 4);
        builder.Property(d => d.AccumulatedDepreciationAfter).HasPrecision(18, 4);
        builder.Property(d => d.BookValueAfter).HasPrecision(18, 4);

        builder.HasOne(d => d.Asset)
            .WithMany(a => a.DepreciationEntries)
            .HasForeignKey(d => d.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.AssetId, d.ScheduleDate });
    }
}
