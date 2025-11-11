using WorkOrder.Transcription.Domain.ValueObjects;

namespace WorkOrder.Transcription.Application.DTOs;

public record TranscriptionResponse(
    Guid WorkOrderId,
    string Text,
    WorkOrderFields Fields,
    string AudioUri,
    TranscriptionMetadata Metadata);
