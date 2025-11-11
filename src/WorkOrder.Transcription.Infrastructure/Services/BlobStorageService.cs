using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using WorkOrder.Transcription.Application.Interfaces;
using WorkOrder.Transcription.Infrastructure.Options;

namespace WorkOrder.Transcription.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageOptions _options;

    public BlobStorageService(IOptions<BlobStorageOptions> options)
    {
        _options = options.Value;
        _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
    }

    public async Task<string> SaveAudioAsync(
        Guid workOrderId,
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobPath = $"{workOrderId}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobPath);

        await blobClient.UploadAsync(audioStream, overwrite: true, cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string audioUri, CancellationToken cancellationToken = default)
    {
        var blobClient = new BlobClient(new Uri(audioUri));
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
