using Microsoft.AspNetCore.Mvc;

namespace MiRestaurante.Api.ErrorHandling;

internal static class RequestBodyMessages
{
    public const string InvalidFormat = "El cuerpo de la petición no tiene un formato válido.";
    public const string MissingBody = "Se requiere un cuerpo en la petición.";

    // The JSON formatter reports its errors under keys starting with '$'; their text exposes .NET types and positions.
    public static Func<ActionContext, IActionResult> Sanitize(Func<ActionContext, IActionResult> defaultFactory) =>
        context =>
        {
            foreach (string key in context.ModelState.Keys.Where(key => key.StartsWith('$')).ToList())
            {
                context.ModelState.ClearValidationState(key);
                context.ModelState.AddModelError(key, InvalidFormat);
            }

            return defaultFactory(context);
        };
}
