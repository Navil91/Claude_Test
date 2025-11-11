using System.Text;
using System.Text.RegularExpressions;

namespace WorkOrder.Transcription.Infrastructure.Services.FieldExtraction;

public class TextCanonicalizer
{
    private static readonly Dictionary<string, string[]> NumberWords = new()
    {
        ["en-GB"] = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" },
        ["sv-SE"] = new[] { "noll", "ett", "två", "tre", "fyra", "fem", "sex", "sju", "åtta", "nio" },
        ["fr-FR"] = new[] { "zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf" },
        ["es-ES"] = new[] { "cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve" },
        ["de-DE"] = new[] { "null", "eins", "zwei", "drei", "vier", "fünf", "sechs", "sieben", "acht", "neun" }
    };

    private static readonly Dictionary<string, string[]> DashWords = new()
    {
        ["en-GB"] = new[] { "dash", "hyphen", "minus" },
        ["sv-SE"] = new[] { "bindestreck", "streck" },
        ["fr-FR"] = new[] { "tiret" },
        ["es-ES"] = new[] { "guion" },
        ["de-DE"] = new[] { "Bindestrich" }
    };

    public string Canonicalize(string text, string locale)
    {
        if (!NumberWords.ContainsKey(locale))
            locale = "en-GB"; // Default fallback

        var result = text;

        // 1) Replace number words with digits (locale-aware)
        result = ReplaceNumberWords(result, locale);

        // 2) Replace dash words
        if (DashWords.ContainsKey(locale))
        {
            foreach (var dashWord in DashWords[locale])
                result = Regex.Replace(result, $@"\b{dashWord}\b", "-", RegexOptions.IgnoreCase);
        }

        // 3) Normalize spacing around alphanumeric IDs
        result = Regex.Replace(result, @"([A-Z])\s+([A-Z])", "$1$2", RegexOptions.IgnoreCase);  // "T X" → "TX"
        result = Regex.Replace(result, @"([A-Z])\s*-\s*(\d)", "$1-$2", RegexOptions.IgnoreCase); // "TX - 482" → "TX-482"
        result = Regex.Replace(result, @"(\d)\s+(\d)", "$1$2");  // "4 8 2" → "482"

        // 4) Normalize "ROLLER A dash 5" → "ROLLER A-5"
        result = Regex.Replace(result, @"ROLLER\s+([A-Z])\s*-\s*(\d+)",
            "ROLLER $1-$2", RegexOptions.IgnoreCase);

        // 5) Collapse multiple spaces
        result = Regex.Replace(result, @"\s{2,}", " ");

        return result.Trim();
    }

    private string ReplaceNumberWords(string text, string locale)
    {
        var words = text.Split(' ');
        var numberWords = NumberWords[locale];
        var result = new StringBuilder();
        var numberSequence = new List<int>();

        foreach (var word in words)
        {
            var lowerWord = word.ToLowerInvariant().Trim(',', '.', '!', '?');
            var digitIndex = Array.IndexOf(numberWords, lowerWord);

            if (digitIndex >= 0)
            {
                numberSequence.Add(digitIndex);
            }
            else
            {
                // Flush accumulated digits
                if (numberSequence.Any())
                {
                    result.Append(string.Join("", numberSequence));
                    result.Append(' ');
                    numberSequence.Clear();
                }
                result.Append(word).Append(' ');
            }
        }

        if (numberSequence.Any())
            result.Append(string.Join("", numberSequence));

        return result.ToString().Trim();
    }
}
