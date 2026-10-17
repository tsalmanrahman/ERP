using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERP.Common.Application.Models.OrderToCash;
using ERP.Common.Infrastructure.Reporting.Models;

namespace ERP.Common.Infrastructure.Reporting.Documents;

public class DeliveryChallanDocument : IDocument
{
    private readonly DeliveryChallanDto _challan;
    private readonly CompanyReportHeaderModel _company;

    public DeliveryChallanDocument(DeliveryChallanDto challan, CompanyReportHeaderModel company)
    {
        _challan = challan;
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
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(_company.CompanyName).FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                    var addr = string.Join(", ", new[] { _company.Address, _company.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrEmpty(addr))
                        c.Item().Text(addr).FontSize(8).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrEmpty(_company.Phone))
                        c.Item().Text($"Tel: {_company.Phone}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(200).Column(c =>
                {
                    c.Item().Background(Colors.Indigo.Darken3).Padding(6).AlignCenter().Text("DELIVERY CHALLAN").FontSize(13).Bold().FontColor(Colors.White);
                    c.Item().PaddingTop(3).AlignCenter().Text("WAREHOUSE GATE PASS & DISPATCH").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken2);
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => { t.Span("Consignee / Customer: ").Bold(); t.Span(_challan.CustomerName).Bold(); });
                    c.Item().Text(t => { t.Span("Dispatching Warehouse: ").Bold(); t.Span(_challan.WarehouseName); });
                    if (!string.IsNullOrEmpty(_challan.Remarks))
                        c.Item().Text(t => { t.Span("Special Remarks: ").Bold(); t.Span(_challan.Remarks); });
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().AlignRight().Text(t => { t.Span("Challan No: ").Bold(); t.Span(_challan.ChallanNumber).Bold(); });
                    c.Item().AlignRight().Text(t => { t.Span("Challan Date: ").Bold(); t.Span(_challan.ChallanDate.ToString("yyyy-MM-dd")); });
                    c.Item().AlignRight().Text(t => { t.Span("Sales Order Ref: ").Bold(); t.Span(_challan.OrderNumber).Bold(); });
                    c.Item().AlignRight().Text(t => { t.Span("Vehicle Number: ").Bold(); t.Span(_challan.VehicleNumber ?? "N/A"); });
                    c.Item().AlignRight().Text(t => { t.Span("Driver Name: ").Bold(); t.Span(_challan.DriverName ?? "N/A"); });
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);  // #
                    columns.ConstantColumn(90);  // Product Code
                    columns.RelativeColumn(5);   // Product Description
                    columns.RelativeColumn(3);   // Batch / Lot #
                    columns.RelativeColumn(2.5f);// Dispatched Qty
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("#");
                    header.Cell().Element(HeaderStyle).Text("Product Code");
                    header.Cell().Element(HeaderStyle).Text("Product Description");
                    header.Cell().Element(HeaderStyle).Text("Batch / Lot No");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Dispatched Qty");

                    static IContainer HeaderStyle(IContainer c) =>
                        c.Background(Colors.Grey.Lighten3)
                         .BorderBottom(1)
                         .BorderColor(Colors.Grey.Darken1)
                         .PaddingVertical(4)
                         .PaddingHorizontal(4)
                         .DefaultTextStyle(t => t.Bold().FontSize(8));
                });

                int index = 1;
                decimal totalQty = 0;
                foreach (var line in _challan.Lines)
                {
                    var isEven = index % 2 == 0;
                    var bg = isEven ? Colors.Grey.Lighten5 : Colors.White;

                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(2).AlignCenter().Text(index.ToString());
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).Text(line.ProductCode).Bold();
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).Text(line.ProductName);
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).Text(line.BatchNumber ?? "DEFAULT-BATCH").Italic();
                    table.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).AlignRight().Text(line.DispatchedQuantity.ToString("#,##0.##")).Bold();

                    totalQty += line.DispatchedQuantity;
                    index++;
                }

                table.Cell().ColumnSpan(4).BorderTop(1).BorderColor(Colors.Grey.Darken1).Padding(4).AlignRight().Text("TOTAL UNITS DISPATCHED:").Bold().FontSize(9);
                table.Cell().BorderTop(1).BorderColor(Colors.Grey.Darken1).Padding(4).AlignRight().Text(totalQty.ToString("#,##0.##")).Bold().FontSize(9);
            });

            // Transport & Gate Pass Notes
            col.Item().PaddingTop(12).Background(Colors.Grey.Lighten4).Padding(6).Column(b =>
            {
                b.Item().Text("Store & Security Dispatch Instructions:").FontSize(8).Bold().FontColor(Colors.Indigo.Darken3);
                b.Item().Text("1. Physical stock counted and verified prior to warehouse gate exit.").FontSize(7.5f);
                b.Item().Text("2. The driver/carrier accepts full custody until physical delivery acknowledgment by the customer.").FontSize(7.5f);
                b.Item().Text("3. Any damage or discrepancy must be reported immediately on this challan before vehicle departure.").FontSize(7.5f);
            });

            // 3-Party Signatures: Store In-Charge, Driver, Customer Received
            col.Item().PaddingTop(50).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text("Store Keeper / Dispatcher").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Warehouse Department").FontSize(7).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(25);

                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text("Driver / Transporter").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Vehicle Operator").FontSize(7).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(25);

                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text("Customer Receiver").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Received with Sign & Date").FontSize(7).FontColor(Colors.Grey.Darken1);
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
                    text.Span("ERP Enterprise Suite 10.0 | Delivery Challan & Gate Pass | Printed: ").FontSize(7).FontColor(Colors.Grey.Darken1);
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
