using ERP.Common.Application.Models.Payroll;

namespace ERP.Common.Application.Abstractions.Payroll;

public interface IPayrollReportingService
{
    Task<PaySlipDto?> GetPaySlipAsync(int salarySheetId, int employeeId, CancellationToken cancellationToken = default);
    Task<BankAdviceDto?> GetBankAdviceAsync(int salarySheetId, CancellationToken cancellationToken = default);
}
