namespace ERP.Common.Application.Security;

public static class Permissions
{
    // System Administration
    public const string SaCompaniesView = "Permissions.Sa.Companies.View";
    public const string SaCompaniesManage = "Permissions.Sa.Companies.Manage";
    public const string SaUsersView = "Permissions.Sa.Users.View";
    public const string SaUsersManage = "Permissions.Sa.Users.Manage";
    public const string SaRolesManage = "Permissions.Sa.Roles.Manage";

    // General Ledger
    public const string GlAccountsView = "Permissions.Gl.Accounts.View";
    public const string GlAccountsManage = "Permissions.Gl.Accounts.Manage";
    public const string GlVouchersView = "Permissions.Gl.Vouchers.View";
    public const string GlVouchersCreate = "Permissions.Gl.Vouchers.Create";
    public const string GlVouchersPost = "Permissions.Gl.Vouchers.Post";

    // Bank & Cash
    public const string BcAccountsView = "Permissions.Bc.Accounts.View";
    public const string BcTransactionsManage = "Permissions.Bc.Transactions.Manage";

    // Accounts Payable
    public const string ApVendorsView = "Permissions.Ap.Vendors.View";
    public const string ApVendorsManage = "Permissions.Ap.Vendors.Manage";
    public const string ApBillsManage = "Permissions.Ap.Bills.Manage";

    // Accounts Receivable
    public const string ArCustomersView = "Permissions.Ar.Customers.View";
    public const string ArCustomersManage = "Permissions.Ar.Customers.Manage";
    public const string ArInvoicesManage = "Permissions.Ar.Invoices.Manage";

    // Inventory
    public const string InvProductsView = "Permissions.Inv.Products.View";
    public const string InvProductsManage = "Permissions.Inv.Products.Manage";
    public const string InvStockManage = "Permissions.Inv.Stock.Manage";

    // Sales & Purchasing
    public const string SalesOrdersManage = "Permissions.Sales.Orders.Manage";
    public const string PurchOrdersManage = "Permissions.Purch.Orders.Manage";

    // HR & Payroll
    public const string HrEmployeesView = "Permissions.Hr.Employees.View";
    public const string HrEmployeesManage = "Permissions.Hr.Employees.Manage";

    public static IReadOnlyList<string> All => new[]
    {
        SaCompaniesView, SaCompaniesManage, SaUsersView, SaUsersManage, SaRolesManage,
        GlAccountsView, GlAccountsManage, GlVouchersView, GlVouchersCreate, GlVouchersPost,
        BcAccountsView, BcTransactionsManage,
        ApVendorsView, ApVendorsManage, ApBillsManage,
        ArCustomersView, ArCustomersManage, ArInvoicesManage,
        InvProductsView, InvProductsManage, InvStockManage,
        SalesOrdersManage, PurchOrdersManage,
        HrEmployeesView, HrEmployeesManage
    };
}
