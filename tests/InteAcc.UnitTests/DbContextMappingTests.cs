using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Infrastructure.Data;
using InteAcc.Common.Domain.Entities.SystemAdmin;
using InteAcc.Common.Domain.Entities.GeneralLedger;
using InteAcc.Common.Domain.Entities.AccountsReceivable;
using InteAcc.Common.Domain.Entities.AccountsPayable;
using InteAcc.Common.Domain.Entities.Inventory;
using Xunit;

namespace InteAcc.UnitTests;

public class DbContextMappingTests
{
    [Fact]
    public void InteAccDbContext_ShouldMapEntitiesToCorrectSchemas()
    {
        var options = new DbContextOptionsBuilder<InteAccDbContext>()
            .UseSqlServer("Server=dummy;Database=ERP;TrustServerCertificate=True;")
            .Options;

        using var context = new InteAccDbContext(options);
        var model = context.Model;

        var companyEntity = model.FindEntityType(typeof(Company));
        Assert.NotNull(companyEntity);
        Assert.Equal("Companies", companyEntity.GetTableName());
        Assert.Equal("sa", companyEntity.GetSchema());

        var accountEntity = model.FindEntityType(typeof(Account));
        Assert.NotNull(accountEntity);
        Assert.Equal("Accounts", accountEntity.GetTableName());
        Assert.Equal("gl", accountEntity.GetSchema());

        var voucherEntity = model.FindEntityType(typeof(JournalVoucher));
        Assert.NotNull(voucherEntity);
        Assert.Equal("JournalVouchers", voucherEntity.GetTableName());
        Assert.Equal("gl", voucherEntity.GetSchema());

        var customerEntity = model.FindEntityType(typeof(Customer));
        Assert.NotNull(customerEntity);
        Assert.Equal("Customers", customerEntity.GetTableName());
        Assert.Equal("ar", customerEntity.GetSchema());

        var vendorEntity = model.FindEntityType(typeof(Vendor));
        Assert.NotNull(vendorEntity);
        Assert.Equal("Vendors", vendorEntity.GetTableName());
        Assert.Equal("ap", vendorEntity.GetSchema());

        var productEntity = model.FindEntityType(typeof(Product));
        Assert.NotNull(productEntity);
        Assert.Equal("Products", productEntity.GetTableName());
        Assert.Equal("inv", productEntity.GetSchema());
    }
}
