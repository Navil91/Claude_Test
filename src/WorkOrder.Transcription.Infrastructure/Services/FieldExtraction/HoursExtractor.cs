using System.Globalization;
using System.Text.RegularExpressions;

namespace WorkOrder.Transcription.Infrastructure.Services.FieldExtraction;

public class HoursExtractor
{
    private static readonly Dictionary<string, string[]> HourKeywords = new()
    {
        ["en-GB"] = new[] { "hours", "hrs", "hr", "h" },
        ["sv-SE"] = new[] { "timmar", "timme", "tim", "h" },
        ["fr-FR"] = new[] { "heures", "heure", "h" },
        ["es-ES"] = new[] { "horas", "hora", "h" },
        ["de-DE"] = new[] { "Stunden", "Stunde", "Std", "h" }
    };

    public decimal? Extract(string text, string locale)
    {
        if (!HourKeywords.ContainsKey(locale))
            locale = "en-GB";

        var keywords = string.Join("|", HourKeywords[locale]);
        var pattern = $@"(?<hours>\d+(?:[.,]\d+)?)\s*(?:{keywords})\b";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        var match = regex.Match(text);
        if (!match.Success)
            return null;

        var hoursStr = match.Groups["hours"].Value.Replace(',', '.'); // European decimal

        return decimal.TryParse(hoursStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var hours)
            ? hours
            : null;
    }
}
