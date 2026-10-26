using System.Security.Claims;

namespace Horizon.Channels.Features.Channels.UpdateChannel;

// El contrato HTTP: los campos editables (el handle no está, porque no se puede cambiar)
public sealed record UpdateChannelRequest(string Name, string? Description);

public sealed class UpdateChannelEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/channels/{id:guid}", async (
                Guid id, UpdateChannelRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            // 1. quién lo pide: sale del token
            var userId = Guid.Parse(user.FindFirstValue("sub")!);

            // 2. Mapster convierte la petición en el comando, y le agregamos el canal y el usuario
            var command = request.Adapt<UpdateChannelCommand>() with { ChannelId = id, UserId = userId };

            // 3. ISender lo lleva al validador y luego al handler
            var result = await sender.Send(command, ct);

            return Results.Ok(result);
        })
            .RequireAuthorization() // exige sesión; que sea el dueño lo revisa el handler
            .WithTags("Channels")
            .WithName("UpdateChannel")
            .WithSummary("Edita el nombre y la descripción del canal")
            .WithDescription(
                "Reemplaza el nombre y la descripción del canal. El handle no se puede cambiar. " +
                "Se deben enviar ambos campos tal como deben quedar; una descripción vacía la elimina. " +
                "Solo el dueño del canal puede editarlo.")
            .Produces<UpdateChannelResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithOpenApi();
    }
}