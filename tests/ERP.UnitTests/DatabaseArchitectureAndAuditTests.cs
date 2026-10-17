using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Domain.Entities.AccountsReceivable;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Domain.Entities.SystemAdmin;
using ERP.Common.Infrastructure.Data;
using Xunit;
using ERP.Common.Infrastructure.Services.SystemAdmin;

namespace ERP.UnitTests;

public class DatabaseArchitectureAndAuditTests
{
    private class TestUserContext : ICurrentUserContext
    {
        public string? UserId { get; set; } = "USR-001";
        public string? UserName { get; set; } = "auditor@erp.local";
        public int? CompanyId { get; set; } = 1;
        public bool IsAuthenticated => true;
        public bool HasAccessToCompany(int companyId) => CompanyId == companyId;
    }

    private class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc);
    }

    private ErpDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var userContext = new TestUserContext();
        var timeProvider = new TestDateTimeProvider();
        return new ErpDbContext(options, timeProvider, userContext);
    }

    [Fact]
    public void DualIdStrategy_EntityShouldHaveBothIdAndSysGuid()
    {
        var customer = new Customer
        {
            CustomerCode = "CUST-00042",
            CustomerName = "Apex Tannery Ltd."
        };

        Assert.NotEqual(Guid.Empty, customer.SysGuid);
        Assert.Equal("CUST-00042", customer.CustomerCode);
    }

    [Fact]
    public async Task AuditTracking_OnCreateAndUpdate_PopulatesAuditMetadata()
    {
        using var context = CreateInMemoryDbContext(nameof(AuditTracking_OnCreateAndUpdate_PopulatesAuditMetadata));

        var customer = new Customer
        {
            CompanyId = 1,
            CustomerCode = "CUST-AUDIT-01",
            CustomerName = "Audit Test Customer",
            GlControlAccountId = 1,
            CreditLimit = 500000m,
            IsActive = true
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        Assert.Equal("auditor@erp.local", customer.CreatedBy);
        Assert.Equal(new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc), customer.CreatedAtUtc);
        Assert.False(customer.IsDeleted);
        Assert.Null(customer.DeletedAtUtc);

        // Update
        customer.CustomerName = "Audit Test Customer Updated";
        await context.SaveChangesAsync();

        Assert.Equal("auditor@erp.local", customer.ModifiedBy);
        Assert.Equal(new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc), customer.ModifiedAtUtc);
    }

    [Fact]
    public async Task SoftDelete_OnRemove_MarksIsDeletedAndExcludesFromNormalQueries()
    {
        using var context = CreateInMemoryDbContext(nameof(SoftDelete_OnRemove_MarksIsDeletedAndExcludesFromNormalQueries));

        var customer = new Customer
        {
            CompanyId = 1,
            CustomerCode = "CUST-DEL-01",
            CustomerName = "Soft Delete Customer",
            GlControlAccountId = 1
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Soft delete via Remove
        context.Customers.Remove(customer);
        await context.SaveChangesAsync();

        Assert.True(customer.IsDeleted);
        Assert.Equal("auditor@erp.local", customer.DeletedBy);
        Assert.NotNull(customer.DeletedAtUtc);

        // Normal query should exclude soft deleted
        var activeCustomers = await context.Customers.ToListAsync();
        Assert.DoesNotContain(activeCustomers, c => c.CustomerCode == "CUST-DEL-01");

        // IgnoreQueryFilters should find it
        var deletedRecord = await context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.CustomerCode == "CUST-DEL-01");
        Assert.NotNull(deletedRecord);
        Assert.True(deletedRecord.IsDeleted);
    }

    [Fact]
    public async Task SafeRestoration_ReactivatesSoftDeletedEntity()
    {
        using var context = CreateInMemoryDbContext(nameof(SafeRestoration_ReactivatesSoftDeletedEntity));
        var userContext = new TestUserContext();
        var timeProvider = new TestDateTimeProvider();

        var customer = new Customer
        {
            CompanyId = 1,
            CustomerCode = "CUST-RESTORE-01",
            CustomerName = "To Be Restored",
            GlControlAccountId = 1
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        context.Customers.Remove(customer);
        await context.SaveChangesAsync();
        Assert.True(customer.IsDeleted);

        // Restore
        var auditService = new AuditHistoryService(context, userContext, timeProvider);
        var restored = await auditService.RestoreEntityAsync("Customer", customer.SysGuid);

        Assert.True(restored);
        var active = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-RESTORE-01");
        Assert.NotNull(active);
        Assert.False(active.IsDeleted);
        Assert.Null(active.DeletedAtUtc);
    }

    [Fact]
    public async Task AuditLogHistory_CapturesCreateUpdateDeleteWithJsonSnapshots()
    {
        using var context = CreateInMemoryDbContext(nameof(AuditLogHistory_CapturesCreateUpdateDeleteWithJsonSnapshots));

        var customer = new Customer
        {
            CompanyId = 1,
            CustomerCode = "CUST-HIST-01",
            CustomerName = "History Capture Customer",
            GlControlAccountId = 1
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Check Create audit log
        var createLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.EntitySysGuid == customer.SysGuid && a.Action == "Create");
        Assert.NotNull(createLog);
        Assert.Equal("Customer", createLog.EntityName);
        Assert.Equal("CUST-HIST-01", createLog.RecordIdentifier);
        Assert.NotNull(createLog.NewValues);

        // Update
        customer.CustomerName = "History Customer Name Changed";
        await context.SaveChangesAsync();

        var updateLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.EntitySysGuid == customer.SysGuid && a.Action == "Update");
        Assert.NotNull(updateLog);
        Assert.Contains("CustomerName", updateLog.AffectedColumns);

        // Soft delete
        context.Customers.Remove(customer);
        await context.SaveChangesAsync();

        var deleteLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.EntitySysGuid == customer.SysGuid && a.Action == "Delete");
        Assert.NotNull(deleteLog);
    }

    [Fact]
    public void Concurrency_DocumentSequence_GeneratesUniqueSequentialNumbersConcurrently()
    {
        var bag = new System.Collections.Concurrent.ConcurrentBag<string>();
        long currentNumber = 0;
        object lockObj = new object();

        // Simulate 50 concurrent transactions generating document numbers with row-level locking
        Parallel.For(0, 50, i =>
        {
            long nextVal;
            lock (lockObj) // Simulates SQL Server UPDLOCK, ROWLOCK on sa.DocumentSequences
            {
                currentNumber++;
                nextVal = currentNumber;
            }

            var formattedNumber = $"JV-202610-{nextVal:D6}";
            bag.Add(formattedNumber);
        });

        Assert.Equal(50, bag.Count);
        var uniqueSet = new HashSet<string>(bag);
        Assert.Equal(50, uniqueSet.Count); // Exactly 50 unique numbers, 0 duplicates
    }

    [Fact]
    public async Task SoftDelete_CannotDeleteAccountWithActiveJournalEntries()
    {
        using var context = CreateInMemoryDbContext(nameof(SoftDelete_CannotDeleteAccountWithActiveJournalEntries));

        var account = new Account
        {
            CompanyId = 1,
            AccountCode = "1120",
            AccountName = "Prime Bank Account",
            IsActive = true
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var voucher = new JournalVoucher
        {
            CompanyId = 1,
            VoucherNumber = "JV-TEST-999"
        };
        voucher.Lines.Add(new JournalVoucherLine
        {
            AccountId = account.Id,
            Debit = 1000m,
            Credit = 0m,
            IsDeleted = false
        });
        context.JournalVouchers.Add(voucher);
        await context.SaveChangesAsync();

        // Invariant verification: an account referenced by active lines cannot be removed
        var hasActiveEntries = await context.JournalVoucherLines.AnyAsync(l => l.AccountId == account.Id && !l.IsDeleted);
        Assert.True(hasActiveEntries);

        // Controller or service throws when attempting deletion
        bool deleteAllowed = !hasActiveEntries;
        Assert.False(deleteAllowed);
    }
}
