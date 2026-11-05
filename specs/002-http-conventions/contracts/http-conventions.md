# Contrato: convenciones HTTP transversales

Esta feature no añade endpoints de negocio. Fija el contrato que cumplen todos los endpoints actuales y futuros. Las formas de los cuerpos están en [../data-model.md](../data-model.md).

## 1. Formato de error

Toda respuesta de error (4xx y 5xx) tiene `Content-Type: application/problem+json` y el cuerpo descrito en el modelo de datos (RFC 9457). Ejemplos verificados:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Recurso no encontrado","status":404,"detail":"No existe el plato 7","traceId":"00-…-00"}
```

```http
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/problem+json

{"type":"https://tools.ietf.org/html/rfc4918#section-11.2","title":"Regla de negocio incumplida","status":422,"detail":"Plato no disponible","traceId":"00-…-00"}
```

```http
HTTP/1.1 500 Internal Server Error
Content-Type: application/problem+json

{"type":"https://tools.ietf.org/html/rfc9110#section-15.6.1","title":"Error interno del servidor","status":500,"traceId":"00-…-00"}
```

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json; charset=utf-8

{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Uno o más errores de validación","status":400,"errors":{"$":["El cuerpo de la petición no tiene un formato válido."]},"traceId":"00-…-00"}
```

## 2. Correspondencia código ↔ origen

| Código | Origen                                                    | `detail`          |
| ------ | --------------------------------------------------------- | ----------------- |
| 400    | Validación (DataAnnotations) o cuerpo ilegible            | No (hay `errors`) |
| 4xx    | `BadHttpRequestException` (título «Solicitud no válida»)  | No                |
| 404    | `NotFoundException`, ruta inexistente                     | Solo con excepción |
| 405    | Método no admitido en una ruta existente                  | No                |
| 415    | Tipo de contenido no admitido                             | No                |
| 422    | `BusinessRuleException`                                   | Sí                |
| 500    | Cualquier otra excepción                                  | No                |

Ninguna respuesta 500 incluye traza, mensaje ni tipo de la excepción, en ningún entorno.

## 3. Reglas para controladores (verificadas por `MiRestaurante.Architecture.Tests`)

1. Todo controlador lleva `[ApiController]`.
2. Toda plantilla de ruta de clase empieza por `api/v1/` (por ejemplo, `[Route("api/v1/[controller]")]`).
3. Una plantilla de acción que empiece por `/` o `~/` debe, tras quitarlo, empezar por `api/v1/`.
4. Toda acción pública lleva al menos un `[ProducesResponseType]` en la propia acción.
5. Ningún controlador contiene `try`/`catch` (revisión de código; el manejador único traduce los errores).

## 4. Endpoints por entorno

| Ruta                    | Development                                  | Otro entorno                  |
| ----------------------- | -------------------------------------------- | ----------------------------- |
| `/scalar`               | 302 → `/scalar/` (200, interfaz Scalar)      | 404 ProblemDetails            |
| `/openapi/v1.json`      | 200, documento OpenAPI                       | 404 ProblemDetails            |
| Cabeceras `Access-Control-*` | Cualquier origen, método y cabecera, sin credenciales | Ninguna                |
| Cualquier ruta inexistente | 404 ProblemDetails                        | 404 ProblemDetails            |

`/health` queda fuera de esta feature (llegará con la persistencia).
