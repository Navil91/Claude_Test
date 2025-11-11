namespace WorkOrder.Transcription.Application.DTOs;

public record ErrorResponse(
    string Error,
    string? Detail = null,
    Dictionary<string, string[]>? ValidationErrors = null);
