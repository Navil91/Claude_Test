namespace WorkOrder.Transcription.Application.DTOs;

public class SpeechRecognitionResult
{
    public bool Success { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public TimeSpan Duration { get; set; }
    public string? RequestId { get; set; }
    public int ProcessingTimeMs { get; set; }
}
