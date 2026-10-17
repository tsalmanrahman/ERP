using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Specialized;
using ERP.Common.Application.Models.Specialized;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/re")]
[Authorize]
public class RealEstateController : ControllerBase
{
    private readonly IRealEstateService _realEstateService;

    public RealEstateController(IRealEstateService realEstateService)
    {
        _realEstateService = realEstateService;
    }

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects(CancellationToken cancellationToken)
    {
        var list = await _realEstateService.GetProjectsAsync(cancellationToken);
        return Ok(new { success = true, data = list });
    }

    [HttpPost("projects")]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _realEstateService.CreateProjectAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Project {project.ProjectName} created successfully.", data = project });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("units")]
    public async Task<IActionResult> GetUnits([FromQuery] int? projectId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var list = await _realEstateService.GetPropertyUnitsAsync(projectId, status, cancellationToken);
        return Ok(new { success = true, data = list });
    }

    [HttpPost("units")]
    public async Task<IActionResult> CreateUnit([FromBody] CreatePropertyUnitRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var unit = await _realEstateService.CreatePropertyUnitAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Unit {unit.UnitNumber} created successfully.", data = unit });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings([FromQuery] int? projectId, [FromQuery] int? customerId, CancellationToken cancellationToken)
    {
        var list = await _realEstateService.GetPropertyBookingsAsync(projectId, customerId, cancellationToken);
        return Ok(new { success = true, data = list });
    }

    [HttpGet("bookings/{id:int}")]
    public async Task<IActionResult> GetBookingById(int id, CancellationToken cancellationToken)
    {
        var booking = await _realEstateService.GetPropertyBookingByIdAsync(id, cancellationToken);
        if (booking == null)
        {
            return NotFound(new { success = false, message = $"Booking with ID {id} was not found." });
        }
        return Ok(new { success = true, data = booking });
    }

    [HttpPost("bookings")]
    public async Task<IActionResult> CreateBooking([FromBody] CreatePropertyBookingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var booking = await _realEstateService.CreatePropertyBookingAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Booking {booking.BookingNumber} created successfully.", data = booking });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("installments/collect")]
    public async Task<IActionResult> CollectInstallment([FromBody] CollectInstallmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _realEstateService.CollectInstallmentAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Installment #{result.InstallmentNumber} collected and posted to GL voucher {result.JournalVoucherNumber}.", data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
