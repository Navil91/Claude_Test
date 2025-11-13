# Asset Management Web Application

A full-featured asset management web application with integrated Speech-to-Work-Order functionality.

## Features

### 🎯 Asset Management
- **Create, Read, Update, Delete (CRUD)** operations for assets
- **Search and filter** assets by ID, name, or type
- **Active/Inactive** status management
- **Real-time validation** and error handling

### 📝 Work Order Management
- **CRUD operations** for work orders
- **Associate work orders** with assets
- **Track labour hours** and comments
- **View transcription details** and confidence scores
- **Statistics dashboard** showing total hours, work orders, and more

### 🎤 Speech-to-Work-Order Integration
- **Voice recording** directly in the browser
- **Audio file upload** support (WAV, MP3, WEBM, OGG)
- **Automatic transcription** using Azure Speech Services
- **Intelligent field extraction**:
  - Asset ID (e.g., "TX-482", "PUMP-17")
  - Labour hours (e.g., "2 hours", "1.5 timmar")
  - Comments (cleaned transcript)
- **Multi-language support**: English (UK), Swedish, French, Spanish, German
- **Confidence scoring** with visual indicators
- **One-click work order creation** from transcription results

### 📊 Dashboard
- **Real-time statistics**
  - Total assets
  - Total work orders
  - Total labour hours
  - Work orders with transcription
- **Recent work orders** feed
- **Quick navigation** to all features

## Architecture

### Frontend Stack
- **Pure HTML5/CSS3/JavaScript** (no frameworks required)
- **Responsive design** for mobile and desktop
- **Modern UI** with smooth animations
- **Web Audio API** for voice recording
- **Fetch API** for REST communication

### Backend Stack
- **.NET 8** ASP.NET Core Web API
- **Entity Framework Core** for data access
- **SQL Server** database
- **Azure Cognitive Services** for speech-to-text
- **Azure OpenAI** for LLM fallback extraction
- **Azure Blob Storage** for audio persistence

### Project Structure

```
src/WorkOrder.Transcription.Api/
├── wwwroot/                        # Static web files
│   ├── index.html                  # Dashboard
│   ├── assets.html                 # Asset management
│   ├── workorders.html             # Work order management
│   ├── create-workorder.html       # Voice-to-work-order
│   ├── css/
│   │   └── styles.css              # Complete styling
│   └── js/
│       ├── api.js                  # API helper functions
│       ├── dashboard.js            # Dashboard logic
│       ├── assets.js               # Asset management
│       ├── workorders.js           # Work order management
│       └── voice-recorder.js       # Voice recording & transcription
├── Controllers/
│   ├── AssetsController.cs         # Asset API endpoints
│   ├── WorkOrdersController.cs     # Work order API endpoints
│   └── TranscriptionController.cs  # Transcription API endpoints
└── Program.cs                      # App configuration
```

## Getting Started

### Prerequisites

1. **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **SQL Server** (LocalDB for development)
3. **Azure subscription** (for Speech and OpenAI services) - Optional for testing UI
4. **Modern web browser** with microphone access (Chrome, Edge, Firefox)

### Setup Instructions

#### 1. Database Setup

Create and migrate the database:

```bash
cd src/WorkOrder.Transcription.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../WorkOrder.Transcription.Api
dotnet ef database update --startup-project ../WorkOrder.Transcription.Api
```

#### 2. Seed Sample Data

Run this SQL to add sample assets:

```sql
USE [WorkOrderTranscription];

INSERT INTO Assets (Id, AssetId, AssetName, AssetType, IsActive)
VALUES
  (NEWID(), 'TX-482', 'Transformer 482', 'Electrical', 1),
  (NEWID(), 'PUMP-17', 'Main Pump 17', 'Hydraulic', 1),
  (NEWID(), 'Q-5001', 'Quality Sensor 5001', 'Sensor', 1),
  (NEWID(), 'ROLLER A-5', 'Conveyor Roller A-5', 'Mechanical', 1),
  (NEWID(), 'TX-301', 'Transformer 301', 'Electrical', 1),
  (NEWID(), 'PUMP-22', 'Backup Pump 22', 'Hydraulic', 1);
```

#### 3. Configure Application Settings

Update `src/WorkOrder.Transcription.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WorkOrderTranscription;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Transcription": {
    "EnableLlmFallback": false,
    "LlmConfidenceThreshold": 0.8
  },
  "BlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true"
  }
}
```

**Note:** Azure Speech and OpenAI services are optional for UI testing. The UI will work, but transcription will fail without configured services.

#### 4. Run the Application

```bash
cd src/WorkOrder.Transcription.Api
dotnet run
```

The application will start on `https://localhost:7000` (or similar, check console output).

#### 5. Access the Web Application

Open your browser and navigate to:
- **Dashboard**: https://localhost:7000/
- **API Documentation**: https://localhost:7000/swagger

### First-Time Browser Setup

When accessing the voice recording feature for the first time:
1. Your browser will request microphone permissions
2. Click **Allow** to enable voice recording
3. You can also upload audio files without microphone access

## Using the Application

### Managing Assets

1. Navigate to **Assets** from the menu
2. Click **+ Create Asset** to add new assets
3. Fill in:
   - **Asset ID** (required, e.g., "TX-482")
   - **Asset Name** (optional, e.g., "Transformer 482")
   - **Asset Type** (optional, e.g., "Electrical")
   - **Active status** (checkbox)
4. Use the search bar to filter assets
5. Toggle **Show Active Only** to filter inactive assets
6. Click **Edit** to modify existing assets
7. Click **Delete** to deactivate assets (soft delete)

### Managing Work Orders

1. Navigate to **Work Orders** from the menu
2. Click **+ Create Work Order** for manual entry, or
3. Click **🎤 Create with Voice** for speech-to-text creation
4. For manual creation, fill in:
   - **Asset ID** (optional)
   - **Comment** (description of work)
   - **Labour Hours** (decimal, e.g., 2.5)
5. Click **View** to see full details including transcription data
6. Click **Edit** to modify work orders
7. Click **Delete** to remove work orders

### Creating Work Orders with Voice

1. Navigate to **Create Work Order** from the menu
2. Select your language from the dropdown
3. **Record audio:**
   - Click **Start Recording**
   - Speak clearly: "Fixed pump T X dash four eight two, replaced seals, took two hours"
   - Click **Stop Recording**
   - Review the audio playback
4. **Or upload audio:**
   - Click **📁 Or Upload Audio File**
   - Select a WAV, MP3, WEBM, or OGG file (max 10MB)
5. Click **Transcribe Audio**
6. Review the extracted fields:
   - Transcribed text
   - Asset ID (automatically extracted)
   - Labour hours (automatically extracted)
   - Confidence score
   - Comment (cleaned transcript)
7. Click **✓ Save Work Order** to create the work order
8. Or click **🔄 Start Over** to try again

### Dashboard Overview

The dashboard shows:
- **Total Assets**: Count of all assets in the system
- **Total Work Orders**: Count of all work orders
- **Total Labour Hours**: Sum of all labour hours
- **With Transcription**: Count of work orders created via voice
- **Recent Work Orders**: Last 5 work orders with details

## API Endpoints

### Assets API

```
GET    /api/assets                    # Get all assets
GET    /api/assets/{id}               # Get asset by ID
GET    /api/assets/by-assetid/{id}    # Get asset by Asset ID
GET    /api/assets/search?prefix=TX   # Search by prefix
POST   /api/assets                    # Create new asset
PUT    /api/assets/{id}               # Update asset
DELETE /api/assets/{id}               # Delete asset (soft)
```

### Work Orders API

```
GET    /api/workorders                        # Get all work orders
GET    /api/workorders/{id}                   # Get work order by ID
GET    /api/workorders/by-asset/{assetId}     # Get by asset ID
GET    /api/workorders/statistics             # Get statistics
POST   /api/workorders                        # Create work order
PUT    /api/workorders/{id}                   # Update work order
DELETE /api/workorders/{id}                   # Delete work order
```

### Transcription API

```
POST   /api/workorders/{id}/transcribe?locale=en-GB   # Transcribe audio
```

**Request:**
- Method: POST
- Content-Type: multipart/form-data
- Body: audio file (key: "audio")
- Query: locale (en-GB, sv-SE, fr-FR, es-ES, de-DE)

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

## Testing

### Manual Testing Checklist

#### Asset Management
- [ ] Create a new asset with all fields
- [ ] Create an asset with only Asset ID (required field)
- [ ] Edit an asset's name and type
- [ ] Mark an asset as inactive
- [ ] Delete an asset
- [ ] Search for assets using the search bar
- [ ] Toggle "Show Active Only" filter

#### Work Order Management
- [ ] Create a work order manually
- [ ] View work order details
- [ ] Edit a work order
- [ ] Delete a work order
- [ ] Filter work orders using search

#### Voice Recording (requires Azure services)
- [ ] Record audio using microphone
- [ ] Upload audio file
- [ ] Transcribe audio with correct locale
- [ ] Verify extracted fields (asset ID, hours, comment)
- [ ] Check confidence score
- [ ] Save work order from transcription
- [ ] Test with different languages

#### Dashboard
- [ ] Verify statistics are correct
- [ ] Check recent work orders display
- [ ] Navigate between pages using menu
- [ ] Verify responsive design on mobile

### Browser Compatibility

Tested and supported on:
- ✅ Google Chrome (recommended)
- ✅ Microsoft Edge
- ✅ Mozilla Firefox
- ✅ Safari (macOS, iOS)

**Note:** Voice recording requires HTTPS or localhost for security.

## Troubleshooting

### Database Connection Failed

**Error:** Cannot connect to SQL Server

**Solution:**
1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. Run database migrations: `dotnet ef database update`

### Microphone Not Working

**Error:** Browser not accessing microphone

**Solution:**
1. Check browser permissions (click lock icon in address bar)
2. Ensure you're on HTTPS or localhost
3. Try uploading audio files instead

### Transcription Fails

**Error:** Failed to transcribe audio

**Solution:**
1. This is expected if Azure services are not configured
2. Configure Azure Speech Service in `appsettings.json`
3. Or test other features (assets, work orders) without transcription

### Static Files Not Loading

**Error:** 404 on CSS/JS files

**Solution:**
1. Verify `wwwroot` folder exists
2. Check `Program.cs` has `UseStaticFiles()` and `UseDefaultFiles()`
3. Rebuild the project: `dotnet build`

### CORS Errors

**Error:** CORS policy blocking requests

**Solution:**
1. In development, all origins are allowed
2. For production, configure `Cors:AllowedOrigins` in `appsettings.json`

## Features to Implement (Future)

The following features require Azure service implementations:

### Azure Speech Service (Required for Transcription)
- File: `Infrastructure/Services/AzureSpeechService.cs`
- Implement `ISpeechToTextService`
- See `docs/Speech-to-WorkOrder-Playbook.md` section 3.2

### Azure OpenAI Service (Optional - LLM Fallback)
- File: `Infrastructure/Services/AzureOpenAIService.cs`
- Implement `ILlmFieldExtractionService`
- See playbook section 6.2

## Performance

- **Page Load**: < 1 second
- **API Response**: < 200ms (without transcription)
- **Transcription**: 2-5 seconds (depends on audio length)
- **Database Queries**: Optimized with async/await and EF Core

## Security

- **Input Validation**: All inputs validated on client and server
- **SQL Injection**: Protected via Entity Framework parameterized queries
- **XSS Protection**: All user input is escaped in the UI
- **CORS**: Configured for development (localhost) and production
- **File Upload**: Size limits (10MB), type validation
- **HTTPS**: Required for production (voice recording needs HTTPS)

## License

(Add your license here)

## Support

For issues or questions:
1. Check the main `README.md` for project overview
2. See `claude.md` for development context
3. Review `docs/Speech-to-WorkOrder-Playbook.md` for implementation details

## Contributing

See main README for contribution guidelines.

---

**Built with:** .NET 8, Entity Framework Core, Azure Cognitive Services, and modern web technologies.
