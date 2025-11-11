using WorkOrder.Transcription.Application.DTOs;

namespace WorkOrder.Transcription.Application.Interfaces;

public interface ILlmFieldExtractionService
{
    Task<LlmExtraction> ExtractAsync(
        string transcript,
        string locale,
        CancellationToken cancellationToken = default);
}
