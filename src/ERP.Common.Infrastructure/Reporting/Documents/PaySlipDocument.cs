using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERP.Common.Application.Models.Payroll;
using ERP.Common.Infrastructure.Reporting.Models;

namespace ERP.Common.Infrastructure.Reporting.Documents;

public class PaySlipDocument : IDocument
{
    private readonly PaySlipDto _slip;
    private readonly CompanyReportHeaderModel _company;

    public PaySlipDocument(PaySlipDto slip, CompanyReportHeaderModel company)
    {
        _slip = slip;
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
                        c.Item().Text($"HR Helpline: {_company.Phone} | Email: hr@erp.local").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(200).Column(c =>
                {
                    c.Item().Background(Colors.Blue.Darken3).Padding(6).AlignCenter().Text("SALARY PAY SLIP").FontSize(13).Bold().FontColor(Colors.White);
                    c.Item().PaddingTop(3).AlignCenter().Text($"PAY PERIOD: {_slip.PeriodName.ToUpper()}").FontSize(8).Bold().FontColor(Colors.Blue.Darken4);
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(10).Column(col =>
        {
            // Employee Profile & Bank Details Card
            col.Item().Background(Colors.Grey.Lighten5).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Row(row =>
            {
                // Left Col: Employee Details
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => { t.Span("Employee ID: ").Bold(); t.Span(_slip.EmployeeCode).Bold().FontColor(Colors.Blue.Darken3); });
                    c.Item().Text(t => { t.Span("Full Name: ").Bold(); t.Span(_slip.EmployeeName).Bold(); });
                    c.Item().Text(t => { t.Span("Designation: ").Bold(); t.Span(_slip.DesignationTitle); });
                    c.Item().Text(t => { t.Span("Department: ").Bold(); t.Span(_slip.DepartmentName); });
                });

                // Right Col: Payroll & Bank Details
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => { t.Span("Joining Date: ").Bold(); t.Span(_slip.JoiningDate.ToString("yyyy-MM-dd")); });
                    c.Item().Text(t => { t.Span("Bank Name: ").Bold(); t.Span(_slip.BankName ?? "Direct Bank Transfer"); });
                    c.Item().Text(t => { t.Span("A/C Number: ").Bold(); t.Span(_slip.BankAccountNumber ?? "N/A"); });
                    c.Item().Text(t => { t.Span("Disbursed Date: ").Bold(); t.Span(_slip.ProcessingDate.ToString("yyyy-MM-dd")); });
                });
            });

            // Attendance & Working Metrics Grid
            col.Item().PaddingTop(8).Background(Colors.Blue.Lighten5).Border(1).BorderColor(Colors.Blue.Lighten3).Padding(6).Row(row =>
            {
                row.RelativeItem().AlignCenter().Text(t => { t.Span("Calendar Days: ").FontSize(8); t.Span($"{_slip.WorkingDays}").Bold().FontSize(8.5f); });
                row.RelativeItem().AlignCenter().Text(t => { t.Span("Days Present: ").FontSize(8); t.Span($"{_slip.PresentDays}").Bold().FontSize(8.5f).FontColor(Colors.Green.Darken2); });
                row.RelativeItem().AlignCenter().Text(t => { t.Span("Days Late: ").FontSize(8); t.Span($"{_slip.LateDays}").Bold().FontSize(8.5f).FontColor(Colors.Amber.Darken3); });
                row.RelativeItem().AlignCenter().Text(t => { t.Span("Days Absent: ").FontSize(8); t.Span($"{_slip.AbsentDays}").Bold().FontSize(8.5f).FontColor(Colors.Red.Darken2); });
                row.RelativeItem().AlignCenter().Text(t => { t.Span("Overtime: ").FontSize(8); t.Span($"{_slip.OvertimeHours:F1} hrs").Bold().FontSize(8.5f); });
            });

            // Earnings vs Deductions Breakdown (Side-by-Side Table)
            col.Item().PaddingTop(12).Row(row =>
            {
                // Left Column: Earnings
                row.RelativeItem().Column(e =>
                {
                    e.Item().Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text($"EARNINGS & ALLOWANCES ({_company.Currency})").Bold().FontSize(8.5f);

                    AddRow(e, "Basic Salary", _slip.BasicSalary);
                    AddRow(e, "House Rent Allowance", _slip.HouseRent);
                    AddRow(e, "Medical Allowance", _slip.MedicalAllowance);
                    AddRow(e, "Conveyance Allowance", _slip.ConveyanceAllowance);
                    if (_slip.OtherAllowance > 0)
                        AddRow(e, "Other Allowance", _slip.OtherAllowance);
                    if (_slip.OvertimeAmount > 0)
                        AddRow(e, $"Overtime ({_slip.OvertimeHours:F1}h @ {_slip.OvertimeRate:F2})", _slip.OvertimeAmount);

                    e.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                    e.Item().Padding(4).Row(r =>
                    {
                        r.RelativeItem().Text("Total Gross Earnings:").Bold();
                        r.RelativeItem().AlignRight().Text(_slip.TotalGrossEarnings.ToString("#,##0.00")).Bold().FontColor(Colors.Blue.Darken3);
                    });
                });

                row.ConstantItem(15);

                // Right Column: Deductions
                row.RelativeItem().Column(d =>
                {
                    d.Item().Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text($"STATUTORY DEDUCTIONS ({_company.Currency})").Bold().FontSize(8.5f);

                    AddRow(d, "Absenteeism Deduction", _slip.AbsentDeduction, Colors.Red.Medium);
                    AddRow(d, "Advance Income Tax (AIT)", _slip.TaxDeduction);
                    AddRow(d, "Provident Fund (PF)", _slip.ProvidentFundDeduction);

                    d.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                    d.Item().Padding(4).Row(r =>
                    {
                        r.RelativeItem().Text("Total Deductions:").Bold();
                        r.RelativeItem().AlignRight().Text(_slip.TotalDeductions.ToString("#,##0.00")).Bold().FontColor(Colors.Red.Darken2);
                    });
                });

                static void AddRow(ColumnDescriptor colDesc, string label, decimal amount, string? color = null)
                {
                    colDesc.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Row(r =>
                    {
                        r.RelativeItem().Text(label).FontSize(8.5f);
                        var text = r.RelativeItem().AlignRight().Text(amount.ToString("#,##0.00")).FontSize(8.5f);
                        if (color != null) text.FontColor(color);
                    });
                }
            });

            // Net Payable Card
            col.Item().PaddingTop(12).Background(Colors.Green.Lighten5).Border(1.5f).BorderColor(Colors.Green.Darken2).Padding(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("NET TAKE-HOME SALARY PAYABLE:").FontSize(10).Bold().FontColor(Colors.Green.Darken3);
                    c.Item().Text(NumberToWordsConverter.ConvertToWords(_slip.NetSalaryPayable, _company.Currency)).FontSize(8).Italic().FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(150).AlignRight().AlignMiddle().Text($"{_slip.NetSalaryPayable:#,##0.00} {_company.Currency}")
                    .FontSize(15).Bold().FontColor(Colors.Green.Darken3);
            });

            // Confidentiality Notice
            col.Item().PaddingTop(10).Text("Note: This document contains confidential remuneration details strictly for the recipient. Any unauthorized disclosure is prohibited.").FontSize(7).Italic().FontColor(Colors.Grey.Darken1);

            // Signatures
            col.Item().PaddingTop(40).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text("Employee Signature").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Acknowledgment").FontSize(7).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(40);

                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text("Prepared By (HR Officer)").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Payroll Department").FontSize(7).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(40);

                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderTop(1).BorderColor(Colors.Grey.Darken2).PaddingTop(2).AlignCenter().Text("Approved By (Head of HR / CFO)").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Executive Authorization").FontSize(7).FontColor(Colors.Grey.Darken1);
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
                    text.Span("ERP Enterprise Suite 10.0 | HRIS & Payroll Division | Generated: ").FontSize(7).FontColor(Colors.Grey.Darken1);
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
