using ERP.Common.Domain.Entities.GeneralLedger;
using Xunit;

namespace ERP.UnitTests;

public class JournalVoucherDoubleEntryTests
{
    [Fact]
    public void JournalVoucher_WhenDebitsEqualCredits_ShouldBeBalanced()
    {
        var voucher = new JournalVoucher
        {
            VoucherNumber = "JV-TEST-001",
            VoucherDate = new DateOnly(2026, 10, 8),
            CompanyId = 1
        };

        voucher.Lines.Add(new JournalVoucherLine
        {
            AccountId = 1,
            Debit = 50000m,
            Credit = 0m,
            CurrencyCode = "BDT"
        });

        voucher.Lines.Add(new JournalVoucherLine
        {
            AccountId = 2,
            Debit = 0m,
            Credit = 50000m,
            CurrencyCode = "BDT"
        });

        Assert.Equal(50000m, voucher.TotalDebit);
        Assert.Equal(50000m, voucher.TotalCredit);
        Assert.True(voucher.IsBalanced);
    }

    [Fact]
    public void JournalVoucher_WhenDebitsDoNotEqualCredits_ShouldNotBeBalanced()
    {
        var voucher = new JournalVoucher
        {
            VoucherNumber = "JV-TEST-002",
            VoucherDate = new DateOnly(2026, 10, 8),
            CompanyId = 1
        };

        voucher.Lines.Add(new JournalVoucherLine
        {
            AccountId = 1,
            Debit = 50000m,
            Credit = 0m
        });

        voucher.Lines.Add(new JournalVoucherLine
        {
            AccountId = 2,
            Debit = 0m,
            Credit = 49000m
        });

        Assert.Equal(50000m, voucher.TotalDebit);
        Assert.Equal(49000m, voucher.TotalCredit);
        Assert.False(voucher.IsBalanced);
    }

    [Fact]
    public void JournalVoucher_MultiLeg_ShouldCalculateTotalsCorrectly()
    {
        var voucher = new JournalVoucher
        {
            VoucherNumber = "JV-TEST-003",
            VoucherDate = new DateOnly(2026, 10, 8),
            CompanyId = 1
        };

        // Compound entry: Office Rent (30,000) + Utilities (10,000) paid via Bank (40,000)
        voucher.Lines.Add(new JournalVoucherLine { AccountId = 1, Debit = 30000m, Credit = 0m });
        voucher.Lines.Add(new JournalVoucherLine { AccountId = 2, Debit = 10000m, Credit = 0m });
        voucher.Lines.Add(new JournalVoucherLine { AccountId = 3, Debit = 0m, Credit = 40000m });

        Assert.Equal(40000m, voucher.TotalDebit);
        Assert.Equal(40000m, voucher.TotalCredit);
        Assert.True(voucher.IsBalanced);
    }
}
