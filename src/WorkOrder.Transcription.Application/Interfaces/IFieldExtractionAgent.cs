using WorkOrder.Transcription.Application.DTOs;

namespace WorkOrder.Transcription.Application.Interfaces;

public interface IFieldExtractionAgent
{
    Task<FieldExtractionResult> ExtractAsync(
        string text,
        string locale,
        CancellationToken cancellationToken = default);
}
