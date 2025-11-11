using System.Text.RegularExpressions;

namespace WorkOrder.Transcription.Infrastructure.Services.FieldExtraction;

public class AssetIdExtractor
{
    // Patterns ordered by specificity
    private static readonly Regex[] Patterns = new[]
    {
        // 1) ROLLER A-5 (exact format)
        new Regex(@"\bROLLER\s+([A-Z])-(\d+)\b", RegexOptions.IgnoreCase),

        // 2) Standard prefix-number: TX-482, PUMP-17
        new Regex(@"\b([A-Z]{2,6})-(\d{1,6})\b", RegexOptions.IgnoreCase),

        // 3) No-dash variant: Q5001, TX482
        new Regex(@"\b([A-Z]{1,4})(\d{3,6})\b", RegexOptions.IgnoreCase),

        // 4) Spaced variant: "TX 482" (after canonicalize should be TX482)
        new Regex(@"\b([A-Z]{2,6})\s+(\d{1,6})\b", RegexOptions.IgnoreCase)
    };

    public List<string> Extract(string canonicalText)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in Patterns)
        {
            var matches = pattern.Matches(canonicalText);
            foreach (Match match in matches)
            {
                var candidate = match.Value.Trim();
                // Normalize: ensure dash between letters and numbers
                candidate = NormalizeFormat(candidate);
                candidates.Add(candidate);
            }
        }

        return candidates.ToList();
    }

    private string NormalizeFormat(string id)
    {
        // "TX482" → "TX-482", "ROLLERA5" → "ROLLER A-5"
        if (id.StartsWith("ROLLER", StringComparison.OrdinalIgnoreCase))
            return Regex.Replace(id, @"ROLLER\s*([A-Z])\s*(\d+)", "ROLLER $1-$2", RegexOptions.IgnoreCase);

        return Regex.Replace(id, @"([A-Z]+)\s*(\d+)", "$1-$2", RegexOptions.IgnoreCase);
    }
}
