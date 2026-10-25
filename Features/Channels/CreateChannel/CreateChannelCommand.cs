namespace Horizon.Channels.Features.Channels.CreateChannel;

// ---------- lo que queremos hacer ----------
public sealed record CreateChannelCommand(
    Guid OwnerId,          // sale del JWT, nunca del cuerpo de la petición
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
internal sealed class CreateChannelHandler(ChannelsDbContext db, ILogger<CreateChannelHandler> logger)
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

        // TEMPORAL: hasta tener el correo, el token se ve en el log. Se borra al conectar el email
        logger.LogInformation("Token de confirmación del canal {Handle}: {Token}", channel.Handle, token);

        // 4. respondemos con un resumen (nunca el token ni el hash)
        return new CreateChannelResult(
            channel.Id, channel.Name, channel.Handle, channel.Status.ToString());
    }
}