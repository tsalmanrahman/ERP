using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Domain.Entities.AccountsReceivable;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Infrastructure.Data;
using Xunit;
using ERP.Common.Infrastructure.Services.Finance;

namespace ERP.UnitTests;

public class AccountServiceTests
{
    private sealed class FakeUserContext : ICurrentUserContext
    {
        public string? UserId { get; set; } = "1";
        public string? UserName { get; set; } = "tester@erp.local";
        public int? CompanyId { get; set; } = 7;
        public bool IsAuthenticated => true;
        public bool HasAccessToCompany(int companyId) => CompanyId == companyId;
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc);
    }

    private static (AccountService Service, ErpDbContext Db, FakeUserContext User) Create(string dbName)
    {
        var user = new FakeUserContext();
        var clock = new FixedClock();
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new ErpDbContext(options, clock, user);
        return (new AccountService(db, user, clock), db, user);
    }

    [Fact]
    public async Task CreateAccount_UsesCurrentCompanyContext_NotAHardCodedCompany()
    {
        var (service, db, _) = Create(nameof(CreateAccount_UsesCurrentCompanyContext_NotAHardCodedCompany));

        var dto = await service.CreateAccountAsync(new CreateAccountRequest { AccountCode = " 1010 ", AccountName = " Cash " });

        Assert.Equal("1010", dto.AccountCode);
        Assert.Equal("Cash", dto.AccountName);
        var stored = await db.Accounts.SingleAsync();
        Assert.Equal(7, stored.CompanyId);
    }

    [Fact]
    public async Task CreateAccount_WithoutCompanyContext_IsRejected()
    {
        var (service, _, user) = Create(nameof(CreateAccount_WithoutCompanyContext_IsRejected));
        user.CompanyId = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAccountAsync(new CreateAccountRequest { AccountCode = "1", AccountName = "X" }));
    }

    [Theory]
    [InlineData("", "Name")]
    [InlineData("1000", " ")]
    public async Task CreateAccount_RequiresCodeAndName(string code, string name)
    {
        var (service, _, _) = Create("required-" + code + name);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAccountAsync(new CreateAccountRequest { AccountCode = code, AccountName = name }));
    }

    [Fact]
    public async Task CreateAccount_RejectsDuplicateCodeWithinSameCompany()
    {
        var (service, _, _) = Create(nameof(CreateAccount_RejectsDuplicateCodeWithinSameCompany));
        await service.CreateAccountAsync(new CreateAccountRequest { AccountCode = "2000", AccountName = "Payables" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAccountAsync(new CreateAccountRequest { AccountCode = "2000", AccountName = "Again" }));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task DeleteAccount_SoftDeletes_AndRestoreBringsItBack()
    {
        var (service, db, _) = Create(nameof(DeleteAccount_SoftDeletes_AndRestoreBringsItBack));
        var dto = await service.CreateAccountAsync(new CreateAccountRequest { AccountCode = "3000", AccountName = "Equity" });

        await service.DeleteAccountAsync(dto.Id);

        Assert.Empty(await db.Accounts.ToListAsync());
        Assert.True((await db.Accounts.IgnoreQueryFilters().SingleAsync()).IsDeleted);

        await service.RestoreAccountAsync(dto.Id);

        var restored = await db.Accounts.SingleAsync();
        Assert.False(restored.IsDeleted);
        Assert.Equal("tester@erp.local", restored.ModifiedBy);
    }

    [Fact]
    public async Task DeleteAccount_BlockedWhenUsedAsSubledgerControlAccount()
    {
        var (service, db, _) = Create(nameof(DeleteAccount_BlockedWhenUsedAsSubledgerControlAccount));
        var dto = await service.CreateAccountAsync(new CreateAccountRequest { AccountCode = "1200", AccountName = "AR Control" });
        db.Customers.Add(new Customer
        {
            CompanyId = 7,
            CustomerCode = "CUST-1",
            CustomerName = "Acme",
            GlControlAccountId = dto.Id
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAccountAsync(dto.Id));
        Assert.Contains("subledger control", ex.Message);
        Assert.Single(await db.Accounts.ToListAsync());
    }

    [Fact]
    public async Task DeleteAndRestore_ThrowKeyNotFound_ForUnknownAccount()
    {
        var (service, _, _) = Create(nameof(DeleteAndRestore_ThrowKeyNotFound_ForUnknownAccount));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAccountAsync(999));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RestoreAccountAsync(999));
    }
}
