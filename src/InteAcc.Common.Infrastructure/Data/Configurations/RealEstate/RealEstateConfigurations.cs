using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InteAcc.Common.Domain.Entities.RealEstate;

namespace InteAcc.Common.Infrastructure.Data.Configurations.RealEstate;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", "re");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ProjectCode).HasMaxLength(30).IsRequired();
        builder.Property(p => p.ProjectName).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Location).HasMaxLength(200).IsRequired();
        builder.Property(p => p.EstimatedCost).HasPrecision(18, 4);
        builder.Property(p => p.Status).HasMaxLength(30).IsRequired();
        builder.HasIndex(p => new { p.CompanyId, p.ProjectCode }).IsUnique();
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

public class PropertyUnitConfiguration : IEntityTypeConfiguration<PropertyUnit>
{
    public void Configure(EntityTypeBuilder<PropertyUnit> builder)
    {
        builder.ToTable("PropertyUnits", "re");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UnitNumber).HasMaxLength(30).IsRequired();
        builder.Property(u => u.UnitType).HasMaxLength(30).IsRequired();
        builder.Property(u => u.AreaSqFt).HasPrecision(10, 2);
        builder.Property(u => u.RatePerSqFt).HasPrecision(18, 4);
        builder.Property(u => u.TotalPrice).HasPrecision(18, 4);
        builder.Property(u => u.Status).HasMaxLength(30).IsRequired();

        builder.HasOne(u => u.Project)
            .WithMany(p => p.Units)
            .HasForeignKey(u => u.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => new { u.ProjectId, u.UnitNumber }).IsUnique();
        builder.Property(u => u.RowVersion).IsRowVersion();
    }
}

public class PropertyBookingConfiguration : IEntityTypeConfiguration<PropertyBooking>
{
    public void Configure(EntityTypeBuilder<PropertyBooking> builder)
    {
        builder.ToTable("PropertyBookings", "re");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BookingNumber).HasMaxLength(30).IsRequired();
        builder.Property(b => b.AgreedPrice).HasPrecision(18, 4);
        builder.Property(b => b.DownPayment).HasPrecision(18, 4);
        builder.Property(b => b.TotalPaid).HasPrecision(18, 4);
        builder.Property(b => b.Status).HasMaxLength(30).IsRequired();

        builder.HasOne(b => b.Project)
            .WithMany()
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.PropertyUnit)
            .WithMany()
            .HasForeignKey(b => b.PropertyUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Customer)
            .WithMany()
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Installments)
            .WithOne(i => i.Booking)
            .HasForeignKey(i => i.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.CompanyId, b.BookingNumber }).IsUnique();
        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}

public class BookingInstallmentConfiguration : IEntityTypeConfiguration<BookingInstallment>
{
    public void Configure(EntityTypeBuilder<BookingInstallment> builder)
    {
        builder.ToTable("BookingInstallments", "re");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.AmountDue).HasPrecision(18, 4);
        builder.Property(i => i.AmountPaid).HasPrecision(18, 4);
        builder.Property(i => i.Status).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Remarks).HasMaxLength(250);
    }
}
