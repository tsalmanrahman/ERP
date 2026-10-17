using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Specialized;
using ERP.Common.Application.Models.Specialized;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/fa")]
[Authorize]
public class FixedAssetsController : ControllerBase
{
    private readonly IFixedAssetService _fixedAssetService;

    public FixedAssetsController(IFixedAssetService fixedAssetService)
    {
        _fixedAssetService = fixedAssetService;
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GetAssetGroups(CancellationToken cancellationToken)
    {
        var list = await _fixedAssetService.GetAssetGroupsAsync(cancellationToken);
        return Ok(new { success = true, data = list });
    }

    [HttpPost("groups")]
    public async Task<IActionResult> CreateAssetGroup([FromBody] CreateAssetGroupRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var group = await _fixedAssetService.CreateAssetGroupAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Asset group {group.GroupName} created successfully.", data = group });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("assets")]
    public async Task<IActionResult> GetAssets([FromQuery] int? assetGroupId, [FromQuery] bool? isDisposed, CancellationToken cancellationToken)
    {
        var list = await _fixedAssetService.GetAssetsAsync(assetGroupId, isDisposed, cancellationToken);
        return Ok(new { success = true, data = list });
    }

    [HttpGet("assets/{id:int}")]
    public async Task<IActionResult> GetAssetById(int id, CancellationToken cancellationToken)
    {
        var asset = await _fixedAssetService.GetAssetByIdAsync(id, cancellationToken);
        if (asset == null)
        {
            return NotFound(new { success = false, message = $"Fixed asset with ID {id} was not found." });
        }
        return Ok(new { success = true, data = asset });
    }

    [HttpPost("assets")]
    public async Task<IActionResult> CreateAsset([FromBody] CreateAssetRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var asset = await _fixedAssetService.CreateAssetAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Fixed asset {asset.AssetName} ({asset.AssetCode}) created successfully.", data = asset });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("depreciation/run")]
    public async Task<IActionResult> RunDepreciation([FromBody] RunDepreciationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fixedAssetService.RunDepreciationAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Monthly depreciation run completed and posted to GL voucher {result.JournalVoucherNumber}.", data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("assets/dispose")]
    public async Task<IActionResult> DisposeAsset([FromBody] DisposeAssetRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fixedAssetService.DisposeAssetAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Asset {result.AssetCode} successfully disposed and posted to GL voucher {result.JournalVoucherNumber}.", data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("reports/schedule")]
    public async Task<IActionResult> GetAssetSchedule([FromQuery] DateTime? asOfDate, CancellationToken cancellationToken)
    {
        var report = await _fixedAssetService.GetAssetScheduleReportAsync(asOfDate, cancellationToken);
        return Ok(new { success = true, data = report });
    }
}
