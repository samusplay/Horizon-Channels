namespace Horizon.Channels.Features.Channels.UploadBanner;

// ---------- lo que queremos hacer ----------
public sealed record UploadBannerCommand(
    Guid ChannelId,
    Guid UserId,          // quién lo pide (sale del JWT)
    Stream Content,       // la imagen
    string ContentType,   // image/jpeg, image/png o image/webp
    long Length)          // tamaño en bytes, para el validador
    : ICommand<UploadBannerResult>;

// ---------- lo que devolvemos ----------
public sealed record UploadBannerResult(Guid ChannelId, string BannerUrl);

// ---------- quien lo ejecuta ----------
internal sealed class UploadBannerHandler(
    ChannelsDbContext db,
    IFileStorage storage,
    ILogger<UploadBannerHandler> logger)
    : ICommandHandler<UploadBannerCommand, UploadBannerResult>
{
    public async Task<UploadBannerResult> Handle(UploadBannerCommand cmd, CancellationToken ct)
    {
        // 1. el canal debe existir
        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == cmd.ChannelId, ct)
            ?? throw new NotFoundException("El canal no existe.");

        // 2. autorización por recurso: solo el dueño cambia el banner
        if (!channel.IsOwnedBy(cmd.UserId))
            throw new ForbiddenException("Solo el dueño del canal puede cambiar su banner.");

        // 3. primero subimos la imagen nueva
        var newUrl = await storage.UploadBannerAsync(channel.Id, cmd.Content, cmd.ContentType, ct);

        // 4. la entidad cambia su banner y nos devuelve el anterior
        var previousUrl = channel.ChangeBanner(newUrl);

        // 5. guardamos en la base; si falla, borramos la imagen recién subida (compensación)
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await storage.DeleteBannerAsync(newUrl, CancellationToken.None);
            throw;
        }

        // 6. borramos el banner anterior; si falla, no rompemos la petición
        if (previousUrl is not null)
        {
            try
            {
                await storage.DeleteBannerAsync(previousUrl, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo borrar el banner anterior {BannerUrl}", previousUrl);
            }
        }

        return new UploadBannerResult(channel.Id, newUrl);
    }
}