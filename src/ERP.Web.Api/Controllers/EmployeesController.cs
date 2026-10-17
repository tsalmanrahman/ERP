using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Payroll;
using ERP.Common.Application.Models.Payroll;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet("api/hr/departments")]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
    {
        var list = await _employeeService.GetDepartmentsAsync(cancellationToken);
        return Ok(new { success = true, data = list });
    }

    [HttpPost("api/hr/departments")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var dept = await _employeeService.CreateDepartmentAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Department {dept.DepartmentName} created successfully.", data = dept });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("api/hr/designations")]
    public async Task<IActionResult> GetDesignations(CancellationToken cancellationToken)
    {
        var list = await _employeeService.GetDesignationsAsync(cancellationToken);
        return Ok(new { success = true, data = list });
    }

    [HttpPost("api/hr/designations")]
    public async Task<IActionResult> CreateDesignation([FromBody] CreateDesignationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var desig = await _employeeService.CreateDesignationAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Designation {desig.DesignationTitle} created successfully.", data = desig });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("api/hr/employees")]
    public async Task<IActionResult> GetEmployees([FromQuery] int? departmentId, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var list = await _employeeService.GetEmployeesAsync(departmentId, isActive, cancellationToken);
        return Ok(new { success = true, data = list });
    }

    [HttpGet("api/hr/employees/{id:int}")]
    public async Task<IActionResult> GetEmployeeById(int id, CancellationToken cancellationToken)
    {
        var emp = await _employeeService.GetEmployeeByIdAsync(id, cancellationToken);
        if (emp == null)
        {
            return NotFound(new { success = false, message = $"Employee with ID {id} was not found." });
        }
        return Ok(new { success = true, data = emp });
    }

    [HttpPost("api/hr/employees")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var emp = await _employeeService.CreateEmployeeAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Employee {emp.FullName} ({emp.EmployeeCode}) registered successfully.", data = emp });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("api/hr/employees/{id:int}")]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var emp = await _employeeService.UpdateEmployeeAsync(id, request, cancellationToken);
            return Ok(new { success = true, message = $"Employee {emp.FullName} updated successfully.", data = emp });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
