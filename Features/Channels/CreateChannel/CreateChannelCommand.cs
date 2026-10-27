using Horizon.Channels.Common.Messaging;
using Horizon.Channels.Common.Messaging.Events;

namespace Horizon.Channels.Features.Channels.CreateChannel;

// ---------- lo que queremos hacer ----------
public sealed record CreateChannelCommand(
    Guid OwnerId,          // sale del JWT, nunca del cuerpo de la petición
    string OwnerEmail,     // también sale del JWT (claim "email"): a dónde va el correo
    string Name,
    string Handle,
    string? Description)
    : ICommand<CreateChannelResult>;

// ---------- lo que devolvemos al terminar ----------
public sealed record CreateChannelResult(
    Guid Id,
    string Name,
    string Handle,
    string Status);

// ---------- quien lo ejecuta ----------
//sealed es que nadie puede heredar esta clase
internal sealed class CreateChannelHandler(
    ChannelsDbContext db,
    IEventPublisher publisher,
    IConfiguration configuration,
    ILogger<CreateChannelHandler> logger)
    : ICommandHandler<CreateChannelCommand, CreateChannelResult>
{
    public async Task<CreateChannelResult> Handle(CreateChannelCommand cmd, CancellationToken ct)
    {
        // las reglas (un canal por usuario, handle libre) ya las comprobó el validador

        // 1. fabricamos el token y su hash (el token va al correo, el hash a la base)
        var (token, hash) = ConfirmationToken.Generate();

        // 2. creamos el canal: nace "Pendiente" (Channel.Create deja el handle en minúsculas)
        var channel = Channel.Create(
            cmd.OwnerId, cmd.Name, cmd.Handle, cmd.Description, hash, DateTime.UtcNow);

        // 3. guardamos
        db.Channels.Add(channel);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // dos peticiones a la vez: el índice único de la base rechazó la segunda
            throw new ConflictException("Ya tienes un canal o ese handle está en uso.");
        }

        // 4. avisamos al resto del sistema: Notifications enviará el correo
        var confirmationRequested = new ChannelConfirmationRequested(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            UserId: cmd.OwnerId,
            Email: cmd.OwnerEmail,
            ChannelName: channel.Name,
            ConfirmationUrl: BuildConfirmationUrl(token));

        try
        {
            // CancellationToken.None: el canal ya se guardó; el aviso debe salir aunque el cliente se desconecte
            await publisher.PublishAsync(
                ChannelConfirmationRequested.RoutingKey,
                confirmationRequested.EventId,
                confirmationRequested,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            // el canal YA existe: no fallamos la petición; el usuario puede pedir "reenviar confirmación"
            // (nunca se registra el token ni la URL)
            logger.LogError(ex, "No se pudo publicar la confirmación del canal {ChannelId}", channel.Id);
        }

        // 5. respondemos con un resumen (nunca el token ni el hash)
        return new CreateChannelResult(
            channel.Id, channel.Name, channel.Handle, channel.Status.ToString());
    }

    // el enlace apunta al FRONTEND; la página de confirmación llamará a la API con el token
    private string BuildConfirmationUrl(string token) =>
        $"{configuration["Frontend:BaseUrl"]}/channels/confirm?token={Uri.EscapeDataString(token)}";
}