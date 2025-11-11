namespace WorkOrder.Transcription.Infrastructure.Options;

public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "wo-audio";
}
