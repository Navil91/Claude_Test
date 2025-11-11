using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkOrder.Transcription.Application.DTOs;
using WorkOrder.Transcription.Application.Exceptions;
using WorkOrder.Transcription.Application.Interfaces;
using WorkOrder.Transcription.Application.Options;
using WorkOrder.Transcription.Domain.Entities;
using WorkOrder.Transcription.Domain.ValueObjects;

namespace WorkOrder.Transcription.Application.Services;

public class WorkOrderTranscriptionService
{
    private readonly IBlobStorageService _blobStorage;
    private readonly ISpeechToTextService _stt;
    private readonly IFieldExtractionAgent _fieldAgent;
    private readonly ICatalogueResolverService _catalogueResolver;
    private readonly ILlmFieldExtractionService _llmFallback;
    private readonly IWorkOrderRepository _woRepo;
    private readonly ITranscriptRepository _transcriptRepo;
    private readonly ILogger<WorkOrderTranscriptionService> _logger;
    private readonly TranscriptionOptions _options;

    public WorkOrderTranscriptionService(
        IBlobStorageService blobStorage,
        ISpeechToTextService stt,
        IFieldExtractionAgent fieldAgent,
        ICatalogueResolverService catalogueResolver,
        ILlmFieldExtractionService llmFallback,
        IWorkOrderRepository woRepo,
        ITranscriptRepository transcriptRepo,
        ILogger<WorkOrderTranscriptionService> logger,
        IOptions<TranscriptionOptions> options)
    {
        _blobStorage = blobStorage;
        _stt = stt;
        _fieldAgent = fieldAgent;
        _catalogueResolver = catalogueResolver;
        _llmFallback = llmFallback;
        _woRepo = woRepo;
        _transcriptRepo = transcriptRepo;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<TranscriptionResponse> TranscribeAsync(
        Guid workOrderId,
        Stream audioStream,
        string locale,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Step 1: Validate WO exists
            var workOrder = await _woRepo.GetByIdAsync(workOrderId, ct)
                ?? throw new NotFoundException($"Work order {workOrderId} not found");

            _logger.LogInformation("Starting transcription for WO {WorkOrderId}, locale: {Locale}",
                workOrderId, locale);

            // Step 2: Save audio to blob storage
            var audioUri = await _blobStorage.SaveAudioAsync(
                workOrderId,
                audioStream,
                $"{Guid.NewGuid()}.wav",
                ct);

            _logger.LogInformation("Audio saved: {AudioUri}", audioUri);

            // Reset stream position for STT
            audioStream.Position = 0;

            // Step 3: Speech-to-Text
            var sttResult = await _stt.RecognizeAsync(audioStream, locale, ct);

            if (!sttResult.Success || string.IsNullOrWhiteSpace(sttResult.Text))
                throw new SpeechRecognitionException("Failed to transcribe audio");

            _logger.LogInformation("Transcribed: {Text} (confidence: {Confidence})",
                sttResult.Text, sttResult.Confidence);

            // Step 4: Rules-based extraction
            var rulesResult = await _fieldAgent.ExtractAsync(sttResult.Text, locale, ct);

            // Step 5: Catalogue resolution
            AssetResolution? assetResolution = null;
            if (rulesResult.AssetCandidates.Any())
            {
                assetResolution = await _catalogueResolver.ResolveAsync(
                    rulesResult.AssetCandidates, ct);
            }

            var asset = assetResolution?.Asset?.AssetId;
            var hours = rulesResult.Hours;
            var comment = rulesResult.Comment ?? string.Empty;
            var confidence = CalculateConfidence(assetResolution, rulesResult);
            var usedLlmFallback = false;

            // Step 6: LLM fallback (if needed)
            if (_options.EnableLlmFallback && confidence < _options.LlmThreshold)
            {
                _logger.LogInformation("Triggering LLM fallback (confidence: {Confidence})", confidence);

                var llmResult = await _llmFallback.ExtractAsync(sttResult.Text, locale, ct);
                usedLlmFallback = true;

                // Validate LLM asset against catalogue
                if (!string.IsNullOrWhiteSpace(llmResult.Asset))
                {
                    var llmAssetResolution = await _catalogueResolver.ResolveAsync(
                        new List<string> { llmResult.Asset }, ct);

                    if (llmAssetResolution.Asset != null)
                        asset = llmAssetResolution.Asset.AssetId;
                }

                // Merge fields (prefer higher confidence)
                hours ??= llmResult.Hours;
                if (!string.IsNullOrWhiteSpace(llmResult.Comment) &&
                    llmResult.Comment.Length > comment.Length)
                {
                    comment = llmResult.Comment;
                }

                confidence = Math.Max(confidence, asset != null ? 0.85 : 0.7);
            }

            // Step 7: Persist transcript (audit trail)
            var transcript = new Transcript
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrderId,
                Text = sttResult.Text,
                Vendor = "AzureSpeech",
                VendorRequestId = sttResult.RequestId,
                Language = locale,
                AudioUri = audioUri,
                AudioDurationSeconds = (int)sttResult.Duration.TotalSeconds,
                IsFinal = true,
                ExtractedAsset = asset,
                ExtractedHours = hours,
                ExtractedComment = comment,
                Confidence = (decimal)confidence,
                UsedLlmFallback = usedLlmFallback,
                CreatedUtc = DateTime.UtcNow
            };

            await _transcriptRepo.AddAsync(transcript, ct);

            // Step 8: Update work order (only if confidence acceptable)
            if (confidence >= _options.MinConfidenceToApply)
            {
                if (!string.IsNullOrWhiteSpace(asset))
                    workOrder.AssetId = asset;

                if (!string.IsNullOrWhiteSpace(comment))
                {
                    workOrder.Comment = string.IsNullOrWhiteSpace(workOrder.Comment)
                        ? comment
                        : $"{workOrder.Comment}\n\n[Speech]: {comment}";
                }

                if (hours.HasValue)
                    workOrder.LabourHours = hours.Value;

                workOrder.TranscriptText = sttResult.Text;
                workOrder.TranscriptConfidence = (decimal)confidence;
                workOrder.TranscriptLocale = locale;
                workOrder.LastTranscribedUtc = DateTime.UtcNow;

                await _woRepo.UpdateAsync(workOrder, ct);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Transcription complete: WO={WoId}, Confidence={Confidence}, Fallback={Fallback}, Time={Ms}ms",
                workOrderId, confidence, usedLlmFallback, stopwatch.ElapsedMilliseconds);

            // Step 9: Return response
            return new TranscriptionResponse(
                WorkOrderId: workOrderId,
                Text: sttResult.Text,
                Fields: new WorkOrderFields(asset, comment, hours, confidence),
                AudioUri: audioUri,
                Metadata: new TranscriptionMetadata(
                    Vendor: "AzureSpeech",
                    Language: locale,
                    DurationSeconds: (int)sttResult.Duration.TotalSeconds,
                    UsedLlmFallback: usedLlmFallback,
                    ProcessingTimeMs: (int)stopwatch.ElapsedMilliseconds,
                    VendorRequestId: sttResult.RequestId
                )
            );
        }
        catch (Exception ex) when (ex is not TranscriptionException)
        {
            _logger.LogError(ex, "Transcription failed for WO {WoId}", workOrderId);
            throw;
        }
    }

    private double CalculateConfidence(
        AssetResolution? assetResolution,
        FieldExtractionResult rulesResult)
    {
        var confidence = 0.5; // Base

        // Asset resolved
        if (assetResolution?.Asset != null)
        {
            confidence += assetResolution.Type == ResolutionType.Exact ? 0.3 : 0.2;
        }

        // Comment quality
        if (!string.IsNullOrWhiteSpace(rulesResult.Comment))
        {
            if (rulesResult.Comment.Length >= 20)
                confidence += 0.15;
            else
                confidence += 0.05;
        }

        // Hours present
        if (rulesResult.Hours.HasValue)
            confidence += 0.05;

        return Math.Min(confidence, 1.0);
    }
}
