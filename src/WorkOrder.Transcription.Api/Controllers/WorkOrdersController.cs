using Microsoft.AspNetCore.Mvc;
using WorkOrder.Transcription.Application.DTOs;
using WorkOrder.Transcription.Application.Interfaces;

namespace WorkOrder.Transcription.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkOrdersController : ControllerBase
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly ILogger<WorkOrdersController> _logger;

    public WorkOrdersController(
        IWorkOrderRepository workOrderRepository,
        ILogger<WorkOrdersController> logger)
    {
        _workOrderRepository = workOrderRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all work orders
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Domain.Entities.WorkOrder>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var workOrders = await _workOrderRepository.GetAllAsync();

        _logger.LogInformation("Retrieved {Count} work orders", workOrders.Count());

        return Ok(workOrders);
    }

    /// <summary>
    /// Get work order by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Domain.Entities.WorkOrder), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var workOrder = await _workOrderRepository.GetByIdAsync(id);

        if (workOrder == null)
        {
            return NotFound(new ErrorResponse(
                "Work order not found",
                $"No work order found with ID {id}"));
        }

        return Ok(workOrder);
    }

    /// <summary>
    /// Get work orders by asset ID
    /// </summary>
    [HttpGet("by-asset/{assetId}")]
    [ProducesResponseType(typeof(IEnumerable<Domain.Entities.WorkOrder>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByAssetId(string assetId)
    {
        var workOrders = await _workOrderRepository.GetByAssetIdAsync(assetId);

        _logger.LogInformation("Found {Count} work orders for asset {AssetId}",
            workOrders.Count(), assetId);

        return Ok(workOrders);
    }

    /// <summary>
    /// Create a new work order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Domain.Entities.WorkOrder), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest request)
    {
        var workOrder = new Domain.Entities.WorkOrder
        {
            Id = Guid.NewGuid(),
            AssetId = request.AssetId,
            Comment = request.Comment,
            LabourHours = request.LabourHours
        };

        await _workOrderRepository.AddAsync(workOrder);

        _logger.LogInformation("Created work order {WorkOrderId} for asset {AssetId}",
            workOrder.Id, workOrder.AssetId);

        return CreatedAtAction(nameof(GetById), new { id = workOrder.Id }, workOrder);
    }

    /// <summary>
    /// Update an existing work order
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Domain.Entities.WorkOrder), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkOrderRequest request)
    {
        var workOrder = await _workOrderRepository.GetByIdAsync(id);

        if (workOrder == null)
        {
            return NotFound(new ErrorResponse(
                "Work order not found",
                $"No work order found with ID {id}"));
        }

        // Update fields
        if (request.AssetId != null)
            workOrder.AssetId = request.AssetId;

        if (request.Comment != null)
            workOrder.Comment = request.Comment;

        if (request.LabourHours.HasValue)
            workOrder.LabourHours = request.LabourHours.Value;

        await _workOrderRepository.UpdateAsync(workOrder);

        _logger.LogInformation("Updated work order {WorkOrderId}", workOrder.Id);

        return Ok(workOrder);
    }

    /// <summary>
    /// Delete a work order
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var workOrder = await _workOrderRepository.GetByIdAsync(id);

        if (workOrder == null)
        {
            return NotFound(new ErrorResponse(
                "Work order not found",
                $"No work order found with ID {id}"));
        }

        await _workOrderRepository.DeleteAsync(id);

        _logger.LogInformation("Deleted work order {WorkOrderId}", id);

        return NoContent();
    }

    /// <summary>
    /// Get work order statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(WorkOrderStatistics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        var allWorkOrders = await _workOrderRepository.GetAllAsync();
        var workOrdersList = allWorkOrders.ToList();

        var stats = new WorkOrderStatistics
        {
            TotalWorkOrders = workOrdersList.Count,
            TotalLabourHours = workOrdersList.Sum(wo => wo.LabourHours ?? 0),
            WithTranscription = workOrdersList.Count(wo => !string.IsNullOrEmpty(wo.TranscriptText)),
            AverageConfidence = workOrdersList
                .Where(wo => wo.TranscriptConfidence.HasValue)
                .Select(wo => wo.TranscriptConfidence!.Value)
                .DefaultIfEmpty(0)
                .Average(),
            WorkOrdersByAsset = workOrdersList
                .Where(wo => !string.IsNullOrEmpty(wo.AssetId))
                .GroupBy(wo => wo.AssetId!)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return Ok(stats);
    }
}

// DTOs
public record CreateWorkOrderRequest(
    string? AssetId,
    string? Comment,
    decimal? LabourHours);

public record UpdateWorkOrderRequest(
    string? AssetId,
    string? Comment,
    decimal? LabourHours);

public record WorkOrderStatistics(
    int TotalWorkOrders,
    decimal TotalLabourHours,
    int WithTranscription,
    decimal AverageConfidence,
    Dictionary<string, int> WorkOrdersByAsset);
