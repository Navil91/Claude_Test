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
}
