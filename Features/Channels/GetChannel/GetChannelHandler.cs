namespace Horizon.Channels.Features.Channels.GetChannel;

// ---------- lo que pedimos ----------
public sealed record GetChannelQuery(string Handle) : IQuery<ChannelDetail>;

// ---------- lo que devolvemos: la ficha pública del canal ----------
public sealed record ChannelDetail(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Handle,
    string? Description,
    string? BannerUrl,
    DateTime? ConfirmedAt);

// ---------- quien lo ejecuta ----------
internal sealed class GetChannelHandler(ChannelsDbContext db)
    : IQueryHandler<GetChannelQuery, ChannelDetail>
{
    public async Task<ChannelDetail> Handle(GetChannelQuery query, CancellationToken ct)
    {
        // en la base los handles están en minúsculas, y PostgreSQL distingue mayúsculas
        var handle = query.Handle.Trim().ToLowerInvariant();

        return await db.Channels
            .Where(c => c.Handle == handle && c.Status == ChannelStatus.Active)
            .Select(c => new ChannelDetail(
                c.Id, c.OwnerId, c.Name, c.Handle, c.Description, c.BannerUrl, c.ConfirmedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"No existe el canal '{handle}'.");
    }
}