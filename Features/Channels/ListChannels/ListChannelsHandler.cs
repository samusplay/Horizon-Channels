namespace Horizon.Channels.Features.Channels.ListChannels;

// ---------- lo que pedimos ----------
public sealed record ListChannelsQuery(int Page, int PageSize) : IQuery<ListChannelsResult>;

// ---------- lo que devolvemos: solo datos públicos ----------
public sealed record ChannelSummary(
    Guid Id,
    string Name,
    string Handle,
    string? Description,
    string? BannerUrl);

public sealed record ListChannelsResult(
    IReadOnlyList<ChannelSummary> Items,
    int Page,
    int PageSize,
    int TotalCount);

// ---------- quien lo ejecuta ----------
internal sealed class ListChannelsHandler(ChannelsDbContext db)
    : IQueryHandler<ListChannelsQuery, ListChannelsResult>
{
    public async Task<ListChannelsResult> Handle(ListChannelsQuery query, CancellationToken ct)
    {
        // 1. solo canales públicos (los pendientes no se muestran)
        var active = db.Channels.Where(c => c.Status == ChannelStatus.Active);

        // 2. el total, para que el frontend sepa cuántas páginas hay
        var total = await active.CountAsync(ct);

        // 3. la página pedida, con orden estable y solo las columnas públicas
        var items = await active
            .OrderByDescending(c => c.ConfirmedAt)
            .ThenBy(c => c.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new ChannelSummary(c.Id, c.Name, c.Handle, c.Description, c.BannerUrl))
            .ToListAsync(ct);

        return new ListChannelsResult(items, query.Page, query.PageSize, total);
    }
}