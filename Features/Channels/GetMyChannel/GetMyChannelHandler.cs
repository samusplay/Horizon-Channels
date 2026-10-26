namespace Horizon.Channels.Features.Channels.GetMyChannel;

// ---------- lo que pedimos: el canal de este usuario ----------
public sealed record GetMyChannelQuery(Guid UserId) : IQuery<MyChannelResult>;

// ---------- lo que devolvemos: la vista del dueño (incluye estado y vencimiento) ----------
public sealed record MyChannelResult(
    Guid Id,
    string Name,
    string Handle,
    string? Description,
    string? BannerUrl,
    string Status,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? ConfirmationExpiresAt);

// ---------- quien lo ejecuta ----------
internal sealed class GetMyChannelHandler(ChannelsDbContext db)
    : IQueryHandler<GetMyChannelQuery, MyChannelResult>
{
    public async Task<MyChannelResult> Handle(GetMyChannelQuery query, CancellationToken ct)
    {
        // sin filtro de estado: el dueño ve su canal también si está pendiente
        return await db.Channels
            .Where(c => c.OwnerId == query.UserId)
            .Select(c => new MyChannelResult(
                c.Id,
                c.Name,
                c.Handle,
                c.Description,
                c.BannerUrl,
                c.Status.ToString(),
                c.CreatedAt,
                c.ConfirmedAt,
                c.ConfirmationExpiresAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Aún no tienes un canal.");
    }
}