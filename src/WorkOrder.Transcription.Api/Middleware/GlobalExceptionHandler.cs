using Microsoft.AspNetCore.Diagnostics;
using WorkOrder.Transcription.Application.DTOs;
using WorkOrder.Transcription.Application.Exceptions;

namespace WorkOrder.Transcription.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, error, detail) = exception switch
        {
            NotFoundException ex =>
                (StatusCodes.Status404NotFound, "Resource not found", ex.Message),

            SpeechRecognitionException ex =>
                (StatusCodes.Status422UnprocessableEntity,
                 "Speech recognition failed",
                 "Unable to transcribe audio. Please ensure clear speech in a quiet environment."),

            AudioProcessingException ex =>
                (StatusCodes.Status400BadRequest,
                 "Audio file invalid",
                 "The audio file could not be processed. Ensure it's a valid audio file."),

            OperationCanceledException =>
                (StatusCodes.Status408RequestTimeout,
                 "Request timeout",
                 "The transcription request timed out. Please try a shorter recording."),

            _ =>
                (StatusCodes.Status500InternalServerError,
                 "Internal server error",
                 "An unexpected error occurred. Please try again.")
        };

        _logger.LogError(exception, "Transcription error: {Error}", error);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ErrorResponse(error, detail),
            cancellationToken);

        return true;
    }
}
