# Implementation TODO List

## High Priority (Core Functionality)

### 1. Azure Speech Service Implementation
**File**: `src/WorkOrder.Transcription.Infrastructure/Services/AzureSpeechService.cs`

- [ ] Implement `ISpeechToTextService` interface
- [ ] Configure `SpeechConfig` with subscription key and region
- [ ] Add phrase list support per locale
- [ ] Implement single-shot recognition (`RecognizeOnceAsync`)
- [ ] Handle `ResultReason` cases (RecognizedSpeech, NoMatch, Canceled)
- [ ] Add retry logic with Polly
- [ ] Return `SpeechRecognitionResult` with confidence scores

**Reference**: See playbook section 3.2 for complete implementation

---

### 2. Azure OpenAI Service Implementation
**File**: `src/WorkOrder.Transcription.Infrastructure/Services/AzureOpenAIService.cs`

- [ ] Implement `ILlmFieldExtractionService` interface
- [ ] Configure `OpenAIClient` with endpoint and API key
- [ ] Build strict JSON extraction prompt
- [ ] Enable JSON response format mode
- [ ] Parse and validate LLM response
- [ ] Add timeout and retry policies
- [ ] Handle rate limiting (429 errors)

**Reference**: See playbook section 6.2 for complete implementation

---

### 3. Database Migration
**Location**: `src/WorkOrder.Transcription.Infrastructure/Migrations/`

- [ ] Run `dotnet ef migrations add InitialCreate`
- [ ] Review generated migration SQL
- [ ] Apply migration to development database
- [ ] Seed sample asset data (TX-482, PUMP-17, Q-5001, ROLLER A-5)
- [ ] Create indexes for performance

**Commands**:
```bash
cd src/WorkOrder.Transcription.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../WorkOrder.Transcription.Api
dotnet ef database update --startup-project ../WorkOrder.Transcription.Api
```

---

## Medium Priority (Testing & Validation)

### 4. Unit Tests
**Location**: `tests/WorkOrder.Transcription.Tests/`

- [x] `TextCanonicalizerTests` (completed)
- [x] `AssetIdExtractorTests` (completed)
- [ ] `HoursExtractorTests` - Test all locales
- [ ] `CatalogueResolverTests` - Exact + fuzzy matching
- [ ] `FieldExtractionAgentTests` - End-to-end extraction
- [ ] Mock repository tests

---

### 5. Integration Tests
**Location**: `tests/WorkOrder.Transcription.Tests/Integration/`

- [ ] Create `TranscriptionIntegrationTests.cs`
- [ ] Add test audio files to `tests/TestData/`
- [ ] Implement golden dataset tests (6 samples from playbook)
- [ ] Test multi-language support
- [ ] Test error scenarios (invalid audio, timeout, etc.)

**Golden dataset locales**: en-GB, sv-SE, fr-FR, es-ES, de-DE

---

### 6. API Testing
**Tool**: Postman / Swagger / xUnit

- [ ] Test `/api/workorders/{id}/transcribe` endpoint
- [ ] Test validation (missing audio, invalid locale, file size)
- [ ] Test error responses (404, 422, 500)
- [ ] Test health check endpoint
- [ ] Load test with k6 (script in playbook section 7.2)

---

## Low Priority (Production Readiness)

### 7. Configuration & Secrets
**File**: `appsettings.json`

- [ ] Replace placeholder Azure keys with real credentials
- [ ] Configure Application Insights connection string
- [ ] Set up Azure Key Vault integration
- [ ] Configure rate limiting thresholds
- [ ] Set CORS allowed origins

---

### 8. Monitoring & Telemetry
**Location**: `src/WorkOrder.Transcription.Infrastructure/Telemetry/`

- [ ] Create `TranscriptionTelemetry.cs` service
- [ ] Add custom Application Insights events
- [ ] Track metrics: confidence, fallback rate, latency
- [ ] Implement KQL queries for dashboards
- [ ] Configure alerts (error rate, latency, cost)

**Reference**: See playbook section 11 for KQL queries

---

### 9. Documentation
**Location**: `docs/`

- [x] Implementation playbook (completed)
- [ ] API documentation (OpenAPI/Swagger descriptions)
- [ ] Architecture diagrams (draw.io / Mermaid)
- [ ] Deployment guide
- [ ] Troubleshooting guide

---

### 10. DevOps & Deployment
**Location**: Root directory

- [ ] Create `azure-pipelines.yml` (see playbook section 10.2)
- [ ] Create Bicep/Terraform for Azure resources
- [ ] Configure blue-green deployment slots
- [ ] Set up staging environment
- [ ] Create rollback procedure

---

## Optional Enhancements (Future)

### 11. Advanced Features
- [ ] Real-time streaming transcription (WebSockets)
- [ ] Multi-speaker diarization
- [ ] Offline mode with Whisper
- [ ] Voice commands ("assign to John")
- [ ] Custom Speech model training
- [ ] Clarification loop (incremental updates)

---

### 12. Client Implementation
**Technology**: React / MAUI

- [ ] Audio recording component (MediaRecorder API)
- [ ] Upload progress indicator
- [ ] Auto-fill form fields with transcription results
- [ ] Confidence banner ("Review before submitting")
- [ ] Retry / clarify button

---

## Notes

- **Priority order**: Azure services → Database → Testing → Production readiness
- **Timeline estimate**: 2-3 weeks for MVP (items 1-6)
- **Blockers**: Azure subscription required for Speech + OpenAI services
- **References**: All implementation details in `docs/Speech-to-WorkOrder-Playbook.md`

---

## Progress Tracking

| Category | Status | Progress |
|----------|--------|----------|
| Core Services | ⚠️ In Progress | 60% (regex done, Azure services pending) |
| Database | ⚠️ Pending | 0% (EF Core configured, migration needed) |
| Testing | ⚠️ Partial | 20% (unit tests started) |
| API | ✅ Complete | 100% |
| Documentation | ✅ Complete | 100% |
| DevOps | ⚠️ Pending | 0% |

**Overall Progress**: 50% (Boilerplate complete, Azure integration needed)
