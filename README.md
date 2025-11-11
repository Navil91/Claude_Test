# Work Order Transcription - Speech-to-Text System

## Overview

This is a complete .NET 8 implementation of the **Speech-to-Work-Order** automation system. It enables engineers to create work orders via voice dictation, with automatic extraction of Asset IDs, hours, and comments.

## Architecture

The solution follows **Clean Architecture** principles with four main layers:

```
WorkOrderTranscription/
├── src/
│   ├── WorkOrder.Transcription.Domain/       # Entities, value objects
│   ├── WorkOrder.Transcription.Application/  # Business logic, interfaces, DTOs
│   ├── WorkOrder.Transcription.Infrastructure/ # Azure services, repositories
│   └── WorkOrder.Transcription.Api/          # REST API, controllers
├── tests/
│   └── WorkOrder.Transcription.Tests/        # Unit & integration tests
└── docs/
    └── Speech-to-WorkOrder-Playbook.md       # Detailed implementation guide
```

## Key Features

✅ **Multi-language support**: en-GB, sv-SE, fr-FR, es-ES, de-DE
✅ **Regex-first extraction**: Fast, deterministic asset ID parsing
✅ **LLM fallback**: Azure OpenAI for low-confidence cases
✅ **Fuzzy matching**: Levenshtein-based asset catalogue resolution
✅ **Audit trail**: All transcripts + audio stored
✅ **Comprehensive error handling**: Custom exception middleware

## Technology Stack

- **.NET 8** (C#)
- **Azure Cognitive Services** (Speech-to-Text)
- **Azure OpenAI** (GPT-4o-mini for fallback)
- **Azure Blob Storage** (Audio persistence)
- **SQL Server** (Entity Framework Core)
- **ASP.NET Core** (Web API)

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB for development)
- Azure subscription (for Speech + OpenAI services)
- Azurite (optional, for local blob storage)

### Configuration

1. **Update appsettings.json** with your Azure credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_SQL_CONNECTION_STRING"
  },
  "AzureSpeech": {
    "SubscriptionKey": "YOUR_KEY",
    "Region": "westeurope"
  },
  "AzureOpenAI": {
    "Endpoint": "https://YOUR_RESOURCE.openai.azure.com/",
    "ApiKey": "YOUR_KEY"
  },
  "BlobStorage": {
    "ConnectionString": "YOUR_BLOB_CONNECTION_STRING"
  }
}
```

2. **Run database migrations**:

```bash
cd src/WorkOrder.Transcription.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../WorkOrder.Transcription.Api
dotnet ef database update --startup-project ../WorkOrder.Transcription.Api
```

3. **Start the API**:

```bash
cd src/WorkOrder.Transcription.Api
dotnet run
```

API will be available at: `https://localhost:7000` (check console output)

### API Endpoints

#### Transcribe Audio

```http
POST /api/workorders/{id}/transcribe?locale=en-GB
Content-Type: multipart/form-data

{
  "audio": <file.wav>
}
```

**Response:**

```json
{
  "workOrderId": "guid",
  "text": "Fixed pump TX-482, took two hours",
  "fields": {
    "asset": "TX-482",
    "comment": "Fixed pump TX-482, took two hours",
    "hours": 2.0,
    "confidence": 0.95
  },
  "audioUri": "https://...",
  "metadata": {
    "vendor": "AzureSpeech",
    "language": "en-GB",
    "durationSeconds": 15,
    "usedLlmFallback": false,
    "processingTimeMs": 1234
  }
}
```

## Project Structure

### Domain Layer

- **Entities**: `WorkOrder`, `Transcript`, `Asset`
- **Value Objects**: `WorkOrderFields`, `AssetResolution`, `TranscriptionMetadata`

### Application Layer

- **Services**: `WorkOrderTranscriptionService`
- **Interfaces**: Service contracts (STT, LLM, repositories)
- **DTOs**: Request/response models
- **Exceptions**: Custom exception types

### Infrastructure Layer

- **Data**: `WorkOrderDbContext`, EF Core configurations
- **Repositories**: `WorkOrderRepository`, `AssetRepository`, `TranscriptRepository`
- **Services**:
  - `FieldExtractionAgent` (regex-based extraction)
  - `TextCanonicalizer` (number words → digits)
  - `AssetIdExtractor` (multi-pattern regex)
  - `HoursExtractor` (locale-aware keywords)
  - `CatalogueResolverService` (exact + fuzzy matching)
  - `BlobStorageService` (Azure Blob integration)
  - *(TODO: `AzureSpeechService`, `AzureOpenAIService`)*

### API Layer

- **Controllers**: `TranscriptionController`
- **Middleware**: `GlobalExceptionHandler`
- **Configuration**: Dependency injection, options patterns

## Testing

Run unit tests:

```bash
cd tests/WorkOrder.Transcription.Tests
dotnet test
```

Example tests to implement:

- `TextCanonicalizerTests`: Verify number word conversion
- `AssetIdExtractorTests`: Test regex patterns
- `CatalogueResolverTests`: Exact + fuzzy matching
- `HoursExtractorTests`: Multi-locale hour extraction

## Implementation Status

| Component | Status | Notes |
|-----------|--------|-------|
| Domain models | ✅ Complete | |
| Application interfaces | ✅ Complete | |
| Database context | ✅ Complete | |
| Repositories | ✅ Complete | |
| Field extraction (regex) | ✅ Complete | |
| Catalogue resolver | ✅ Complete | |
| Blob storage | ✅ Complete | |
| API controller | ✅ Complete | |
| Azure Speech service | ⚠️ TODO | See playbook for implementation |
| Azure OpenAI service | ⚠️ TODO | See playbook for implementation |
| Unit tests | ⚠️ Partial | Golden dataset tests needed |

## Next Steps

### 1. Implement Azure Services

Create the following services in `Infrastructure`:

- **AzureSpeechService.cs**: Implement `ISpeechToTextService`
  - Use `Microsoft.CognitiveServices.Speech` SDK
  - Add phrase lists per locale
  - Handle recognition errors

- **AzureOpenAIService.cs**: Implement `ILlmFieldExtractionService`
  - Use `Azure.AI.OpenAI` SDK
  - Strict JSON extraction prompt
  - Retry logic with Polly

### 2. Database Migration

Create initial migration:

```bash
dotnet ef migrations add AddTranscriptionSupport \
  --project src/WorkOrder.Transcription.Infrastructure \
  --startup-project src/WorkOrder.Transcription.Api
```

### 3. Seed Data

Add sample assets to the database:

```sql
INSERT INTO Assets (Id, AssetId, AssetName, AssetType, IsActive)
VALUES
  (NEWID(), 'TX-482', 'Transformer 482', 'Electrical', 1),
  (NEWID(), 'PUMP-17', 'Main Pump 17', 'Hydraulic', 1),
  (NEWID(), 'Q-5001', 'Quality Sensor 5001', 'Sensor', 1),
  (NEWID(), 'ROLLER A-5', 'Conveyor Roller A-5', 'Mechanical', 1);
```

### 4. Testing

- Add golden dataset audio files to `tests/TestData/`
- Implement integration tests for E2E transcription
- Load test with k6 (script in playbook)

### 5. Deployment

See `docs/Speech-to-WorkOrder-Playbook.md` section 10 for:

- Azure infrastructure (Bicep/Terraform)
- CI/CD pipeline (Azure DevOps YAML)
- Blue-green deployment strategy
- Monitoring & alerts

## Documentation

- **Detailed Playbook**: `docs/Speech-to-WorkOrder-Playbook.md`
  - Complete implementation guide
  - Code samples for all components
  - Testing strategy
  - Deployment instructions
  - Monitoring & KPIs

## Support & Contribution

For questions or contributions, please refer to the playbook documentation.

## License

*(Add your license here)*
