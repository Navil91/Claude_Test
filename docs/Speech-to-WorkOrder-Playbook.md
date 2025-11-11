# REFINED SPEECH-TO-WORK-ORDER PLAYBOOK v2

## 0) Executive Summary & Success Criteria

**Goal:** Enable hands-free WO creation with ≥85% field auto-fill accuracy, <5s end-to-end latency.

**Success metrics:**
- Asset resolution rate: ≥90% (exact or fuzzy)
- User correction rate: ≤15%
- P95 latency: <5s (upload → response)
- Cost per WO: <$0.01

**Non-goals (v1):**
- Real-time streaming transcription
- Speaker diarization (multi-engineer)
- Offline mode

---

## 1) Architecture Enhancements

### 1.1 Client-Side Specifications

**Audio capture requirements:**
```javascript
// MediaRecorder config (modern browsers)
const constraints = {
  audio: {
    channelCount: 1,        // MONO
    sampleRate: 16000,      // 16kHz
    echoCancellation: true, // reduce background noise
    noiseSuppression: true,
    autoGainControl: true
  }
};

// Format: audio/wav with PCM encoding
const mediaRecorder = new MediaRecorder(stream, {
  mimeType: 'audio/wav',   // Fallback: audio/webm;codecs=opus
  audioBitsPerSecond: 256000
});
```

**Browser compatibility matrix:**
| Browser | MediaRecorder | Web Speech (preview) | Notes |
|---------|---------------|---------------------|-------|
| Chrome 90+ | ✅ | ✅ | Primary target |
| Safari 14+ | ✅ | ❌ | Use MediaRecorder only |
| Firefox 88+ | ✅ | ❌ | |
| Edge 90+ | ✅ | ✅ | Chromium-based |

**MAUI native (iOS/Android):**
- Use `Plugin.AudioRecorder` NuGet package
- Target WAV/PCM 16kHz mono
- Max recording: 2 minutes (120s hard limit)

**UI/UX flow:**
```
[Mic button] → Recording (0:00) → [Stop] → "Processing..." spinner →
Auto-fill fields + banner: "✓ Filled from speech. Review & submit."
```

### 1.2 API Layer (ASP.NET Core)

**Service architecture:**
```
Controller (TranscribeController)
    ↓
WorkOrderService (orchestrator)
    ↓
├─ BlobStorageService (audio persistence)
├─ SpeechToTextService (Azure wrapper)
├─ FieldExtractionAgent (rules + LLM)
└─ CatalogueResolverService (exact + fuzzy)
```

**Endpoint spec:**
```csharp
[HttpPost("api/workorders/{id:guid}/transcribe")]
[Consumes("multipart/form-data")]
[ProducesResponseType(typeof(TranscriptionResponse), 200)]
[ProducesResponseType(typeof(ErrorResponse), 400)]
[ProducesResponseType(typeof(ErrorResponse), 422)]
[RequestSizeLimit(10_000_000)] // 10MB max
[RequestTimeout(30_000)]       // 30s timeout
public async Task<IActionResult> Transcribe(
    [FromRoute] Guid id,
    [FromForm] IFormFile audio,
    [FromQuery] string locale = "en-GB",
    CancellationToken ct = default)
```

**Validation middleware:**
```csharp
// Custom validation filter
if (!SupportedLocales.Contains(locale))
    return BadRequest(new { error = $"Unsupported locale: {locale}" });

if (audio == null || audio.Length == 0)
    return BadRequest(new { error = "Audio file required" });

if (audio.Length > 10_000_000) // 10MB
    return BadRequest(new { error = "Audio file too large (max 10MB)" });

var allowedTypes = new[] { "audio/wav", "audio/webm", "audio/mpeg", "audio/ogg" };
if (!allowedTypes.Contains(audio.ContentType))
    return BadRequest(new { error = $"Unsupported audio type: {audio.ContentType}" });
```

---

## 2) Enhanced Data Model

### 2.1 SQL Schema (with indexes & constraints)

```sql
-- Existing WorkOrders table (add columns)
ALTER TABLE WorkOrders ADD COLUMN
    TranscriptText NVARCHAR(MAX) NULL,
    TranscriptConfidence DECIMAL(3,2) NULL,  -- 0.00 - 1.00
    TranscriptLocale VARCHAR(10) NULL,
    LastTranscribedUtc DATETIME2 NULL;

-- New Transcripts table (audit trail)
CREATE TABLE Transcripts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WorkOrderId UNIQUEIDENTIFIER NOT NULL,
    Text NVARCHAR(MAX) NOT NULL,
    Vendor VARCHAR(50) NOT NULL,           -- 'AzureSpeech'
    VendorRequestId VARCHAR(100) NULL,     -- Azure request ID
    Language VARCHAR(10) NOT NULL,         -- 'en-GB', 'sv-SE'
    AudioUri NVARCHAR(500) NOT NULL,
    AudioDurationSeconds INT NULL,
    IsFinal BIT NOT NULL DEFAULT 1,        -- False for intermediate
    CreatedUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Extracted fields (denormalized for audit)
    ExtractedAsset NVARCHAR(64) NULL,
    ExtractedHours DECIMAL(5,2) NULL,
    ExtractedComment NVARCHAR(MAX) NULL,
    Confidence DECIMAL(3,2) NULL,
    UsedLlmFallback BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_Transcripts_WorkOrders
        FOREIGN KEY (WorkOrderId) REFERENCES WorkOrders(Id) ON DELETE CASCADE,

    INDEX IX_Transcripts_WorkOrderId (WorkOrderId),
    INDEX IX_Transcripts_CreatedUtc (CreatedUtc DESC),
    INDEX IX_Transcripts_Language (Language)
);

-- Assets table (assumed existing, add index for fuzzy)
CREATE INDEX IX_Assets_AssetId_Prefix
    ON Assets(AssetId)
    INCLUDE (AssetName, AssetType);

-- Performance: columnstore for analytics (optional)
CREATE NONCLUSTERED COLUMNSTORE INDEX IX_Transcripts_Analytics
    ON Transcripts (Language, Confidence, UsedLlmFallback, AudioDurationSeconds);
```

### 2.2 Enhanced C# Models

```csharp
// Request
public record TranscribeRequest(
    IFormFile Audio,
    string Locale = "en-GB"
);

// Response
public record TranscriptionResponse(
    Guid WorkOrderId,
    string Text,
    WorkOrderFields Fields,
    string AudioUri,
    TranscriptionMetadata Metadata
);

public record WorkOrderFields(
    string? Asset,
    string Comment,
    decimal? Hours,
    double Confidence
)
{
    public bool IsHighConfidence => Confidence >= 0.8;
    public bool RequiresReview => Asset == null || Confidence < 0.7;
}

public record TranscriptionMetadata(
    string Vendor,
    string Language,
    int DurationSeconds,
    bool UsedLlmFallback,
    int ProcessingTimeMs,
    string? VendorRequestId = null
);

// Error response
public record ErrorResponse(
    string Error,
    string? Detail = null,
    Dictionary<string, string[]>? ValidationErrors = null
);
```

---

## 3) Speech-to-Text Service (Azure Integration)

### 3.1 Configuration

```json
// appsettings.json
{
  "AzureSpeech": {
    "SubscriptionKey": "***",
    "Region": "westeurope",
    "Endpoint": "https://westeurope.api.cognitive.microsoft.com",
    "RecognitionMode": "Conversation",
    "ProfanityOption": "Masked",
    "OutputFormat": "Detailed",
    "EnableDictation": true,
    "RequestTimeoutSeconds": 30,
    "RetryAttempts": 2,
    "RetryDelayMs": 1000
  },

  "PhraseLists": {
    "en-GB": ["TX", "PUMP", "ROLLER", "VALVE", "asset", "hours", "work order"],
    "sv-SE": ["TX", "PUMP", "ROLLER", "VALVE", "tillgång", "timmar", "arbetsorder"],
    "fr-FR": ["TX", "PUMP", "ROLLER", "VALVE", "actif", "heures", "ordre"],
    "es-ES": ["TX", "PUMP", "ROLLER", "VALVE", "activo", "horas", "orden"],
    "de-DE": ["TX", "PUMP", "ROLLER", "VALVE", "Anlage", "Stunden", "Auftrag"]
  }
}
```

### 3.2 Service Implementation

```csharp
public class AzureSpeechService : ISpeechToTextService
{
    private readonly SpeechConfig _config;
    private readonly ILogger<AzureSpeechService> _logger;
    private readonly IOptions<AzureSpeechOptions> _options;

    public async Task<SpeechRecognitionResult> RecognizeAsync(
        Stream audioStream,
        string locale,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        // Setup config
        var config = SpeechConfig.FromSubscription(
            _options.Value.SubscriptionKey,
            _options.Value.Region);

        config.SpeechRecognitionLanguage = locale;
        config.OutputFormat = OutputFormat.Detailed;
        config.SetProfanity(ProfanityOption.Masked);

        // Add phrase list (boost recognition)
        var phraseList = PhraseListGrammar.FromRecognizer(recognizer);
        foreach (var phrase in _options.Value.PhraseLists[locale])
            phraseList.AddPhrase(phrase);

        // Recognize (single-shot)
        using var audioConfig = AudioConfig.FromStreamInput(
            new PushAudioInputStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1)));

        using var recognizer = new SpeechRecognizer(config, audioConfig);

        var result = await recognizer.RecognizeOnceAsync(ct);

        stopwatch.Stop();

        // Handle result
        return result.Reason switch
        {
            ResultReason.RecognizedSpeech => new SpeechRecognitionResult
            {
                Success = true,
                Text = result.Text,
                Confidence = result.Best().FirstOrDefault()?.Confidence ?? 0.0,
                Duration = result.Duration,
                RequestId = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult),
                ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds
            },

            ResultReason.NoMatch => throw new SpeechRecognitionException(
                "No speech recognized. Please try again in a quieter environment."),

            ResultReason.Canceled =>
                throw new SpeechRecognitionException($"Recognition canceled: {result.CancellationReason}"),

            _ => throw new SpeechRecognitionException($"Unexpected result: {result.Reason}")
        };
    }
}
```

---

## 4) Field Extraction Agent (Rules + LLM)

### 4.1 Enhanced Canonicalization

```csharp
public class TextCanonicalizer
{
    private static readonly Dictionary<string, string[]> NumberWords = new()
    {
        ["en-GB"] = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" },
        ["sv-SE"] = new[] { "noll", "ett", "två", "tre", "fyra", "fem", "sex", "sju", "åtta", "nio" },
        // ... other locales
    };

    private static readonly Dictionary<string, string[]> DashWords = new()
    {
        ["en-GB"] = new[] { "dash", "hyphen", "minus" },
        ["sv-SE"] = new[] { "bindestreck", "streck" },
        // ...
    };

    public string Canonicalize(string text, string locale)
    {
        var result = text;

        // 1) Replace number words with digits (locale-aware)
        result = ReplaceNumberWords(result, locale);

        // 2) Replace dash words
        foreach (var dashWord in DashWords[locale])
            result = Regex.Replace(result, $@"\b{dashWord}\b", "-", RegexOptions.IgnoreCase);

        // 3) Normalize spacing around alphanumeric IDs
        // "T X dash 4 8 2" → "TX-482"
        result = Regex.Replace(result, @"([A-Z])\s+([A-Z])", "$1$2");  // TX
        result = Regex.Replace(result, @"([A-Z])\s*-\s*(\d)", "$1-$2"); // TX-482
        result = Regex.Replace(result, @"(\d)\s+(\d)", "$1$2");         // 482

        // 4) Normalize "ROLLER A dash 5" → "ROLLER A-5"
        result = Regex.Replace(result, @"ROLLER\s+([A-Z])\s*-\s*(\d+)",
            "ROLLER $1-$2", RegexOptions.IgnoreCase);

        // 5) Collapse multiple spaces
        result = Regex.Replace(result, @"\s{2,}", " ");

        return result.Trim();
    }

    private string ReplaceNumberWords(string text, string locale)
    {
        // Handle sequences: "five zero zero one" → "5001"
        var words = text.Split(' ');
        var numberWords = NumberWords[locale];
        var result = new StringBuilder();
        var numberSequence = new List<int>();

        foreach (var word in words)
        {
            var lowerWord = word.ToLowerInvariant();
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
```

### 4.2 Asset ID Extraction (Multi-Pattern)

```csharp
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
            return Regex.Replace(id, @"ROLLER([A-Z])(\d+)", "ROLLER $1-$2", RegexOptions.IgnoreCase);

        return Regex.Replace(id, @"([A-Z]+)(\d+)", "$1-$2", RegexOptions.IgnoreCase);
    }
}
```

### 4.3 Hours Extraction (Locale-Aware)

```csharp
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
```

---

## 5) Catalogue Resolver (Exact + Fuzzy)

### 5.1 Multi-Tier Resolution Strategy

```csharp
public class CatalogueResolverService
{
    private readonly IAssetRepository _assetRepo;
    private readonly IMemoryCache _cache;
    private const double FUZZY_THRESHOLD = 0.90; // 90% similarity

    public async Task<AssetResolution> ResolveAsync(List<string> candidates, CancellationToken ct)
    {
        if (!candidates.Any())
            return AssetResolution.NotFound();

        // Tier 1: Exact match (case-insensitive, normalized)
        foreach (var candidate in candidates)
        {
            var exact = await _assetRepo.FindExactAsync(Normalize(candidate), ct);
            if (exact != null)
                return AssetResolution.Exact(exact, confidence: 1.0);
        }

        // Tier 2: Fuzzy match (prefix-constrained)
        var fuzzyResults = new List<(Asset asset, double similarity)>();

        foreach (var candidate in candidates)
        {
            var prefix = GetPrefix(candidate); // e.g., "TX" from "TX-482"
            var assetsWithPrefix = await GetAssetsByPrefixAsync(prefix, ct);

            foreach (var asset in assetsWithPrefix)
            {
                var similarity = CalculateSimilarity(candidate, asset.AssetId);
                if (similarity >= FUZZY_THRESHOLD)
                    fuzzyResults.Add((asset, similarity));
            }
        }

        if (!fuzzyResults.Any())
            return AssetResolution.NotFound();

        // Pick highest similarity
        var best = fuzzyResults.OrderByDescending(x => x.similarity).First();
        return AssetResolution.Fuzzy(best.asset, confidence: best.similarity);
    }

    private string Normalize(string id)
    {
        // Remove spaces, normalize dash
        return id.Replace(" ", "").Replace("–", "-").ToUpperInvariant();
    }

    private string GetPrefix(string id)
    {
        var match = Regex.Match(id, @"^([A-Z]+)");
        return match.Success ? match.Groups[1].Value : "";
    }

    private double CalculateSimilarity(string a, string b)
    {
        // Levenshtein distance normalized
        a = Normalize(a);
        b = Normalize(b);

        var distance = ComputeLevenshteinDistance(a, b);
        var maxLength = Math.Max(a.Length, b.Length);

        return 1.0 - (double)distance / maxLength;
    }

    private async Task<List<Asset>> GetAssetsByPrefixAsync(string prefix, CancellationToken ct)
    {
        // Cache prefix lookups (hot path)
        var cacheKey = $"assets:prefix:{prefix}";
        if (_cache.TryGetValue(cacheKey, out List<Asset> cached))
            return cached;

        var assets = await _assetRepo.FindByPrefixAsync(prefix, ct);
        _cache.Set(cacheKey, assets, TimeSpan.FromMinutes(30));
        return assets;
    }
}

public record AssetResolution(
    Asset? Asset,
    ResolutionType Type,
    double Confidence
)
{
    public static AssetResolution Exact(Asset asset, double confidence) =>
        new(asset, ResolutionType.Exact, confidence);

    public static AssetResolution Fuzzy(Asset asset, double confidence) =>
        new(asset, ResolutionType.Fuzzy, confidence);

    public static AssetResolution NotFound() =>
        new(null, ResolutionType.NotFound, 0.0);
}

public enum ResolutionType { Exact, Fuzzy, NotFound }
```

---

## 6) LLM Fallback (Azure OpenAI)

### 6.1 Configuration

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini",
    "ApiKey": "***",
    "MaxTokens": 150,
    "Temperature": 0.0,
    "TimeoutSeconds": 10,
    "EnableFallback": true,
    "ConfidenceThreshold": 0.8
  }
}
```

### 6.2 Extraction Service

```csharp
public class LlmFieldExtractionService
{
    private readonly OpenAIClient _client;
    private readonly IOptions<AzureOpenAIOptions> _options;

    public async Task<LlmExtraction> ExtractAsync(string transcript, string locale, CancellationToken ct)
    {
        var prompt = BuildPrompt(transcript, locale);

        var chatOptions = new ChatCompletionsOptions
        {
            DeploymentName = _options.Value.DeploymentName,
            Messages =
            {
                new ChatRequestSystemMessage("You extract work order fields from speech transcripts. Return ONLY valid JSON."),
                new ChatRequestUserMessage(prompt)
            },
            MaxTokens = _options.Value.MaxTokens,
            Temperature = 0.0f,
            ResponseFormat = new ChatCompletionsJsonResponseFormat()
        };

        var response = await _client.GetChatCompletionsAsync(chatOptions, ct);
        var content = response.Value.Choices[0].Message.Content;

        var extraction = JsonSerializer.Deserialize<LlmExtraction>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return extraction ?? throw new InvalidOperationException("Failed to parse LLM response");
    }

    private string BuildPrompt(string transcript, string locale)
    {
        return $@"
Extract work order fields from this speech transcript (language: {locale}).

RULES:
1. If an asset/equipment code is mentioned (e.g., ""TX-482"", ""PUMP-17"", ""Q5001"", ""ROLLER A-5""), set ""asset"" field.
2. DO NOT invent asset codes. If uncertain or no code mentioned, set ""asset"": null.
3. Normalize spacing/punctuation in asset codes (e.g., ""T X dash 4 8 2"" → ""TX-482"").
4. Extract work duration into ""hours"" field (number). If not mentioned, set null.
5. Extract the main work description into ""comment"" field (string). Clean up filler words but preserve technical meaning.
6. Return ONLY this JSON structure (no additional text):

{{
  ""asset"": ""<code or null>"",
  ""comment"": ""<description>"",
  ""hours"": <number or null>
}}

TRANSCRIPT:
---
{transcript}
---

JSON:";
    }
}

public record LlmExtraction(
    string? Asset,
    string Comment,
    decimal? Hours
);
```

---

## 7) Testing Strategy

### 7.1 Unit Tests (Key Examples)

```csharp
[TestClass]
public class TextCanonicalizerTests
{
    private readonly TextCanonicalizer _canonicalizer = new();

    [TestMethod]
    [DataRow("T X dash four eight two", "en-GB", "TX-482")]
    [DataRow("PUMP seventeen", "en-GB", "PUMP-17")]
    [DataRow("Q five zero zero one", "en-GB", "Q-5001")]
    [DataRow("ROLLER A dash five", "en-GB", "ROLLER A-5")]
    [DataRow("TX bindestreck fyra åtta två", "sv-SE", "TX-482")]
    public void Canonicalize_VariousFormats_NormalizesCorrectly(
        string input, string locale, string expected)
    {
        var result = _canonicalizer.Canonicalize(input, locale);
        Assert.AreEqual(expected, result);
    }
}

[TestClass]
public class CatalogueResolverTests
{
    [TestMethod]
    public async Task Resolve_ExactMatch_ReturnsHighConfidence()
    {
        // Arrange
        var mockRepo = new Mock<IAssetRepository>();
        mockRepo.Setup(r => r.FindExactAsync("TX-482", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Asset { AssetId = "TX-482", AssetName = "Transformer 482" });

        var resolver = new CatalogueResolverService(mockRepo.Object, Mock.Of<IMemoryCache>());

        // Act
        var result = await resolver.ResolveAsync(new List<string> { "TX-482" }, CancellationToken.None);

        // Assert
        Assert.AreEqual(ResolutionType.Exact, result.Type);
        Assert.AreEqual(1.0, result.Confidence);
        Assert.AreEqual("TX-482", result.Asset.AssetId);
    }
}
```

### 7.2 Golden Dataset

```csv
Locale,Utterance,Expected Asset,Expected Hours,Expected Comment (snippet)
en-GB,"Fixed pump TX-482, took two hours",TX-482,2.0,"Fixed pump"
en-GB,"Roller A dash five needs lubrication",ROLLER A-5,null,"needs lubrication"
sv-SE,"Reparat pump TX fyra åtta två, tog två timmar",TX-482,2.0,"Reparat pump"
fr-FR,"Réparé la pompe TX tiret quatre huit deux",TX-482,null,"Réparé la pompe"
es-ES,"Arreglé la bomba PUMP guion diecisiete, tres horas",PUMP-17,3.0,"Arreglé la bomba"
de-DE,"Ventil Q fünf null null eins gewartet",Q-5001,null,"gewartet"
```

---

## 8) Deployment & Infrastructure

### 8.1 Azure Resources Required

```yaml
Resources:

  # 1. App Service
  - Type: Microsoft.Web/sites
    Name: workorder-api-prod
    SKU: P2v3 (2 cores, 8GB RAM)
    Instances: 2 (auto-scale 2-5)

  # 2. Azure Cognitive Services (Speech)
  - Type: Microsoft.CognitiveServices/accounts
    Kind: SpeechServices
    SKU: S0 (Standard)
    Location: westeurope

  # 3. Azure OpenAI
  - Type: Microsoft.CognitiveServices/accounts
    Kind: OpenAI
    Deployment: gpt-4o-mini (10K TPM quota)

  # 4. Storage Account (Blob)
  - Type: Microsoft.Storage/storageAccounts
    Container: wo-audio

  # 5. SQL Database
  - Type: Microsoft.Sql/servers/databases
    SKU: S3 (100 DTU)

  # 6. Application Insights
  - Type: Microsoft.Insights/components
```

---

## 9) Rollout Plan (Phased)

### Phase 1: Internal Beta (Weeks 1-2)
- Deploy to **dev environment**
- Enable for **5 pilot engineers**
- **Success criteria**: 80% asset resolution, <5s latency

### Phase 2: Limited Production (Weeks 3-4)
- Deploy to **prod** with feature flag (10% of users)
- **Success criteria**: 85% asset resolution, <10% fallback rate

### Phase 3: General Availability (Week 5+)
- Ramp up to **100% of users**
- **Success criteria**: 90% asset resolution, <5% correction rate

---

## 10) Appendix: Localization Reference

### Supported Locales

| Locale | Language | Hours Keywords | Dash Keywords | Number Words (0-9) |
|--------|----------|----------------|---------------|-------------------|
| en-GB | English | hours, hrs, h | dash, hyphen | zero, one, two, three, four, five, six, seven, eight, nine |
| sv-SE | Swedish | timmar, tim, h | bindestreck, streck | noll, ett, två, tre, fyra, fem, sex, sju, åtta, nio |
| fr-FR | French | heures, h | tiret | zéro, un, deux, trois, quatre, cinq, six, sept, huit, neuf |
| es-ES | Spanish | horas, h | guion | cero, uno, dos, tres, cuatro, cinco, seis, siete, ocho, nueve |
| de-DE | German | Stunden, Std, h | Bindestrich | null, eins, zwei, drei, vier, fünf, sechs, sieben, acht, neun |

---

**END OF PLAYBOOK**
