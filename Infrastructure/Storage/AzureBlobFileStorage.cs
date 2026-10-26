using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace Horizon.Channels.Infrastructure.Storage;

// Implementa el puerto IFileStorage con Azure Blob Storage
internal sealed class AzureBlobFileStorage(
    BlobServiceClient blobService,
    IOptions<AzureStorageOptions> options) : IFileStorage
{
    private readonly AzureStorageOptions _options = options.Value;

    // tipos de imagen aceptados y la extensión con la que se guardan
    private static readonly Dictionary<string, string> Extensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
        };

    public async Task<string> UploadBannerAsync(
        Guid channelId, Stream content, string contentType, CancellationToken ct)
    {
        if (!Extensions.TryGetValue(contentType, out var extension))
            throw new ArgumentException($"Tipo de imagen no soportado: {contentType}", nameof(contentType));

        // nombre único en cada subida: {idDelCanal}/{guid}.jpg
        var container = blobService.GetBlobContainerClient(_options.BannersContainer);
        var blob = container.GetBlobClient($"{channelId}/{Guid.NewGuid()}{extension}");

        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType,                               // el navegador lo muestra como imagen
                CacheControl = "public, max-age=31536000, immutable",    // se puede cachear un año
            },
        }, ct);

        return blob.Uri.ToString();
    }

    public async Task DeleteBannerAsync(string bannerUrl, CancellationToken ct)
    {
        // de la URL completa sacamos solo el nombre del archivo dentro del contenedor
        var blobName = new BlobUriBuilder(new Uri(bannerUrl)).BlobName;

        var container = blobService.GetBlobContainerClient(_options.BannersContainer);
        await container.DeleteBlobIfExistsAsync(blobName, cancellationToken: ct);
    }
}