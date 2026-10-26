using System.Security.Claims;

namespace Horizon.Channels.Features.Channels.UploadBanner;

public sealed class UploadBannerEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/channels/{id:guid}/banner", async (
                Guid id, IFormFile file, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            // 1. quién lo pide: sale del token
            var userId = Guid.Parse(user.FindFirstValue("sub")!);

            // 2. abrimos el archivo como stream; se cierra solo al terminar la petición
            await using var content = file.OpenReadStream();

            // 3. ISender lo lleva al validador y luego al handler
            var result = await sender.Send(new UploadBannerCommand(
                id, userId, content, file.ContentType, file.Length), ct);

            return Results.Ok(result);
        })
            .RequireAuthorization()   // exige sesión; que sea el dueño lo revisa el handler
            .DisableAntiforgery()     // API con JWT, no formularios con cookies (ver nota abajo)
            .WithTags("Channels")
            .WithName("UploadChannelBanner")
            .WithSummary("Sube o reemplaza el banner del canal")
            .WithDescription(
                "Recibe una imagen (JPG, PNG o WEBP, máximo 5 MB) en el campo 'file' de un formulario " +
                "multipart y la guarda como banner del canal. Reemplaza y borra el banner anterior. " +
                "Solo el dueño del canal puede cambiarlo.")
            .Produces<UploadBannerResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithOpenApi();
    }
}