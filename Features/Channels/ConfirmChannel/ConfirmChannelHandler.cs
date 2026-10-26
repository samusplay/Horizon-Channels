namespace Horizon.Channels.Features.Channels.ConfirmChannel;

// ---------- lo que queremos hacer ----------
public sealed record ConfirmChannelCommand(string Token) : ICommand<ConfirmChannelResult>;

// ---------- lo que devolvemos al terminar ----------
public sealed record ConfirmChannelResult(Guid Id, string Handle, string Status);

// ---------- quien lo ejecuta ----------
internal sealed class ConfirmChannelHandler(ChannelsDbContext db)
    : ICommandHandler<ConfirmChannelCommand, ConfirmChannelResult>
{
    public async Task<ConfirmChannelResult> Handle(ConfirmChannelCommand cmd, CancellationToken ct)
    {
        // 1. en la base solo existe el hash, así que calculamos el del token recibido
        var hash = ConfirmationToken.Hash(cmd.Token);

        // 2. buscamos el canal que tenga ese hash
        var channel = await db.Channels
            .FirstOrDefaultAsync(c => c.ConfirmationTokenHash == hash, ct)
            ?? throw new NotFoundException("El enlace de confirmación no es válido o ya fue usado.");

        // 3. la entidad decide si se puede confirmar
        if (channel.Confirm(DateTime.UtcNow) == ConfirmResult.Expired)
            throw new GoneException("El enlace de confirmación venció. Pide uno nuevo.");

        // 4. guardamos el cambio de estado
        await db.SaveChangesAsync(ct);

        return new ConfirmChannelResult(channel.Id, channel.Handle, channel.Status.ToString());
    }
}