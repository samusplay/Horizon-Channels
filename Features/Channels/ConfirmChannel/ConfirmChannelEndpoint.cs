namespace Horizon.Channels.Features.Channels.ConfirmChannel;

// El contrato HTTP: el token que llegó en el enlace del correo
public sealed record ConfirmChannelRequest(string Token);

public sealed class ConfirmChannelEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/channels/confirm", async (
                ConfirmChannelRequest request, ISender sender, CancellationToken ct) =>
        {
            // 1. Mapster convierte la petición HTTP en el comando
            var command = request.Adapt<ConfirmChannelCommand>();

            // 2. ISender lo lleva a su handler (pasando antes por el validador)
            var result = await sender.Send(command, ct);

            return Results.Ok(result);
        })
            .AllowAnonymous() // público: el token del correo es la prueba
            .WithTags("Channels")
            .WithName("ConfirmChannel")
            .WithSummary("Confirma un canal con el token del correo")
            .WithDescription(
                "Activa un canal pendiente usando el token del enlace de confirmación. " +
                "El token sirve una sola vez y vence a las 24 horas. No requiere sesión.")
            .Produces<ConfirmChannelResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone)
            .WithOpenApi();
    }
}