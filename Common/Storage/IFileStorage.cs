namespace Horizon.Channels.Common.Storage;

// Lo que los casos de uso necesitan del almacenamiento de archivos,
// sin saber si detrás está Azure, S3 o el disco
public interface IFileStorage
{
    // sube el banner de un canal y devuelve su URL pública
    Task<string> UploadBannerAsync(
        Guid channelId, Stream content, string contentType, CancellationToken ct);

    // borra un banner anterior a partir de su URL (si ya no existe, no pasa nada)
    Task DeleteBannerAsync(string bannerUrl, CancellationToken ct);
}