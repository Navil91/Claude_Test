using Microsoft.EntityFrameworkCore;
using WorkOrder.Transcription.Application.Interfaces;
using WorkOrder.Transcription.Domain.Entities;
using WorkOrder.Transcription.Infrastructure.Data;

namespace WorkOrder.Transcription.Infrastructure.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly WorkOrderDbContext _context;

    public AssetRepository(WorkOrderDbContext context)
    {
        _context = context;
    }

    public async Task<Asset?> FindExactAsync(string assetId, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(assetId);
        return await _context.Assets
            .Where(a => a.IsActive)
            .FirstOrDefaultAsync(a => a.AssetId.ToUpper() == normalized, cancellationToken);
    }

    public async Task<List<Asset>> FindByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return new List<Asset>();

        var upperPrefix = prefix.ToUpperInvariant();
        return await _context.Assets
            .Where(a => a.IsActive && a.AssetId.ToUpper().StartsWith(upperPrefix))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string assetId, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(assetId);
        return await _context.Assets
            .AnyAsync(a => a.IsActive && a.AssetId.ToUpper() == normalized, cancellationToken);
    }

    private static string Normalize(string id)
    {
        return id.Replace(" ", "").Replace("–", "-").ToUpperInvariant();
    }
}
