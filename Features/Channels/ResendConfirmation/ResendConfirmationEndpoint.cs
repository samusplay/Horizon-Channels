using System.Security.Claims;

namespace Horizon.Channels.Features.Channels.ResendConfirmation;

public sealed class ResendConfirmationEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/channels/{id:guid}/resend-confirmation", async (
                Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            // quién lo pide y a dónde enviar el enlace: todo sale del token
            var userId = Guid.Parse(user.FindFirstValue("sub")!);
            var email = user.FindFirstValue("email");

            if (string.IsNullOrWhiteSpace(email))
                return Results.Unauthorized(); // sin email no hay a dónde enviar el enlace

            var result = await sender.Send(new ResendConfirmationCommand(id, userId, email), ct);

            // 202: aceptado; el correo lo envía Notifications por su cuenta (asíncrono)
            return Results.Accepted(value: result);
        })
            .RequireAuthorization() // exige sesión; que sea el dueño lo revisa el handler
            .WithTags("Channels")
            .WithName("ResendChannelConfirmation")
            .WithSummary("Genera un nuevo enlace de confirmación del canal")
            .WithDescription(
                "Reemplaza el token de confirmación de un canal pendiente por uno nuevo y lo envía por correo. " +
                "El enlace anterior deja de servir. Solo el dueño puede pedirlo, una vez por minuto como máximo.")
            .Produces<ResendConfirmationResult>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithOpenApi();
    }
}