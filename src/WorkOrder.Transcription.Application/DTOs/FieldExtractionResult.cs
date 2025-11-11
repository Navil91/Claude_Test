namespace WorkOrder.Transcription.Application.DTOs;

public class FieldExtractionResult
{
    public List<string> AssetCandidates { get; set; } = new();
    public decimal? Hours { get; set; }
    public string? Comment { get; set; }
}
