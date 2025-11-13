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

    public async Task<List<Asset>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Assets.ToListAsync(cancellationToken);
    }

    public async Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Asset?> GetByAssetIdAsync(string assetId, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(assetId);
        return await _context.Assets
            .FirstOrDefaultAsync(a => a.AssetId.ToUpper() == normalized, cancellationToken);
    }

    public async Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        await _context.Assets.AddAsync(asset, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        _context.Assets.Update(asset);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await GetByIdAsync(id, cancellationToken);
        if (asset != null)
        {
            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static string Normalize(string id)
    {
        return id.Replace(" ", "").Replace("–", "-").ToUpperInvariant();
    }
}
