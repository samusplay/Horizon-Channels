using System.Security.Claims;

namespace Horizon.Channels.Features.Channels.CreateChannel;

// El contrato HTTP: lo que manda el cliente en el cuerpo (sin OwnerId ni email)
public sealed record CreateChannelRequest(string Name, string Handle, string? Description);

public sealed class CreateChannelEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/channels", async (
                CreateChannelRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            // 1. solo los creadores pueden tener canal (403 con mensaje claro)
            if (!user.IsInRole("Creador"))
                throw new ForbiddenException("Solo los creadores pueden tener un canal.");

            // 2. quién es y a dónde enviarle el correo: todo sale del token, nunca del cuerpo
            var ownerId = Guid.Parse(user.FindFirstValue("sub")!);
            var ownerEmail = user.FindFirstValue("email");

            if (string.IsNullOrWhiteSpace(ownerEmail))
                return Results.Unauthorized(); // un token sin email no sirve para confirmar el canal

            // 3. Mapster convierte la petición HTTP en el comando, y le agregamos los datos del token
            var command = request.Adapt<CreateChannelCommand>() with
            {
                OwnerId = ownerId,
                OwnerEmail = ownerEmail
            };

            // 4. ISender lleva el comando a su handler (pasando antes por el validador)
            var result = await sender.Send(command, ct);

            return Results.Created($"/api/channels/{result.Handle}", result);
        })
            .RequireAuthorization() // exige sesión iniciada; el rol se revisa arriba
            .WithTags("Channels")
            .WithName("CreateChannel")
            .WithSummary("Crea el canal del creador autenticado")
            .WithDescription(
                "Crea un canal en estado PendingConfirmation y envía un correo de confirmación. " +
                "Cada usuario puede tener un solo canal y el handle debe ser único. " +
                "Solo los usuarios con rol Creador pueden tener canal.")
            .Produces<CreateChannelResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithOpenApi();
    }
}