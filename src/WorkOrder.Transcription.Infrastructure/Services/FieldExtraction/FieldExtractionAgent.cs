using WorkOrder.Transcription.Application.DTOs;
using WorkOrder.Transcription.Application.Interfaces;

namespace WorkOrder.Transcription.Infrastructure.Services.FieldExtraction;

public class FieldExtractionAgent : IFieldExtractionAgent
{
    private readonly TextCanonicalizer _canonicalizer;
    private readonly AssetIdExtractor _assetExtractor;
    private readonly HoursExtractor _hoursExtractor;

    public FieldExtractionAgent()
    {
        _canonicalizer = new TextCanonicalizer();
        _assetExtractor = new AssetIdExtractor();
        _hoursExtractor = new HoursExtractor();
    }

    public Task<FieldExtractionResult> ExtractAsync(
        string text,
        string locale,
        CancellationToken cancellationToken = default)
    {
        // Canonicalize text first
        var canonical = _canonicalizer.Canonicalize(text, locale);

        // Extract candidates
        var assetCandidates = _assetExtractor.Extract(canonical);
        var hours = _hoursExtractor.Extract(text, locale);

        // Build comment (cleaned transcript)
        var comment = CleanComment(text);

        var result = new FieldExtractionResult
        {
            AssetCandidates = assetCandidates,
            Hours = hours,
            Comment = comment
        };

        return Task.FromResult(result);
    }

    private string CleanComment(string text)
    {
        // Basic cleaning: trim, normalize spaces
        var cleaned = text.Trim();
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s{2,}", " ");
        return cleaned;
    }
}
