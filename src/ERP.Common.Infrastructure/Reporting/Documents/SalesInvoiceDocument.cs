using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERP.Common.Application.Models.OrderToCash;
using ERP.Common.Infrastructure.Reporting.Models;

namespace ERP.Common.Infrastructure.Reporting.Documents;

public class SalesInvoiceDocument : IDocument
{
    private readonly InvoiceDto _invoice;
    private readonly CompanyReportHeaderModel _company;

    public SalesInvoiceDocument(InvoiceDto invoice, CompanyReportHeaderModel company)
    {
        _invoice = invoice;
        _company = company;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(25);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken4));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            // Top Header: Company on Left, Invoice Title on Right
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(_company.CompanyName).FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                    var addr = string.Join(", ", new[] { _company.Address, _company.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrEmpty(addr))
                        c.Item().Text(addr).FontSize(8).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrEmpty(_company.TaxOrBin))
                        c.Item().Text($"BIN / Tax ID: {_company.TaxOrBin}").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrEmpty(_company.Phone) || !string.IsNullOrEmpty(_company.Email))
                        c.Item().Text($"Tel: {_company.Phone ?? "N/A"} | Email: {_company.Email ?? "billing@erp.local"}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(180).Column(c =>
                {
                    c.Item().Background(Colors.Blue.Darken3).Padding(6).AlignCenter().Text("TAX INVOICE").FontSize(14).Bold().FontColor(Colors.White);
                    c.Item().PaddingTop(4).AlignRight().Text(text =>
                    {
                        text.Span("Status: ").Bold();
                        text.Span(_invoice.Status.ToString().ToUpper()).Bold().FontColor(_invoice.Status.ToString() == "Paid" ? Colors.Green.Darken2 : Colors.Amber.Darken3);
                    });
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            // Invoice & Customer Info Box
            col.Item().PaddingTop(8).Row(row =>
            {
                // Bill To Customer
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("BILL TO:").FontSize(8).Bold().FontColor(Colors.Blue.Darken3);
                    c.Item().Text(_invoice.CustomerName).FontSize(11).Bold();
                    c.Item().Text($"Customer Code: {_invoice.CustomerCode}").FontSize(8).FontColor(Colors.Grey.Darken2);
                });

                // Invoice Details
                row.RelativeItem().Column(c =>
                {
                    c.Item().AlignRight().Text(t => { t.Span("Invoice No: ").Bold(); t.Span(_invoice.InvoiceNumber).Bold(); });
                    c.Item().AlignRight().Text(t => { t.Span("Invoice Date: ").Bold(); t.Span(_invoice.InvoiceDate.ToString("yyyy-MM-dd")); });
                    if (_invoice.DueDate.HasValue)
                        c.Item().AlignRight().Text(t => { t.Span("Due Date: ").Bold(); t.Span(_invoice.DueDate.Value.ToString("yyyy-MM-dd")); });
                    if (!string.IsNullOrEmpty(_invoice.OrderNumber))
                        c.Item().AlignRight().Text(t => { t.Span("Sales Order: ").Bold(); t.Span(_invoice.OrderNumber); });
                    if (!string.IsNullOrEmpty(_invoice.ChallanNumber))
                        c.Item().AlignRight().Text(t => { t.Span("Challan Ref: ").Bold(); t.Span(_invoice.ChallanNumber); });
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(10).Column(col =>
        {
            // Items Table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);  // #
                    columns.ConstantColumn(75);  // Item Code
                    columns.RelativeColumn(4);   // Description
                    columns.RelativeColumn(1.5f);// Qty
                    columns.RelativeColumn(2);   // Unit Price
                    columns.RelativeColumn(1.5f);// Tax
                    columns.RelativeColumn(2);   // Total
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("#");
                    header.Cell().Element(HeaderStyle).Text("Item Code");
                    header.Cell().Element(HeaderStyle).Text("Description");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Qty");
                    header.Cell().Element(HeaderStyle).AlignRight().Text($"Rate ({_company.Currency})");
                    header.Cell().Element(HeaderStyle).AlignRight().Text($"VAT ({_company.Currency})");
                    header.Cell().Element(HeaderStyle).AlignRight().Text($"Amount ({_company.Currency})");

                    static IContainer HeaderStyle(IContainer c) =>
                        c.Background(Colors.Grey.Lighten3)
                         .BorderBottom(1)
                         .BorderColor(Colors.Grey.Darken1)
                         .PaddingVertical(4)
                         .PaddingHorizontal(4)
                         .DefaultTextStyle(t => t.Bold().FontSize(8));
                });

                int index = 1;
                foreach (var line in _invoice.Lines)
                {
                    var isEven = index % 2 == 0;
                    var bg = isEven ? Colors.Grey.Lighten5 : Colors.White;

                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(2).AlignCenter().Text(index.ToString());
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).Text(line.ProductCode ?? "-");
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).Text(line.ProductName ?? line.Description);
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).AlignRight().Text(line.Quantity.ToString("#,##0.##"));
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).AlignRight().Text(line.UnitPrice.ToString("#,##0.00"));
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).AlignRight().Text(line.TaxAmount.ToString("#,##0.00"));
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).AlignRight().Text(line.LineAmount.ToString("#,##0.00")).Bold();

                    index++;
                }
            });

            // Financial Summary & Calculations
            col.Item().PaddingTop(8).Row(row =>
            {
                // Left: Amount in Words & Bank details
                row.RelativeItem(6).Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("Amount in Words: ").Bold();
                        t.Span(NumberToWordsConverter.ConvertToWords(_invoice.TotalAmount, _company.Currency)).Italic();
                    });

                    c.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(6).Column(b =>
                    {
                        b.Item().Text("Payment Instructions:").FontSize(8).Bold().FontColor(Colors.Blue.Darken3);
                        b.Item().Text($"Bank: Standard Chartered Bank / Dutch-Bangla Bank | A/C: 108-2026-7890").FontSize(7.5f);
                        b.Item().Text($"Payee: {_company.CompanyName}").FontSize(7.5f);
                    });
                });

                row.ConstantItem(20);

                // Right: Financial Summary Totals
                row.RelativeItem(4).Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Sub Total:").FontSize(8.5f);
                        r.RelativeItem().AlignRight().Text(_invoice.SubTotal.ToString("#,##0.00")).FontSize(8.5f);
                    });

                    if (_invoice.DiscountAmount > 0)
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Discount:").FontSize(8.5f).FontColor(Colors.Red.Medium);
                            r.RelativeItem().AlignRight().Text($"-{_invoice.DiscountAmount:#,##0.00}").FontSize(8.5f).FontColor(Colors.Red.Medium);
                        });
                    }

                    if (_invoice.TaxAmount > 0)
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("VAT / Taxes:").FontSize(8.5f);
                            r.RelativeItem().AlignRight().Text(_invoice.TaxAmount.ToString("#,##0.00")).FontSize(8.5f);
                        });
                    }

                    c.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Darken1);

                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Grand Total:").FontSize(10).Bold();
                        r.RelativeItem().AlignRight().Text($"{_invoice.TotalAmount:#,##0.00} {_company.Currency}").FontSize(10).Bold().FontColor(Colors.Blue.Darken3);
                    });

                    if (_invoice.PaidAmount > 0)
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Paid Amount:").FontSize(8.5f).FontColor(Colors.Green.Darken2);
                            r.RelativeItem().AlignRight().Text($"{_invoice.PaidAmount:#,##0.00}").FontSize(8.5f).FontColor(Colors.Green.Darken2);
                        });

                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Balance Due:").FontSize(8.5f).Bold();
                            r.RelativeItem().AlignRight().Text($"{_invoice.BalanceAmount:#,##0.00} {_company.Currency}").FontSize(8.5f).Bold().FontColor(Colors.Red.Darken2);
                        });
                    }
                });
            });

            // Signatures
            col.Item().PaddingTop(45).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text("Customer Acknowledgment / Seal").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Received Goods in Good Condition").FontSize(7).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(60);

                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text($"For {_company.CompanyName}").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Authorized Signatory").FontSize(7).FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("ERP Enterprise Suite 10.0 | Computer Generated Tax Invoice | Printed: ").FontSize(7).FontColor(Colors.Grey.Darken1);
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC")).FontSize(7).FontColor(Colors.Grey.Darken1);
                });
                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ").FontSize(7);
                    x.CurrentPageNumber().FontSize(7);
                    x.Span(" of ").FontSize(7);
                    x.TotalPages().FontSize(7);
                });
            });
        });
    }
}
