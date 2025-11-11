using WorkOrder.Transcription.Domain.ValueObjects;

namespace WorkOrder.Transcription.Application.Interfaces;

public interface ICatalogueResolverService
{
    Task<AssetResolution> ResolveAsync(
        List<string> candidates,
        CancellationToken cancellationToken = default);
}
