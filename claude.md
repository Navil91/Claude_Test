# Claude Context: Speech-to-Work-Order System

## Project Overview

This is a .NET 8 speech-to-text system for work order creation. Engineers speak into their device, and the system automatically extracts:
- **Asset ID** (e.g., "TX-482", "PUMP-17", "ROLLER A-5")
- **Hours** (e.g., "2 hours", "1.5 timmar")
- **Comment** (cleaned transcript)

**Design Philosophy**: **Regex-first (free, deterministic) → LLM fallback (only when needed)** for cost efficiency.

## Architecture

### Clean Architecture Layers

```
┌─────────────────────────────────────────┐
│         API Layer (Controllers)         │  ← REST endpoints, validation
├─────────────────────────────────────────┤
│    Application Layer (Use Cases)        │  ← Orchestration, DTOs, interfaces
├─────────────────────────────────────────┤
│   Infrastructure Layer (Impl)           │  ← Azure services, EF Core, regex
├─────────────────────────────────────────┤
│      Domain Layer (Entities)            │  ← Core business models
└─────────────────────────────────────────┘
```

**Dependencies flow inward**: Infrastructure/API depend on Application → Application depends on Domain.

### Key Design Patterns

1. **Repository Pattern**: Abstract data access (`IAssetRepository`, `IWorkOrderRepository`)
2. **Dependency Injection**: All services registered in `Program.cs`
3. **Options Pattern**: Configuration bound to strongly-typed classes (`TranscriptionOptions`)
4. **Strategy Pattern**: Field extraction (regex → fallback to LLM)

## Critical Files

### Entry Points
- **`src/WorkOrder.Transcription.Api/Program.cs`**: DI container, middleware pipeline
- **`src/WorkOrder.Transcription.Api/Controllers/TranscriptionController.cs`**: Main API endpoint

### Core Business Logic
- **`src/WorkOrder.Transcription.Application/Services/WorkOrderTranscriptionService.cs`**:
  - Orchestrates entire transcription flow
  - Handles STT → extraction → validation → persistence
  - **IMPORTANT**: Main service with 9-step process (read comments in code)

### Field Extraction (Regex-Based)
- **`Infrastructure/Services/FieldExtraction/TextCanonicalizer.cs`**:
  - Converts "T X dash four eight two" → "TX-482"
  - Locale-aware number word replacement
- **`Infrastructure/Services/FieldExtraction/AssetIdExtractor.cs`**:
  - 4 regex patterns (standard, no-dash, roller, spaced)
- **`Infrastructure/Services/FieldExtraction/HoursExtractor.cs`**:
  - Multi-language keywords (hours/timmar/heures/horas/Stunden)

### Asset Resolution
- **`Infrastructure/Services/CatalogueResolverService.cs`**:
  - Tier 1: Exact match (normalized)
  - Tier 2: Fuzzy match (Levenshtein distance, prefix-constrained)
  - **THRESHOLD**: 90% similarity required

### Data Layer
- **`Infrastructure/Data/WorkOrderDbContext.cs`**: EF Core context
- **`Infrastructure/Repositories/*`**: Data access implementations

## Important Conventions

### 1. Supported Locales
```csharp
"en-GB", "sv-SE", "fr-FR", "es-ES", "de-DE"
```
**Always validate locale** in API endpoints.

### 2. Asset ID Formats
Examples that should be recognized:
- `TX-482` (standard with dash)
- `Q5001` (no dash)
- `ROLLER A-5` (special roller format)
- `PUMP-17` (prefix-number)

**Normalization rule**: Always add dash between letters and numbers (`TX482` → `TX-482`)

### 3. Confidence Scoring
```
Base: 0.5
+ 0.3 if asset resolved (exact) OR +0.2 (fuzzy)
+ 0.15 if comment length ≥ 20 chars
+ 0.05 if hours found
= Total confidence (capped at 1.0)
```

**Thresholds**:
- `≥ 0.8`: High confidence (auto-apply)
- `0.6-0.8`: Medium (apply but flag for review)
- `< 0.6`: Low (don't auto-apply, user must review)

### 4. LLM Fallback Trigger
```csharp
if (confidence < 0.8 && EnableLlmFallback)
{
    // Call Azure OpenAI
}
```

Only triggered when regex extraction has low confidence (cost optimization).

## Pending Implementations

### ⚠️ HIGH PRIORITY: Azure Services (Not Yet Implemented)

#### 1. Azure Speech Service
**File to create**: `Infrastructure/Services/AzureSpeechService.cs`

**Implementation checklist**:
- [ ] Implement `ISpeechToTextService` interface
- [ ] Use `Microsoft.CognitiveServices.Speech` SDK
- [ ] Configure `SpeechRecognitionLanguage` from locale parameter
- [ ] Add phrase lists per locale (see `appsettings.json`)
- [ ] Handle `ResultReason` enum (RecognizedSpeech, NoMatch, Canceled)
- [ ] Return `SpeechRecognitionResult` with confidence scores

**Reference**: See `docs/Speech-to-WorkOrder-Playbook.md` section 3.2 for complete code.

#### 2. Azure OpenAI Service
**File to create**: `Infrastructure/Services/AzureOpenAIService.cs`

**Implementation checklist**:
- [ ] Implement `ILlmFieldExtractionService` interface
- [ ] Use `Azure.AI.OpenAI` SDK
- [ ] Build JSON extraction prompt (see playbook section 6.2)
- [ ] Enable `ChatCompletionsJsonResponseFormat`
- [ ] Parse JSON response into `LlmExtraction` record
- [ ] Add retry logic with Polly

**Prompt template location**: Playbook section 6.2

### ⚠️ MEDIUM PRIORITY: Database

#### Migration Needed
```bash
cd src/WorkOrder.Transcription.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../WorkOrder.Transcription.Api
dotnet ef database update --startup-project ../WorkOrder.Transcription.Api
```

#### Sample Seed Data
```sql
INSERT INTO Assets (Id, AssetId, AssetName, AssetType, IsActive) VALUES
  (NEWID(), 'TX-482', 'Transformer 482', 'Electrical', 1),
  (NEWID(), 'PUMP-17', 'Main Pump 17', 'Hydraulic', 1),
  (NEWID(), 'Q-5001', 'Quality Sensor 5001', 'Sensor', 1),
  (NEWID(), 'ROLLER A-5', 'Conveyor Roller A-5', 'Mechanical', 1);
```

## Common Development Tasks

### Running the API
```bash
cd src/WorkOrder.Transcription.Api
dotnet run
# API: https://localhost:7000
# Swagger: https://localhost:7000/swagger
```

### Running Tests
```bash
cd tests/WorkOrder.Transcription.Tests
dotnet test
```

### Testing the Endpoint (curl)
```bash
curl -X POST "https://localhost:7000/api/workorders/{GUID}/transcribe?locale=en-GB" \
  -H "Content-Type: multipart/form-data" \
  -F "audio=@test.wav"
```

### Adding a New Locale

1. **Update `TextCanonicalizer.cs`**:
   ```csharp
   NumberWords["it-IT"] = new[] { "zero", "uno", "due", ... };
   DashWords["it-IT"] = new[] { "trattino" };
   ```

2. **Update `HoursExtractor.cs`**:
   ```csharp
   HourKeywords["it-IT"] = new[] { "ore", "h" };
   ```

3. **Update `appsettings.json`**:
   ```json
   "PhraseLists": {
     "it-IT": ["TX", "PUMP", "ROLLER", "asset", "ore"]
   }
   ```

4. **Update `TranscriptionController.cs`**:
   ```csharp
   private static readonly string[] SupportedLocales = {
     "en-GB", "sv-SE", "fr-FR", "es-ES", "de-DE", "it-IT"
   };
   ```

## Debugging Tips

### Issue: Asset ID Not Extracted

**Check**:
1. Run text through canonicalizer manually:
   ```csharp
   var canonical = new TextCanonicalizer().Canonicalize(text, locale);
   Console.WriteLine(canonical); // Should show "TX-482" format
   ```
2. Check if asset exists in database with exact ID
3. Verify regex patterns match your format

### Issue: Low Confidence Score

**Possible causes**:
- Asset not in catalogue → +0 points
- Comment too short (< 20 chars) → Only +0.05 instead of +0.15
- No hours mentioned → -0.05 potential

**Solution**: Check `CalculateConfidence()` method in `WorkOrderTranscriptionService.cs`

### Issue: LLM Fallback Not Triggering

**Check**:
1. `appsettings.json`: `"EnableLlmFallback": true`
2. Confidence must be `< 0.8` (see `LlmThreshold` config)
3. Azure OpenAI service must be implemented

### Issue: Database Connection Failed

**Check**:
1. Connection string in `appsettings.json`
2. SQL Server running (LocalDB for dev)
3. Migration applied: `dotnet ef database update`

## Testing Strategy

### Unit Tests (Existing)
- ✅ `TextCanonicalizerTests`: Number word conversion
- ✅ `AssetIdExtractorTests`: Regex pattern matching

### Unit Tests (TODO)
- [ ] `HoursExtractorTests`: All locales
- [ ] `CatalogueResolverTests`: Exact + fuzzy matching
- [ ] `FieldExtractionAgentTests`: End-to-end extraction

### Integration Tests (TODO)
- [ ] Create `tests/TestData/` folder
- [ ] Add golden dataset audio files:
  - `test_audio_en_tx482.wav` → Expected: "TX-482", 1.5 hours
  - `test_audio_sv_pump17.wav` → Expected: "PUMP-17", null hours
  - ... (see playbook section 7.2 for full list)

### Load Tests (TODO)
- [ ] k6 script provided in playbook section 7.3
- [ ] Target: 100 concurrent users, P95 < 5s

## Configuration Secrets

### Development
Use `dotnet user-secrets`:
```bash
cd src/WorkOrder.Transcription.Api
dotnet user-secrets set "AzureSpeech:SubscriptionKey" "your-key"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key"
```

### Production
Use **Azure Key Vault**:
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

## Error Handling

### Exception Hierarchy
```
Exception
└── TranscriptionException (base)
    ├── SpeechRecognitionException (422)
    ├── AudioProcessingException (400)
    └── NotFoundException (404)
```

**Handler**: `Api/Middleware/GlobalExceptionHandler.cs`

### Custom Error Responses
All errors return:
```json
{
  "error": "Human-readable error",
  "detail": "Technical details",
  "validationErrors": { /* optional */ }
}
```

## Performance Considerations

### Caching Strategy
**Asset prefix lookups** are cached for 30 minutes:
```csharp
_cache.Set($"assets:prefix:{prefix}", assets, TimeSpan.FromMinutes(30));
```

**Why?** Fuzzy matching queries assets by prefix frequently. Caching reduces DB hits by ~90%.

### Cost Optimization
1. **Regex first**: Free, handles ~80-90% of cases
2. **LLM fallback**: Only ~10-20% of transcriptions
3. **Batch STT**: $0.003/min (cheaper than real-time)

**Target cost**: < $0.01 per work order

## Known Limitations

1. **No real-time streaming**: Uses batch recognition (simpler, cheaper)
2. **No speaker diarization**: Single speaker assumed
3. **No offline mode**: Requires Azure connectivity
4. **Max audio length**: 2 minutes (120 seconds)
5. **Max file size**: 10MB

## Future Enhancements (Post-MVP)

Documented in playbook section 15:
- [ ] Custom Speech model training (company jargon)
- [ ] Voice commands ("assign to John")
- [ ] Real-time streaming with WebSockets
- [ ] Offline mode with Whisper
- [ ] Multi-speaker diarization

## Key Resources

1. **Implementation Guide**: `docs/Speech-to-WorkOrder-Playbook.md` (1000+ lines)
2. **TODO List**: `TODO.md` (prioritized tasks)
3. **README**: `README.md` (quick start)
4. **Azure Speech Docs**: https://learn.microsoft.com/azure/cognitive-services/speech-service/
5. **Azure OpenAI Docs**: https://learn.microsoft.com/azure/ai-services/openai/

## Quick Reference: File Locations

```
Domain Models:              src/WorkOrder.Transcription.Domain/Entities/
Value Objects:              src/WorkOrder.Transcription.Domain/ValueObjects/
Service Interfaces:         src/WorkOrder.Transcription.Application/Interfaces/
DTOs:                       src/WorkOrder.Transcription.Application/DTOs/
Orchestration:              src/WorkOrder.Transcription.Application/Services/
Field Extraction:           src/WorkOrder.Transcription.Infrastructure/Services/FieldExtraction/
Repositories:               src/WorkOrder.Transcription.Infrastructure/Repositories/
Azure Services (TODO):      src/WorkOrder.Transcription.Infrastructure/Services/
API Controller:             src/WorkOrder.Transcription.Api/Controllers/
Configuration:              src/WorkOrder.Transcription.Api/appsettings.json
Tests:                      tests/WorkOrder.Transcription.Tests/
```

## Contributing Guidelines

### Code Style
- Use C# 12 features (records, pattern matching, etc.)
- Prefer `async/await` for all I/O operations
- Follow SOLID principles
- Add XML comments to public APIs

### Commit Messages
```
<type>: <short summary>

<detailed description>

<references>
```

**Types**: feat, fix, docs, test, refactor, perf

### Before Submitting PR
- [ ] Run `dotnet build` (no errors)
- [ ] Run `dotnet test` (all pass)
- [ ] Add/update tests for new features
- [ ] Update `TODO.md` if applicable
- [ ] Check for hardcoded secrets

## Emergency Contacts / Escalation

### System Down
1. Check Application Insights for errors
2. Verify Azure service quotas (Speech/OpenAI rate limits)
3. Check SQL connection pooling

### High Cost Alert
1. Review `UsedLlmFallback` metric (should be < 20%)
2. Check audio duration distribution (outliers?)
3. Verify caching is working (cache hit rate)

### Data Issues
1. Check `Transcripts` table for audit trail
2. Audio files stored in blob for 90 days
3. All operations logged to Application Insights

---

## TL;DR for Claude

**What works**: Regex-based extraction, fuzzy matching, DB setup, API endpoint
**What's missing**: Azure Speech + OpenAI implementations (see playbook sections 3.2 and 6.2)
**Next step**: Implement `AzureSpeechService.cs` and `AzureOpenAIService.cs`
**Critical file**: `WorkOrderTranscriptionService.cs` (main orchestrator)
**Testing**: Run `dotnet test`, add golden dataset files
**Docs**: Everything in `docs/Speech-to-WorkOrder-Playbook.md`
