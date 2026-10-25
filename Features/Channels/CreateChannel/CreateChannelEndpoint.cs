using System.Security.Claims;

namespace Horizon.Channels.Features.Channels.CreateChannel;

// El contrato HTTP: lo que manda el cliente en el cuerpo (sin OwnerId)
public sealed record CreateChannelRequest(string Name, string Handle, string? Description);

public sealed class CreateChannelEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/channels", async (
                CreateChannelRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            // 1. quién es: el dueño sale del token, nunca del cuerpo
            var ownerId = Guid.Parse(user.FindFirstValue("sub")!);

            // 2. Mapster convierte la petición HTTP en el comando, y le agregamos el dueño
            var command = request.Adapt<CreateChannelCommand>() with { OwnerId = ownerId };

            // 3. ISender lleva el comando a su handler (pasando antes por el validador)
            var result = await sender.Send(command, ct);

            return Results.Created($"/api/channels/{result.Handle}", result);
        })
            .RequireAuthorization(policy => policy.RequireRole("Creador"))
            .WithTags("Channels")
            .WithName("CreateChannel")
            .WithSummary("Crea el canal del creador autenticado")
            .WithDescription(
                "Crea un canal en estado PendingConfirmation y envía un correo de confirmación. " +
                "Cada usuario puede tener un solo canal y el handle debe ser único. " +
                "Requiere el rol Creador.")
            .Produces<CreateChannelResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .WithOpenApi();
    }
}