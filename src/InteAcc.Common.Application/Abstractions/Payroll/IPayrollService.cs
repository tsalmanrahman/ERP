using InteAcc.Common.Application.Models.Payroll;

namespace InteAcc.Common.Application.Abstractions.Payroll;

public interface IPayrollService
{
    Task<IReadOnlyList<PayrollPeriodDto>> GetPayrollPeriodsAsync(CancellationToken cancellationToken = default);
    Task<PayrollPeriodDto> CreatePayrollPeriodAsync(CreatePayrollPeriodRequest request, CancellationToken cancellationToken = default);
    Task<SalarySheetDto> GenerateSalarySheetAsync(GenerateSalarySheetRequest request, CancellationToken cancellationToken = default);
    Task<SalarySheetDto?> GetSalarySheetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SalarySheetDto?> GetSalarySheetByPeriodIdAsync(int periodId, CancellationToken cancellationToken = default);
    Task<SalarySheetDto> ApproveSalarySheetAsync(int id, CancellationToken cancellationToken = default);
    Task<SalarySheetDto> DisburseSalarySheetAsync(DisburseSalaryRequest request, CancellationToken cancellationToken = default);
}
