using Microsoft.EntityFrameworkCore;
using WorkOrder.Transcription.Api.Middleware;
using WorkOrder.Transcription.Application.Interfaces;
using WorkOrder.Transcription.Application.Options;
using WorkOrder.Transcription.Application.Services;
using WorkOrder.Transcription.Infrastructure.Data;
using WorkOrder.Transcription.Infrastructure.Options;
using WorkOrder.Transcription.Infrastructure.Repositories;
using WorkOrder.Transcription.Infrastructure.Services;
using WorkOrder.Transcription.Infrastructure.Services.FieldExtraction;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Work Order Transcription API",
        Version = "v1",
        Description = "Speech-to-text API for work order creation"
    });
});

// Database
builder.Services.AddDbContext<WorkOrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Options
builder.Services.Configure<TranscriptionOptions>(
    builder.Configuration.GetSection("Transcription"));
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection("BlobStorage"));

// Repositories
builder.Services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
builder.Services.AddScoped<ITranscriptRepository, TranscriptRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();

// Application Services
builder.Services.AddScoped<WorkOrderTranscriptionService>();
builder.Services.AddScoped<IFieldExtractionAgent, FieldExtractionAgent>();
builder.Services.AddScoped<ICatalogueResolverService, CatalogueResolverService>();

// Infrastructure Services
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// NOTE: Azure Speech and OpenAI services need to be implemented
// builder.Services.AddScoped<ISpeechToTextService, AzureSpeechService>();
// builder.Services.AddScoped<ILlmFieldExtractionService, AzureOpenAIService>();

// Caching
builder.Services.AddMemoryCache();

// Exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Application Insights
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration.GetSection("ApplicationInsights"));

// CORS (configure as needed)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseExceptionHandler();

app.UseAuthorization();

app.MapControllers();

app.Run();
