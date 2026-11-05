using Microsoft.AspNetCore.Mvc;

namespace MiRestaurante.Api.ErrorHandling;

internal static class ProblemDetailsTitles
{
    private const string InvalidRequestTitle = "Solicitud no válida";

    private static readonly Dictionary<int, string> TitlesByStatus = new()
    {
        [StatusCodes.Status400BadRequest] = "Uno o más errores de validación",
        [StatusCodes.Status404NotFound] = "Recurso no encontrado",
        [StatusCodes.Status405MethodNotAllowed] = "Método no permitido",
        [StatusCodes.Status415UnsupportedMediaType] = "Tipo de contenido no admitido",
        [StatusCodes.Status422UnprocessableEntity] = "Regla de negocio incumplida",
        [StatusCodes.Status500InternalServerError] = "Error interno del servidor",
    };

    public static void Customize(ProblemDetailsContext context)
    {
        ProblemDetails problem = context.ProblemDetails;

        if (context.Exception is BadHttpRequestException)
        {
            problem.Title = InvalidRequestTitle;
        }
        else if (TitlesByStatus.TryGetValue(problem.Status ?? context.HttpContext.Response.StatusCode, out string? title))
        {
            problem.Title = title;
        }
    }
}
