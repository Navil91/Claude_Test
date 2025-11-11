using WorkOrder.Transcription.Application.DTOs;

namespace WorkOrder.Transcription.Application.Interfaces;

public interface ISpeechToTextService
{
    Task<SpeechRecognitionResult> RecognizeAsync(
        Stream audioStream,
        string locale,
        CancellationToken cancellationToken = default);
}
