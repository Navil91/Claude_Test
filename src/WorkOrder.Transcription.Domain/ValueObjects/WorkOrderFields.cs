namespace WorkOrder.Transcription.Domain.ValueObjects;

public record WorkOrderFields(
    string? Asset,
    string Comment,
    decimal? Hours,
    double Confidence)
{
    public bool IsHighConfidence => Confidence >= 0.8;
    public bool RequiresReview => Asset == null || Confidence < 0.7;
}
