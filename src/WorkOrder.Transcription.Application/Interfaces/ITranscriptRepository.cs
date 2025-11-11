using WorkOrder.Transcription.Domain.Entities;

namespace WorkOrder.Transcription.Application.Interfaces;

public interface ITranscriptRepository
{
    Task AddAsync(Transcript transcript, CancellationToken cancellationToken = default);
    Task<Transcript?> GetLatestByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
}
