using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Application.Abstractions.Data;
using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Infrastructure.Data;

public class InteAccDbContext : DbContext, IApplicationDbContext
{
    private readonly IDateTimeProvider? _dateTimeProvider;
    private readonly ICurrentUserContext? _currentUserContext;

    public InteAccDbContext(
        DbContextOptions<InteAccDbContext> options,
        IDateTimeProvider? dateTimeProvider = null,
        ICurrentUserContext? currentUserContext = null)
        : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
        _currentUserContext = currentUserContext;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InteAccDbContext).Assembly);
    }

    private void ApplyAuditInformation()
    {
        var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
        var currentUser = _currentUserContext?.UserName ?? "System";

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedBy = currentUser;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = now;
                entry.Entity.ModifiedBy = currentUser;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = now;
                entry.Entity.DeletedBy = currentUser;
            }
        }
    }
}
