namespace WorkOrder.Transcription.Application.Exceptions;

public class AudioProcessingException : TranscriptionException
{
    public AudioProcessingException(string message, Exception inner) : base(message, inner) { }
}
