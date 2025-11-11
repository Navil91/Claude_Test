using Microsoft.EntityFrameworkCore;
using WorkOrder.Transcription.Application.Interfaces;
using WorkOrder.Transcription.Domain.Entities;
using WorkOrder.Transcription.Infrastructure.Data;

namespace WorkOrder.Transcription.Infrastructure.Repositories;

public class TranscriptRepository : ITranscriptRepository
{
    private readonly WorkOrderDbContext _context;

    public TranscriptRepository(WorkOrderDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Transcript transcript, CancellationToken cancellationToken = default)
    {
        await _context.Transcripts.AddAsync(transcript, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Transcript?> GetLatestByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        return await _context.Transcripts
            .Where(t => t.WorkOrderId == workOrderId)
            .OrderByDescending(t => t.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
