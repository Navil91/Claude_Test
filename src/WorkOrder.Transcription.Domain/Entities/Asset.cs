namespace WorkOrder.Transcription.Domain.Entities;

public class Asset
{
    public Guid Id { get; set; }
    public string AssetId { get; set; } = string.Empty;
    public string? AssetName { get; set; }
    public string? AssetType { get; set; }
    public bool IsActive { get; set; } = true;
}
