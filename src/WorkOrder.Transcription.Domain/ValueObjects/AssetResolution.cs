using WorkOrder.Transcription.Domain.Entities;

namespace WorkOrder.Transcription.Domain.ValueObjects;

public record AssetResolution(
    Asset? Asset,
    ResolutionType Type,
    double Confidence)
{
    public static AssetResolution Exact(Asset asset, double confidence) =>
        new(asset, ResolutionType.Exact, confidence);

    public static AssetResolution Fuzzy(Asset asset, double confidence) =>
        new(asset, ResolutionType.Fuzzy, confidence);

    public static AssetResolution NotFound() =>
        new(null, ResolutionType.NotFound, 0.0);
}

public enum ResolutionType
{
    Exact,
    Fuzzy,
    NotFound
}
