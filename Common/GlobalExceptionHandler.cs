using FluentValidation;
using Horizon.Channels.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Horizon.Channels.Common;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx, Exception ex, CancellationToken ct)
    {
        // datos mal enviados: 400 con el detalle por campo
        if (ex is ValidationException ve)
        {
            var errors = ve.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            await Write(ctx, new HttpValidationProblemDetails(errors)
            {
                Title = "Datos inválidos",
                Status = StatusCodes.Status400BadRequest,
            }, ct);
            return true;
        }

        // aquí se decide qué código HTTP corresponde a cada error
        var (status, title, detail) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "No encontrado", ex.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto", ex.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Prohibido", ex.Message),
            GoneException => (StatusCodes.Status410Gone, "Expirado", ex.Message),
            TooManyRequestsException => (StatusCodes.Status429TooManyRequests, "Demasiadas solicitudes", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno",
                  "Ocurrió un error inesperado."),
        };

        // un 500 es un bug nuestro: se guarda completo en el log, y al cliente
        // solo le decimos que falló, sin detalles internos
        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Error no controlado en {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);

        await Write(ctx, new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status,
        }, ct);
        return true;
    }

    private static async Task Write(HttpContext ctx, ProblemDetails problem, CancellationToken ct)
    {
        ctx.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        // pasamos el tipo real para que HttpValidationProblemDetails incluya su campo "errors"
        await ctx.Response.WriteAsJsonAsync(
            problem,
            problem.GetType(),
            options: null,
            contentType: "application/problem+json",
            cancellationToken: ct);
    }
}