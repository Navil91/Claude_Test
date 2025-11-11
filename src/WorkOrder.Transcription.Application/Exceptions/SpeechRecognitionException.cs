namespace WorkOrder.Transcription.Application.Exceptions;

public class SpeechRecognitionException : TranscriptionException
{
    public string? VendorCode { get; init; }

    public SpeechRecognitionException(string message, string? vendorCode = null)
        : base(message)
    {
        VendorCode = vendorCode;
    }
}
