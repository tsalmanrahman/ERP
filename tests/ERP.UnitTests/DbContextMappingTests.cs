using Microsoft.EntityFrameworkCore;
using ERP.Common.Infrastructure.Data;
using ERP.Common.Domain.Entities.SystemAdmin;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Domain.Entities.AccountsReceivable;
using ERP.Common.Domain.Entities.AccountsPayable;
using ERP.Common.Domain.Entities.Inventory;
using Xunit;

namespace ERP.UnitTests;

public class DbContextMappingTests
{
    [Fact]
    public void ErpDbContext_ShouldMapEntitiesToCorrectSchemas()
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlServer("Server=dummy;Database=ERP;TrustServerCertificate=True;")
            .Options;

        using var context = new ErpDbContext(options);
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

        var roleEntity = model.FindEntityType(typeof(Role));
        Assert.NotNull(roleEntity);
        Assert.Equal("Roles", roleEntity.GetTableName());
        Assert.Equal("sa", roleEntity.GetSchema());

        var userRoleEntity = model.FindEntityType(typeof(UserRole));
        Assert.NotNull(userRoleEntity);
        Assert.Equal("UserRoles", userRoleEntity.GetTableName());
        Assert.Equal("sa", userRoleEntity.GetSchema());

        var companyAccessEntity = model.FindEntityType(typeof(UserCompanyAccess));
        Assert.NotNull(companyAccessEntity);
        Assert.Equal("UserCompanyAccess", companyAccessEntity.GetTableName());
        Assert.Equal("sa", companyAccessEntity.GetSchema());
    }
}
