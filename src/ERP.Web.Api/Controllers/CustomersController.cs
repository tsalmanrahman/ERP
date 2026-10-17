using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.OrderToCash;
using ERP.Common.Application.Models.OrderToCash;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/ar/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetCustomersAsync(cancellationToken);
        return Ok(new { success = true, data = customers });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCustomerById(int id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            return NotFound(new { success = false, message = $"Customer with ID {id} was not found." });
        }
        return Ok(new { success = true, data = customer });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerService.CreateCustomerAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, new { success = true, data = customer });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GetCustomerGroups(CancellationToken cancellationToken)
    {
        var groups = await _customerService.GetCustomerGroupsAsync(cancellationToken);
        return Ok(new { success = true, data = groups });
    }

    [HttpGet("by-guid/{sysGuid:guid}")]
    public async Task<IActionResult> GetCustomerByGuid(Guid sysGuid, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetCustomerByGuidAsync(sysGuid, cancellationToken);
        if (customer == null)
        {
            return NotFound(new { success = false, message = $"Customer with GUID {sysGuid} was not found." });
        }
        return Ok(new { success = true, data = customer });
    }

    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetCustomerByCode(string code, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetCustomerByCodeAsync(code, cancellationToken);
        if (customer == null)
        {
            return NotFound(new { success = false, message = $"Customer with code '{code}' was not found." });
        }
        return Ok(new { success = true, data = customer });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDeleteCustomer(int id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _customerService.SoftDeleteCustomerAsync(id, cancellationToken);
            if (!success)
            {
                return NotFound(new { success = false, message = $"Customer with ID {id} was not found." });
            }
            return Ok(new { success = true, message = $"Customer with ID {id} was successfully soft-deleted." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> RestoreCustomer(int id, CancellationToken cancellationToken)
    {
        var success = await _customerService.RestoreCustomerAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { success = false, message = $"Soft-deleted customer with ID {id} was not found." });
        }
        return Ok(new { success = true, message = $"Customer with ID {id} was successfully restored." });
    }
}
