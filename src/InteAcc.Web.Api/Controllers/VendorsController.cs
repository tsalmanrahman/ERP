using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.ProcureToPay;
using InteAcc.Common.Application.Models.ProcureToPay;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/ap/vendors")]
[Authorize]
public class VendorsController : ControllerBase
{
    private readonly IVendorService _vendorService;

    public VendorsController(IVendorService vendorService)
    {
        _vendorService = vendorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetVendors(CancellationToken cancellationToken)
    {
        var vendors = await _vendorService.GetVendorsAsync(cancellationToken);
        return Ok(new { success = true, data = vendors });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetVendorById(int id, CancellationToken cancellationToken)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(id, cancellationToken);
        if (vendor == null)
        {
            return NotFound(new { success = false, message = $"Vendor with ID {id} was not found." });
        }
        return Ok(new { success = true, data = vendor });
    }

    [HttpPost]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var vendor = await _vendorService.CreateVendorAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetVendorById), new { id = vendor.Id }, new { success = true, data = vendor });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GetVendorGroups(CancellationToken cancellationToken)
    {
        var groups = await _vendorService.GetVendorGroupsAsync(cancellationToken);
        return Ok(new { success = true, data = groups });
    }
}
