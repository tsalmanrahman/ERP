using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Application.Abstractions.Payroll;
using InteAcc.Common.Application.Models.Payroll;
using InteAcc.Common.Domain.Entities.HumanResources;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.Common.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly InteAccDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public EmployeeService(InteAccDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var depts = await _dbContext.Departments
            .AsNoTracking()
            .Where(d => d.CompanyId == companyId)
            .OrderBy(d => d.DepartmentName)
            .Select(d => new DepartmentDto(
                d.Id,
                d.CompanyId,
                d.DepartmentCode,
                d.DepartmentName,
                d.IsActive
            ))
            .ToListAsync(cancellationToken);

        return depts;
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var code = request.DepartmentCode.Trim().ToUpperInvariant();

        var exists = await _dbContext.Departments.AnyAsync(d => d.CompanyId == companyId && d.DepartmentCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Department with code '{code}' already exists.");
        }

        var dept = new Department
        {
            CompanyId = companyId,
            DepartmentCode = code,
            DepartmentName = request.DepartmentName.Trim(),
            IsActive = true
        };

        _dbContext.Departments.Add(dept);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DepartmentDto(dept.Id, dept.CompanyId, dept.DepartmentCode, dept.DepartmentName, dept.IsActive);
    }

    public async Task<IReadOnlyList<DesignationDto>> GetDesignationsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var desigs = await _dbContext.Designations
            .AsNoTracking()
            .Where(d => d.CompanyId == companyId)
            .OrderBy(d => d.DesignationTitle)
            .Select(d => new DesignationDto(
                d.Id,
                d.CompanyId,
                d.DesignationCode,
                d.DesignationTitle,
                d.IsActive
            ))
            .ToListAsync(cancellationToken);

        return desigs;
    }

    public async Task<DesignationDto> CreateDesignationAsync(CreateDesignationRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var code = request.DesignationCode.Trim().ToUpperInvariant();

        var exists = await _dbContext.Designations.AnyAsync(d => d.CompanyId == companyId && d.DesignationCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Designation with code '{code}' already exists.");
        }

        var desig = new Designation
        {
            CompanyId = companyId,
            DesignationCode = code,
            DesignationTitle = request.DesignationTitle.Trim(),
            IsActive = true
        };

        _dbContext.Designations.Add(desig);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DesignationDto(desig.Id, desig.CompanyId, desig.DesignationCode, desig.DesignationTitle, desig.IsActive);
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(int? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from e in _dbContext.Employees.AsNoTracking()
                    where e.CompanyId == companyId
                    join d in _dbContext.Departments.AsNoTracking() on e.DepartmentId equals d.Id into deptJoin
                    from d in deptJoin.DefaultIfEmpty()
                    join des in _dbContext.Designations.AsNoTracking() on e.DesignationId equals des.Id into desigJoin
                    from des in desigJoin.DefaultIfEmpty()
                    select new { e, DeptName = d != null ? d.DepartmentName : string.Empty, DesigTitle = des != null ? des.DesignationTitle : string.Empty };

        if (departmentId.HasValue)
        {
            query = query.Where(x => x.e.DepartmentId == departmentId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.e.IsActive == isActive.Value);
        }

        var list = await query.OrderBy(x => x.e.EmployeeCode).ToListAsync(cancellationToken);

        return list.Select(x => MapToDto(x.e, x.DeptName, x.DesigTitle)).ToList();
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var item = await (from e in _dbContext.Employees.AsNoTracking()
                          where e.CompanyId == companyId && e.Id == id
                          join d in _dbContext.Departments.AsNoTracking() on e.DepartmentId equals d.Id into deptJoin
                          from d in deptJoin.DefaultIfEmpty()
                          join des in _dbContext.Designations.AsNoTracking() on e.DesignationId equals des.Id into desigJoin
                          from des in desigJoin.DefaultIfEmpty()
                          select new { e, DeptName = d != null ? d.DepartmentName : string.Empty, DesigTitle = des != null ? des.DesignationTitle : string.Empty })
                         .FirstOrDefaultAsync(cancellationToken);

        return item == null ? null : MapToDto(item.e, item.DeptName, item.DesigTitle);
    }

    public async Task<EmployeeDto?> GetEmployeeByCodeAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var code = employeeCode.Trim();
        var item = await (from e in _dbContext.Employees.AsNoTracking()
                          where e.CompanyId == companyId && e.EmployeeCode == code
                          join d in _dbContext.Departments.AsNoTracking() on e.DepartmentId equals d.Id into deptJoin
                          from d in deptJoin.DefaultIfEmpty()
                          join des in _dbContext.Designations.AsNoTracking() on e.DesignationId equals des.Id into desigJoin
                          from des in desigJoin.DefaultIfEmpty()
                          select new { e, DeptName = d != null ? d.DepartmentName : string.Empty, DesigTitle = des != null ? des.DesignationTitle : string.Empty })
                         .FirstOrDefaultAsync(cancellationToken);

        return item == null ? null : MapToDto(item.e, item.DeptName, item.DesigTitle);
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var code = request.EmployeeCode.Trim();

        var exists = await _dbContext.Employees.AnyAsync(e => e.CompanyId == companyId && e.EmployeeCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Employee with code '{code}' already exists.");
        }

        var gross = request.BasicSalary + request.HouseRent + request.MedicalAllowance + request.ConveyanceAllowance + request.OtherAllowance;

        var employee = new Employee
        {
            CompanyId = companyId,
            EmployeeCode = code,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DepartmentId = request.DepartmentId,
            DesignationId = request.DesignationId,
            DateOfBirth = request.DateOfBirth,
            JoiningDate = request.JoiningDate,
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            GrossSalary = gross,
            BasicSalary = request.BasicSalary,
            HouseRent = request.HouseRent,
            MedicalAllowance = request.MedicalAllowance,
            ConveyanceAllowance = request.ConveyanceAllowance,
            OtherAllowance = request.OtherAllowance,
            ProvidentFundDeduction = request.ProvidentFundDeduction,
            TaxDeduction = request.TaxDeduction,
            BankName = request.BankName?.Trim(),
            BankAccountNumber = request.BankAccountNumber?.Trim(),
            IsActive = true
        };

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetEmployeeByIdAsync(employee.Id, cancellationToken))!;
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.CompanyId == companyId && e.Id == id, cancellationToken);
        if (employee == null)
        {
            throw new InvalidOperationException($"Employee with ID {id} was not found.");
        }

        var gross = request.BasicSalary + request.HouseRent + request.MedicalAllowance + request.ConveyanceAllowance + request.OtherAllowance;

        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.DepartmentId = request.DepartmentId;
        employee.DesignationId = request.DesignationId;
        employee.DateOfBirth = request.DateOfBirth;
        employee.JoiningDate = request.JoiningDate;
        employee.Email = request.Email?.Trim();
        employee.Phone = request.Phone?.Trim();
        employee.GrossSalary = gross;
        employee.BasicSalary = request.BasicSalary;
        employee.HouseRent = request.HouseRent;
        employee.MedicalAllowance = request.MedicalAllowance;
        employee.ConveyanceAllowance = request.ConveyanceAllowance;
        employee.OtherAllowance = request.OtherAllowance;
        employee.ProvidentFundDeduction = request.ProvidentFundDeduction;
        employee.TaxDeduction = request.TaxDeduction;
        employee.BankName = request.BankName?.Trim();
        employee.BankAccountNumber = request.BankAccountNumber?.Trim();
        employee.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetEmployeeByIdAsync(employee.Id, cancellationToken))!;
    }

    private static EmployeeDto MapToDto(Employee e, string deptName, string desigTitle)
    {
        return new EmployeeDto(
            e.Id,
            e.CompanyId,
            e.EmployeeCode,
            e.FirstName,
            e.LastName,
            e.FullName,
            e.DepartmentId,
            deptName,
            e.DesignationId,
            desigTitle,
            e.DateOfBirth,
            e.JoiningDate,
            e.Email,
            e.Phone,
            e.GrossSalary,
            e.BasicSalary,
            e.HouseRent,
            e.MedicalAllowance,
            e.ConveyanceAllowance,
            e.OtherAllowance,
            e.ProvidentFundDeduction,
            e.TaxDeduction,
            e.BankName,
            e.BankAccountNumber,
            e.IsActive
        );
    }
}
