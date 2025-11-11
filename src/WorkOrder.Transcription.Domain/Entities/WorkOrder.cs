namespace WorkOrder.Transcription.Domain.Entities;

public class WorkOrder
{
    public Guid Id { get; set; }
    public string? AssetId { get; set; }
    public string? Comment { get; set; }
    public decimal? LabourHours { get; set; }

    // Transcription-specific fields
    public string? TranscriptText { get; set; }
    public decimal? TranscriptConfidence { get; set; }
    public string? TranscriptLocale { get; set; }
    public DateTime? LastTranscribedUtc { get; set; }

    // Navigation property
    public ICollection<Transcript> Transcripts { get; set; } = new List<Transcript>();
}
