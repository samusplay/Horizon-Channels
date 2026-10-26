namespace Horizon.Channels.Features.Channels.GetChannel;

public sealed class GetChannelEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/channels/{handle}", async (
                string handle, ISender sender, CancellationToken ct) =>
        {
            // el handle llega en la ruta: /api/channels/mi-canal
            var result = await sender.Send(new GetChannelQuery(handle), ct);

            return Results.Ok(result);
        })
            .AllowAnonymous() // público: la página del canal la ve cualquiera
            .WithTags("Channels")
            .WithName("GetChannel")
            .WithSummary("Obtiene un canal activo por su handle")
            .WithDescription(
                "Devuelve la ficha pública de un canal confirmado. " +
                "Los canales pendientes de confirmación responden 404. No requiere sesión.")
            .Produces<ChannelDetail>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();
    }
}