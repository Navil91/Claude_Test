using Microsoft.EntityFrameworkCore;
using WorkOrder.Transcription.Application.Interfaces;
using WorkOrder.Transcription.Infrastructure.Data;

namespace WorkOrder.Transcription.Infrastructure.Repositories;

public class WorkOrderRepository : IWorkOrderRepository
{
    private readonly WorkOrderDbContext _context;

    public WorkOrderRepository(WorkOrderDbContext context)
    {
        _context = context;
    }

    public async Task<Domain.Entities.WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WorkOrders
            .FirstOrDefaultAsync(wo => wo.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Domain.Entities.WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        _context.WorkOrders.Update(workOrder);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.WorkOrder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WorkOrders
            .OrderByDescending(wo => wo.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.WorkOrder>> GetByAssetIdAsync(string assetId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkOrders
            .Where(wo => wo.AssetId == assetId)
            .OrderByDescending(wo => wo.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Domain.Entities.WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        await _context.WorkOrders.AddAsync(workOrder, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workOrder = await GetByIdAsync(id, cancellationToken);
        if (workOrder != null)
        {
            _context.WorkOrders.Remove(workOrder);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
