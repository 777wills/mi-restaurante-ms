using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiRestaurante.Application.Exceptions;

namespace MiRestaurante.Api.ErrorHandling;

internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int status;
        string? detail = null;

        switch (exception)
        {
            case NotFoundException:
                status = StatusCodes.Status404NotFound;
                detail = exception.Message;
                logger.LogInformation("Recurso no encontrado: {Message}", exception.Message);
                break;
            case BusinessRuleException:
                status = StatusCodes.Status422UnprocessableEntity;
                detail = exception.Message;
                logger.LogInformation("Regla de negocio incumplida: {Message}", exception.Message);
                break;
            case BadHttpRequestException badRequest:
                status = badRequest.StatusCode;
                logger.LogInformation("Solicitud no válida ({StatusCode}): {Message}", status, exception.Message);
                break;
            default:
                status = StatusCodes.Status500InternalServerError;
                logger.LogError(exception, "Error no controlado al procesar la petición");
                break;
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Detail = detail },
        });
    }
}
