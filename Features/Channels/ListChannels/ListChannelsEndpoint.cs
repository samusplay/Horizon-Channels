namespace Horizon.Channels.Features.Channels.ListChannels;

public sealed class ListChannelsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/channels", async (
                ISender sender, CancellationToken ct, int page = 1, int pageSize = 20) =>
        {
            // page y pageSize llegan de la URL (?page=2&pageSize=10); si faltan, se usan los valores por defecto
            var result = await sender.Send(new ListChannelsQuery(page, pageSize), ct);

            return Results.Ok(result);
        })
            .AllowAnonymous() // público: cualquiera puede ver los canales
            .WithTags("Channels")
            .WithName("ListChannels")
            .WithSummary("Lista los canales activos")
            .WithDescription(
                "Devuelve los canales confirmados, paginados y ordenados del más reciente al más antiguo. " +
                "Los canales pendientes de confirmación no aparecen. No requiere sesión.")
            .Produces<ListChannelsResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithOpenApi();
    }
}