using WorkOrder.Transcription.Domain.Entities;

namespace WorkOrder.Transcription.Application.Interfaces;

public interface IAssetRepository
{
    Task<Asset?> FindExactAsync(string assetId, CancellationToken cancellationToken = default);
    Task<List<Asset>> FindByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string assetId, CancellationToken cancellationToken = default);
}
