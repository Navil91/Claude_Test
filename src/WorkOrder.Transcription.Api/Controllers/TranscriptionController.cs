using Microsoft.AspNetCore.Mvc;
using WorkOrder.Transcription.Application.DTOs;
using WorkOrder.Transcription.Application.Services;

namespace WorkOrder.Transcription.Api.Controllers;

[ApiController]
[Route("api/workorders")]
public class TranscriptionController : ControllerBase
{
    private readonly WorkOrderTranscriptionService _transcriptionService;
    private readonly ILogger<TranscriptionController> _logger;

    private static readonly string[] SupportedLocales = {
        "en-GB", "sv-SE", "fr-FR", "es-ES", "de-DE"
    };

    private static readonly string[] AllowedAudioTypes = {
        "audio/wav", "audio/webm", "audio/mpeg", "audio/ogg"
    };

    public TranscriptionController(
        WorkOrderTranscriptionService transcriptionService,
        ILogger<TranscriptionController> logger)
    {
        _transcriptionService = transcriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Transcribe audio for a work order
    /// </summary>
    /// <param name="id">Work Order ID</param>
    /// <param name="audio">Audio file (WAV/MP3/WEBM/OGG, max 10MB)</param>
    /// <param name="locale">Language locale (en-GB, sv-SE, fr-FR, es-ES, de-DE)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Transcription result with extracted fields</returns>
    [HttpPost("{id:guid}/transcribe")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TranscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [RequestSizeLimit(10_000_000)] // 10MB max
    public async Task<IActionResult> Transcribe(
        [FromRoute] Guid id,
        [FromForm] IFormFile audio,
        [FromQuery] string locale = "en-GB",
        CancellationToken ct = default)
    {
        // Validation
        if (!SupportedLocales.Contains(locale))
        {
            return BadRequest(new ErrorResponse(
                "Unsupported locale",
                $"Supported locales: {string.Join(", ", SupportedLocales)}"));
        }

        if (audio == null || audio.Length == 0)
        {
            return BadRequest(new ErrorResponse(
                "Audio file required",
                "Please provide a valid audio file"));
        }

        if (audio.Length > 10_000_000)
        {
            return BadRequest(new ErrorResponse(
                "Audio file too large",
                "Maximum file size is 10MB"));
        }

        if (!AllowedAudioTypes.Contains(audio.ContentType?.ToLowerInvariant() ?? ""))
        {
            return BadRequest(new ErrorResponse(
                "Unsupported audio format",
                $"Supported formats: {string.Join(", ", AllowedAudioTypes)}"));
        }

        try
        {
            _logger.LogInformation(
                "Transcription request: WO={WorkOrderId}, Locale={Locale}, FileSize={FileSize}",
                id, locale, audio.Length);

            using var stream = audio.OpenReadStream();
            var result = await _transcriptionService.TranscribeAsync(id, stream, locale, ct);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed for WO {WorkOrderId}", id);
            throw; // Let global exception handler deal with it
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
