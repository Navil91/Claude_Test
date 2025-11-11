namespace WorkOrder.Transcription.Domain.Entities;

public class Transcript
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string? VendorRequestId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string AudioUri { get; set; } = string.Empty;
    public int? AudioDurationSeconds { get; set; }
    public bool IsFinal { get; set; } = true;
    public DateTime CreatedUtc { get; set; }

    // Extracted fields (denormalized for audit)
    public string? ExtractedAsset { get; set; }
    public decimal? ExtractedHours { get; set; }
    public string? ExtractedComment { get; set; }
    public decimal? Confidence { get; set; }
    public bool UsedLlmFallback { get; set; }

    // Navigation property
    public WorkOrder? WorkOrder { get; set; }
}
