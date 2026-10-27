using Horizon.Channels.Common.Messaging;
using Horizon.Channels.Common.Messaging.Events;

namespace Horizon.Channels.Features.Channels.ResendConfirmation;

// ---------- lo que queremos hacer ----------
public sealed record ResendConfirmationCommand(
    Guid ChannelId,
    Guid UserId,          // quién lo pide (sale del JWT)
    string Email)         // a dónde se envía el enlace (sale del JWT)
    : ICommand<ResendConfirmationResult>;

// ---------- lo que devolvemos: solo hasta cuándo vale el enlace, NUNCA el token ----------
public sealed record ResendConfirmationResult(Guid ChannelId, DateTime ExpiresAt);

// ---------- quien lo ejecuta ----------
internal sealed class ResendConfirmationHandler(
    ChannelsDbContext db,
    IEventPublisher publisher,
    IConfiguration configuration)
    : ICommandHandler<ResendConfirmationCommand, ResendConfirmationResult>
{
    public async Task<ResendConfirmationResult> Handle(ResendConfirmationCommand cmd, CancellationToken ct)
    {
        // 1. el canal debe existir
        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == cmd.ChannelId, ct)
            ?? throw new NotFoundException("El canal no existe.");

        // 2. solo el dueño puede pedir un enlace nuevo
        if (!channel.IsOwnedBy(cmd.UserId))
            throw new ForbiddenException("Solo el dueño del canal puede pedir un nuevo enlace.");

        // 3. token nuevo; la entidad decide si se puede reemplazar el anterior
        var (token, hash) = ConfirmationToken.Generate();

        switch (channel.RenewConfirmation(hash, DateTime.UtcNow))
        {
            case RenewResult.AlreadyConfirmed:
                throw new ConflictException("El canal ya está confirmado.");
            case RenewResult.TooSoon:
                throw new TooManyRequestsException("Espera un minuto antes de pedir otro enlace.");
        }

        // 4. guardamos el hash nuevo (el token anterior deja de servir)
        await db.SaveChangesAsync(ct);

        // 5. avisamos al resto del sistema: Notifications enviará el correo
        var confirmationRequested = new ChannelConfirmationRequested(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            UserId: cmd.UserId,
            Email: cmd.Email,
            ChannelName: channel.Name,
            ConfirmationUrl: BuildConfirmationUrl(token));

        // aquí SÍ dejamos subir el error: el usuario pidió explícitamente el correo y debe saber si falló
        await publisher.PublishAsync(
            ChannelConfirmationRequested.RoutingKey,
            confirmationRequested.EventId,
            confirmationRequested,
            CancellationToken.None);

        return new ResendConfirmationResult(channel.Id, channel.ConfirmationExpiresAt!.Value);
    }

    // el enlace apunta al FRONTEND; la página de confirmación llamará a la API con el token
    private string BuildConfirmationUrl(string token) =>
        $"{configuration["Frontend:BaseUrl"]}/channels/confirm?token={Uri.EscapeDataString(token)}";
}