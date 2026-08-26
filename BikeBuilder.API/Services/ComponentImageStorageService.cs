using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BikeBuilder.API.Services;

public class ComponentImageStorageService(BlobContainerClient container)
{
    public async Task<string> UploadAsync(int componentId, string extension, Stream content, string contentType, CancellationToken ct)
    {
        var blobName = $"{componentId}/{Guid.NewGuid()}{extension}";
        await container.GetBlobClient(blobName).UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return blobName;
    }

    public Task DeleteAsync(string blobName, CancellationToken ct) =>
        container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);

    public async Task<(Stream Stream, string ContentType)> OpenReadAsync(string blobName, string contentType, CancellationToken ct)
    {
        var download = await container.GetBlobClient(blobName).DownloadStreamingAsync(cancellationToken: ct);
        return (download.Value.Content, contentType);
    }
}
