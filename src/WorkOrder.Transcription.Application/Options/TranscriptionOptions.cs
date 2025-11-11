namespace WorkOrder.Transcription.Application.Options;

public class TranscriptionOptions
{
    public bool EnableLlmFallback { get; init; } = true;
    public double LlmThreshold { get; init; } = 0.8;
    public double MinConfidenceToApply { get; init; } = 0.6;
}
