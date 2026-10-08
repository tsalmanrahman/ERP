using InteAcc.Common.Application.Models.Payroll;

namespace InteAcc.Common.Application.Abstractions.Payroll;

public interface IEmployeeService
{
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DesignationDto>> GetDesignationsAsync(CancellationToken cancellationToken = default);
    Task<DesignationDto> CreateDesignationAsync(CreateDesignationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(int? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetEmployeeByCodeAsync(string employeeCode, CancellationToken cancellationToken = default);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
}
