using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Reports;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/reports/pdf")]
[Authorize]
public class PdfReportsController : ControllerBase
{
    private readonly IPdfReportingService _pdfReportingService;
    private readonly ILogger<PdfReportsController> _logger;

    public PdfReportsController(IPdfReportingService pdfReportingService, ILogger<PdfReportsController> logger)
    {
        _pdfReportingService = pdfReportingService;
        _logger = logger;
    }

    [HttpGet("voucher/{id:int}")]
    public Task<IActionResult> GetJournalVoucherPdf(int id, CancellationToken cancellationToken) =>
        RenderPdfAsync("voucher", $"Journal_Voucher_{id}.pdf", () => _pdfReportingService.GenerateJournalVoucherPdfAsync(id, cancellationToken));

    [HttpGet("invoice/{id:int}")]
    public Task<IActionResult> GetSalesInvoicePdf(int id, CancellationToken cancellationToken) =>
        RenderPdfAsync("invoice", $"Sales_Invoice_{id}.pdf", () => _pdfReportingService.GenerateSalesInvoicePdfAsync(id, cancellationToken));

    [HttpGet("challan/{id:int}")]
    public Task<IActionResult> GetDeliveryChallanPdf(int id, CancellationToken cancellationToken) =>
        RenderPdfAsync("challan", $"Delivery_Challan_{id}.pdf", () => _pdfReportingService.GenerateDeliveryChallanPdfAsync(id, cancellationToken));

    [HttpGet("payslip/{salarySheetId:int}/{employeeId:int}")]
    public Task<IActionResult> GetPaySlipPdf(int salarySheetId, int employeeId, CancellationToken cancellationToken) =>
        RenderPdfAsync("payslip", $"PaySlip_{salarySheetId}_{employeeId}.pdf", () => _pdfReportingService.GeneratePaySlipPdfAsync(salarySheetId, employeeId, cancellationToken));

    /// <summary>
    /// Shared PDF response pipeline: 404 for missing source documents; unexpected failures are logged
    /// server-side and returned as a generic 500 so internal exception details are never exposed to clients.
    /// </summary>
    private async Task<IActionResult> RenderPdfAsync(string reportKind, string fileName, Func<Task<byte[]>> generate)
    {
        try
        {
            var pdfBytes = await generate();
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate {ReportKind} PDF ({FileName}).", reportKind, fileName);
            return StatusCode(500, new { success = false, message = $"Failed to generate {reportKind} PDF." });
        }
    }
}
