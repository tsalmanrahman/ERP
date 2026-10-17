namespace ERP.Common.Application.Abstractions.Reports;

public interface IPdfReportingService
{
    Task<byte[]> GenerateJournalVoucherPdfAsync(int voucherId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateSalesInvoicePdfAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateDeliveryChallanPdfAsync(int challanId, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePaySlipPdfAsync(int salarySheetId, int employeeId, CancellationToken cancellationToken = default);
}
