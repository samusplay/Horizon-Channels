using System.Security.Claims;

namespace Horizon.Channels.Features.Channels.GetMyChannel;

public sealed class GetMyChannelEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/channels/mine", async (
                ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            // el dueño sale del token: esta ruta solo puede devolver TU canal
            var userId = Guid.Parse(user.FindFirstValue("sub")!);

            var result = await sender.Send(new GetMyChannelQuery(userId), ct);

            return Results.Ok(result);
        })
            .RequireAuthorization()
            .WithTags("Channels")
            .WithName("GetMyChannel")
            .WithSummary("Obtiene el canal del usuario autenticado")
            .WithDescription(
                "Devuelve el canal del usuario de la sesión, incluido si está pendiente de confirmación, " +
                "con su estado y el vencimiento del enlace. Responde 404 si el usuario aún no tiene canal.")
            .Produces<MyChannelResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithOpenApi();
    }
}