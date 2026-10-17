using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Abstractions.OrderToCash;
using ERP.Common.Application.Abstractions.Payroll;
using ERP.Common.Application.Abstractions.Reports;
using ERP.Common.Infrastructure.Data;
using ERP.Common.Infrastructure.Reporting.Documents;
using ERP.Common.Infrastructure.Reporting.Models;

namespace ERP.Common.Infrastructure.Services.Reports;

public class PdfReportingService : IPdfReportingService
{
    private readonly ErpDbContext _dbContext;
    private readonly IJournalVoucherService _journalVoucherService;
    private readonly IInvoiceService _invoiceService;
    private readonly IDeliveryChallanService _deliveryChallanService;
    private readonly IPayrollReportingService _payrollReportingService;
    private readonly ICurrentUserContext _currentUserContext;

    static PdfReportingService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public PdfReportingService(
        ErpDbContext dbContext,
        IJournalVoucherService journalVoucherService,
        IInvoiceService invoiceService,
        IDeliveryChallanService deliveryChallanService,
        IPayrollReportingService payrollReportingService,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _journalVoucherService = journalVoucherService;
        _invoiceService = invoiceService;
        _deliveryChallanService = deliveryChallanService;
        _payrollReportingService = payrollReportingService;
        _currentUserContext = currentUserContext;
    }

    private async Task<CompanyReportHeaderModel> GetCompanyHeaderAsync(int? companyId, CancellationToken cancellationToken)
    {
        var targetCompanyId = companyId ?? _currentUserContext.CompanyId ?? 1;
        var company = await _dbContext.Companies.FirstOrDefaultAsync(c => c.Id == targetCompanyId, cancellationToken);

        if (company != null)
        {
            return new CompanyReportHeaderModel(
                CompanyName: company.CompanyName,
                Address: company.AddressLine1,
                City: company.City,
                Phone: company.Phone1,
                Email: company.Email,
                TaxOrBin: "BIN-002918274-0101", // Standard enterprise BIN
                Currency: company.BaseCurrency ?? "BDT"
            );
        }

        return new CompanyReportHeaderModel(
            CompanyName: "ERP Global Enterprise Ltd.",
            Address: "Gulshan Commercial Area",
            City: "Dhaka",
            Phone: "+880-2-9876543",
            Email: "info@erp.local",
            TaxOrBin: "BIN-002918274-0101",
            Currency: "BDT"
        );
    }

    public async Task<byte[]> GenerateJournalVoucherPdfAsync(int voucherId, CancellationToken cancellationToken = default)
    {
        var voucherResult = await _journalVoucherService.GetVoucherByIdAsync(voucherId, cancellationToken);
        if (voucherResult.IsFailure || voucherResult.Value == null)
        {
            throw new KeyNotFoundException($"Journal Voucher with ID {voucherId} was not found.");
        }

        var voucher = voucherResult.Value;
        var company = await GetCompanyHeaderAsync(voucher.CompanyId, cancellationToken);

        var document = new JournalVoucherDocument(voucher, company);
        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateSalesInvoicePdfAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId, cancellationToken);
        if (invoice == null)
        {
            throw new KeyNotFoundException($"Sales Invoice with ID {invoiceId} was not found.");
        }

        var company = await GetCompanyHeaderAsync(null, cancellationToken);
        var document = new SalesInvoiceDocument(invoice, company);
        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateDeliveryChallanPdfAsync(int challanId, CancellationToken cancellationToken = default)
    {
        var challan = await _deliveryChallanService.GetDeliveryChallanByIdAsync(challanId, cancellationToken);
        if (challan == null)
        {
            throw new KeyNotFoundException($"Delivery Challan with ID {challanId} was not found.");
        }

        var company = await GetCompanyHeaderAsync(null, cancellationToken);
        var document = new DeliveryChallanDocument(challan, company);
        return document.GeneratePdf();
    }

    public async Task<byte[]> GeneratePaySlipPdfAsync(int salarySheetId, int employeeId, CancellationToken cancellationToken = default)
    {
        var slip = await _payrollReportingService.GetPaySlipAsync(salarySheetId, employeeId, cancellationToken);
        if (slip == null)
        {
            throw new KeyNotFoundException($"Pay Slip for employee {employeeId} in salary sheet {salarySheetId} was not found.");
        }

        var company = await GetCompanyHeaderAsync(null, cancellationToken);
        var document = new PaySlipDocument(slip, company);
        return document.GeneratePdf();
    }
}
