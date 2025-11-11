namespace WorkOrder.Transcription.Application.DTOs;

public record LlmExtraction(
    string? Asset,
    string Comment,
    decimal? Hours);
