using Microsoft.AspNetCore.Mvc;
using WorkOrder.Transcription.Application.DTOs;
using WorkOrder.Transcription.Application.Interfaces;
using WorkOrder.Transcription.Domain.Entities;

namespace WorkOrder.Transcription.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly IAssetRepository _assetRepository;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        IAssetRepository assetRepository,
        ILogger<AssetsController> logger)
    {
        _assetRepository = assetRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all assets
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Asset>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var assets = await _assetRepository.GetAllAsync();

        if (activeOnly)
        {
            assets = assets.Where(a => a.IsActive).ToList();
        }

        _logger.LogInformation("Retrieved {Count} assets (activeOnly={ActiveOnly})",
            assets.Count(), activeOnly);

        return Ok(assets);
    }

    /// <summary>
    /// Get asset by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var asset = await _assetRepository.GetByIdAsync(id);

        if (asset == null)
        {
            return NotFound(new ErrorResponse(
                "Asset not found",
                $"No asset found with ID {id}"));
        }

        return Ok(asset);
    }

    /// <summary>
    /// Get asset by asset ID (e.g., "TX-482")
    /// </summary>
    [HttpGet("by-assetid/{assetId}")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAssetId(string assetId)
    {
        var asset = await _assetRepository.GetByAssetIdAsync(assetId);

        if (asset == null)
        {
            return NotFound(new ErrorResponse(
                "Asset not found",
                $"No asset found with Asset ID '{assetId}'"));
        }

        return Ok(asset);
    }

    /// <summary>
    /// Create a new asset
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAssetRequest request)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.AssetId))
        {
            return BadRequest(new ErrorResponse(
                "Asset ID is required",
                "Please provide a valid Asset ID"));
        }

        // Check for duplicates
        var existing = await _assetRepository.GetByAssetIdAsync(request.AssetId);
        if (existing != null)
        {
            return BadRequest(new ErrorResponse(
                "Asset ID already exists",
                $"An asset with ID '{request.AssetId}' already exists"));
        }

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            AssetId = request.AssetId,
            AssetName = request.AssetName,
            AssetType = request.AssetType,
            IsActive = request.IsActive ?? true
        };

        await _assetRepository.AddAsync(asset);

        _logger.LogInformation("Created asset {AssetId} with ID {Id}",
            asset.AssetId, asset.Id);

        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset);
    }

    /// <summary>
    /// Update an existing asset
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssetRequest request)
    {
        var asset = await _assetRepository.GetByIdAsync(id);

        if (asset == null)
        {
            return NotFound(new ErrorResponse(
                "Asset not found",
                $"No asset found with ID {id}"));
        }

        // Update fields
        if (!string.IsNullOrWhiteSpace(request.AssetName))
            asset.AssetName = request.AssetName;

        if (!string.IsNullOrWhiteSpace(request.AssetType))
            asset.AssetType = request.AssetType;

        if (request.IsActive.HasValue)
            asset.IsActive = request.IsActive.Value;

        await _assetRepository.UpdateAsync(asset);

        _logger.LogInformation("Updated asset {AssetId}", asset.AssetId);

        return Ok(asset);
    }

    /// <summary>
    /// Delete an asset (soft delete by marking inactive)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] bool hardDelete = false)
    {
        var asset = await _assetRepository.GetByIdAsync(id);

        if (asset == null)
        {
            return NotFound(new ErrorResponse(
                "Asset not found",
                $"No asset found with ID {id}"));
        }

        if (hardDelete)
        {
            await _assetRepository.DeleteAsync(id);
            _logger.LogInformation("Hard deleted asset {AssetId}", asset.AssetId);
        }
        else
        {
            asset.IsActive = false;
            await _assetRepository.UpdateAsync(asset);
            _logger.LogInformation("Soft deleted (deactivated) asset {AssetId}", asset.AssetId);
        }

        return NoContent();
    }

    /// <summary>
    /// Search assets by prefix
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<Asset>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return BadRequest(new ErrorResponse(
                "Search prefix required",
                "Please provide a search prefix"));
        }

        var assets = await _assetRepository.FindByPrefixAsync(prefix);

        _logger.LogInformation("Found {Count} assets with prefix '{Prefix}'",
            assets.Count(), prefix);

        return Ok(assets);
    }
}

// DTOs
public record CreateAssetRequest(
    string AssetId,
    string? AssetName,
    string? AssetType,
    bool? IsActive);

public record UpdateAssetRequest(
    string? AssetName,
    string? AssetType,
    bool? IsActive);
