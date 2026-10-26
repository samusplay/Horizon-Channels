namespace Horizon.Channels.Features.Channels.UpdateChannel;

// ---------- lo que queremos hacer ----------
public sealed record UpdateChannelCommand(
    Guid ChannelId,
    Guid UserId,          // quién lo pide (sale del JWT)
    string Name,
    string? Description)
    : ICommand<UpdateChannelResult>;

// ---------- lo que devolvemos: el canal como quedó ----------
public sealed record UpdateChannelResult(
    Guid Id,
    string Name,
    string Handle,
    string? Description);

// ---------- quien lo ejecuta ----------
internal sealed class UpdateChannelHandler(ChannelsDbContext db)
    : ICommandHandler<UpdateChannelCommand, UpdateChannelResult>
{
    public async Task<UpdateChannelResult> Handle(UpdateChannelCommand cmd, CancellationToken ct)
    {
        // 1. el canal debe existir
        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == cmd.ChannelId, ct)
            ?? throw new NotFoundException("El canal no existe.");

        // 2. solo el dueño puede editarlo
        if (!channel.IsOwnedBy(cmd.UserId))
            throw new ForbiddenException("Solo el dueño del canal puede editarlo.");

        // 3. la entidad aplica el cambio con sus reglas de limpieza
        channel.UpdateProfile(cmd.Name, cmd.Description);

        // 4. guardamos
        await db.SaveChangesAsync(ct);

        return new UpdateChannelResult(channel.Id, channel.Name, channel.Handle, channel.Description);
    }
}