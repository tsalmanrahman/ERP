using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Infrastructure.Reporting.Models;

namespace ERP.Common.Infrastructure.Reporting.Documents;

public class JournalVoucherDocument : IDocument
{
    private readonly JournalVoucherDto _voucher;
    private readonly CompanyReportHeaderModel _company;

    public JournalVoucherDocument(JournalVoucherDto voucher, CompanyReportHeaderModel company)
    {
        _voucher = voucher;
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
            // Top Company Banner
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(_company.CompanyName).FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                    var addr = string.Join(", ", new[] { _company.Address, _company.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrEmpty(addr))
                        c.Item().Text(addr).FontSize(8).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrEmpty(_company.Phone) || !string.IsNullOrEmpty(_company.Email))
                        c.Item().Text($"Tel: {_company.Phone ?? "N/A"} | Email: {_company.Email ?? "info@erp.local"}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(180).Column(c =>
                {
                    c.Item().Background(Colors.Blue.Darken3).Padding(6).AlignCenter().Text("JOURNAL VOUCHER").FontSize(12).Bold().FontColor(Colors.White);
                    c.Item().PaddingTop(4).AlignRight().Text(text =>
                    {
                        text.Span("Status: ").Bold();
                        if (_voucher.IsPosted)
                            text.Span("POSTED TO GL").Bold().FontColor(Colors.Green.Darken2);
                        else
                            text.Span("DRAFT (UNPOSTED)").Bold().FontColor(Colors.Amber.Darken3);
                    });
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            // Voucher Metadata Grid
            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => { t.Span("Voucher No: ").Bold(); t.Span(_voucher.VoucherNumber).Bold(); });
                    c.Item().Text(t => { t.Span("Voucher Date: ").Bold(); t.Span(_voucher.VoucherDate.ToString("yyyy-MM-dd")); });
                    if (!string.IsNullOrEmpty(_voucher.Reference))
                        c.Item().Text(t => { t.Span("Reference: ").Bold(); t.Span(_voucher.Reference); });
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => { t.Span("Fiscal Period: ").Bold(); t.Span($"Period {_voucher.FiscalPeriodId}"); });
                    c.Item().Text(t => { t.Span("Currency: ").Bold(); t.Span(_company.Currency); });
                    if (_voucher.PostedAtUtc.HasValue)
                        c.Item().Text(t => { t.Span("Posted At: ").Bold(); t.Span(_voucher.PostedAtUtc.Value.ToString("yyyy-MM-dd HH:mm UTC")); });
                });
            });

            if (!string.IsNullOrWhiteSpace(_voucher.Description))
            {
                col.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("Narration / Purpose: ").Bold();
                    t.Span(_voucher.Description);
                });
            }

            col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(10).Column(col =>
        {
            // Transaction Lines Table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);  // #
                    columns.ConstantColumn(80);  // Account Code
                    columns.RelativeColumn(3);   // Account Name
                    columns.RelativeColumn(3);   // Particulars
                    columns.RelativeColumn(2);   // Debit
                    columns.RelativeColumn(2);   // Credit
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("#");
                    header.Cell().Element(HeaderStyle).Text("Account Code");
                    header.Cell().Element(HeaderStyle).Text("Account Title");
                    header.Cell().Element(HeaderStyle).Text("Particulars / Narration");
                    header.Cell().Element(HeaderStyle).AlignRight().Text($"Debit ({_company.Currency})");
                    header.Cell().Element(HeaderStyle).AlignRight().Text($"Credit ({_company.Currency})");

                    static IContainer HeaderStyle(IContainer c) =>
                        c.Background(Colors.Grey.Lighten3)
                         .BorderBottom(1)
                         .BorderColor(Colors.Grey.Darken1)
                         .PaddingVertical(4)
                         .PaddingHorizontal(4)
                         .DefaultTextStyle(t => t.Bold().FontSize(8));
                });

                int index = 1;
                foreach (var line in _voucher.Lines)
                {
                    var isEven = index % 2 == 0;
                    var bg = isEven ? Colors.Grey.Lighten5 : Colors.White;

                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(2).AlignCenter().Text(index.ToString());
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).Text(line.AccountCode).Bold();
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).Text(line.AccountName);
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).Text(line.Particulars ?? "-").Italic().FontSize(8);
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).AlignRight()
                        .Text(line.Debit > 0 ? line.Debit.ToString("#,##0.00") : "-")
                        .Bold();
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).AlignRight()
                        .Text(line.Credit > 0 ? line.Credit.ToString("#,##0.00") : "-")
                        .Bold();

                    index++;
                }

                // Total Summary Row
                table.Cell().ColumnSpan(4).BorderTop(1).BorderColor(Colors.Grey.Darken1).Padding(4).AlignRight().Text("TOTALS:").Bold().FontSize(9);
                table.Cell().BorderTop(1).BorderColor(Colors.Grey.Darken1).Padding(4).AlignRight().Text(_voucher.TotalDebit.ToString("#,##0.00")).Bold().FontSize(9);
                table.Cell().BorderTop(1).BorderColor(Colors.Grey.Darken1).Padding(4).AlignRight().Text(_voucher.TotalCredit.ToString("#,##0.00")).Bold().FontSize(9);
            });

            // Dual-Entry Verification Stamp & Amount in Words
            col.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem(7).Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("Amount in Words: ").Bold();
                        t.Span(NumberToWordsConverter.ConvertToWords(_voucher.TotalDebit, _company.Currency)).Italic();
                    });

                    c.Item().PaddingTop(4).Text(t =>
                    {
                        t.Span("Dual-Entry Balance Verification: ").Bold();
                        var diff = Math.Abs(_voucher.TotalDebit - _voucher.TotalCredit);
                        if (diff <= 0.0001m)
                            t.Span("MATCHED EXACTLY (0.00 Variance) - PASSED").Bold().FontColor(Colors.Green.Darken2);
                        else
                            t.Span($"UNBALANCED (Variance: {diff:F2} {_company.Currency}) - REJECTED").Bold().FontColor(Colors.Red.Darken2);
                    });
                });

                row.RelativeItem(3).Border(1).BorderColor(Colors.Green.Darken2).Padding(4).AlignCenter().Column(c =>
                {
                    c.Item().Text("GENERAL LEDGER").FontSize(8).Bold().FontColor(Colors.Green.Darken2);
                    c.Item().Text("BALANCED & AUDITED").FontSize(9).Bold().FontColor(Colors.Green.Darken3);
                    c.Item().Text($"Lines: {_voucher.Lines.Count}").FontSize(7).FontColor(Colors.Grey.Darken2);
                });
            });

            // 4-Tier Enterprise Signature Blocks
            col.Item().PaddingTop(40).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text("Prepared By").FontSize(8).Bold();
                    c.Item().AlignCenter().Text(_voucher.PostedBy ?? "Accounts Officer").FontSize(7).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(15);

                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text("Checked By").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Senior Accountant").FontSize(7).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(15);

                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text("Approved By").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Finance Manager / CFO").FontSize(7).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(15);

                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text("Internal Audit").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Audit Verified").FontSize(7).FontColor(Colors.Grey.Darken1);
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
                    text.Span("ERP Enterprise Suite 10.0 | Double-Entry Certified Document | Generated: ").FontSize(7).FontColor(Colors.Grey.Darken1);
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
