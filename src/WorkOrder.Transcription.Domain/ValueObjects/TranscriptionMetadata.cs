namespace WorkOrder.Transcription.Domain.ValueObjects;

public record TranscriptionMetadata(
    string Vendor,
    string Language,
    int DurationSeconds,
    bool UsedLlmFallback,
    int ProcessingTimeMs,
    string? VendorRequestId = null);
