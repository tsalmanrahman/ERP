using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.OrderToCash;
using InteAcc.Common.Application.Models.OrderToCash;

namespace InteAcc.Web.Api.Controllers;

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
}
