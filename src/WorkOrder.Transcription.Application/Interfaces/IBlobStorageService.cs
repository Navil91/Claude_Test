namespace WorkOrder.Transcription.Application.Interfaces;

public interface IBlobStorageService
{
    Task<string> SaveAudioAsync(
        Guid workOrderId,
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string audioUri,
        CancellationToken cancellationToken = default);
}
