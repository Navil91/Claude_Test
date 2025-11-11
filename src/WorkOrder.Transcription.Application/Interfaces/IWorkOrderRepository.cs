using WorkOrder.Transcription.Domain.Entities;

namespace WorkOrder.Transcription.Application.Interfaces;

public interface IWorkOrderRepository
{
    Task<WorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);
}
