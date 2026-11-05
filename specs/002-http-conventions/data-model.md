# Modelo de datos: Convenciones HTTP transversales

Esta feature no introduce entidades de dominio ni persistencia (sin tablas, migraciones ni `DbContext`). Los únicos tipos nuevos son las dos excepciones de negocio y la forma de la respuesta de error. El contrato HTTP completo está en [contracts/http-conventions.md](contracts/http-conventions.md).

## Excepciones de negocio (Application)

| Tipo                    | Espacio de nombres                      | Base        | Constructor       | Se traduce a | Sellada |
| ----------------------- | --------------------------------------- | ----------- | ----------------- | ------------ | ------- |
| `NotFoundException`     | `MiRestaurante.Application.Exceptions`  | `Exception` | `(string message)` | 404          | No      |
| `BusinessRuleException` | `MiRestaurante.Application.Exceptions`  | `Exception` | `(string message)` | 422          | No      |

- El mensaje es para el cliente y está redactado en español; se publica como `detail`.
- Sin dependencias fuera de `System` (FR-002). No sellada: una subclase se traduce igual que su base.
- Sin estado de ciclo de vida ni transiciones.

## Respuesta de error (ProblemDetails)

| Campo      | Presencia                                                    | Origen                                                                  |
| ---------- | ------------------------------------------------------------ | ----------------------------------------------------------------------- |
| `type`     | Siempre                                                      | Valor por defecto del framework (no se personaliza)                     |
| `title`    | Siempre                                                      | Tabla fija en español (ver abajo)                                       |
| `status`   | Siempre                                                      | Código HTTP                                                             |
| `detail`   | Solo 404 y 422 originados por excepción de negocio          | `Message` de la excepción (puede quedar vacío si la excepción no lleva mensaje) |
| `traceId`  | Siempre                                                      | Añadido por el framework                                                |
| `errors`   | Solo 400 de validación                                       | Diccionario campo → lista de mensajes                                   |
| `instance` | Nunca                                                        | —                                                                       |

### Títulos

| Código | Título                            | Origen habitual                                                    |
| ------ | --------------------------------- | ------------------------------------------------------------------ |
| 400    | Uno o más errores de validación   | DataAnnotations, cuerpo inválido, vacío o mal formado               |
| 404    | Recurso no encontrado             | `NotFoundException`, ruta inexistente, `NotFound()`, Scalar/OpenAPI fuera de Development |
| 405    | Método no permitido               | Ruta existente con otro método                                      |
| 415    | Tipo de contenido no admitido     | Cuerpo con tipo de contenido no admitido                            |
| 422    | Regla de negocio incumplida       | `BusinessRuleException`                                             |
| 500    | Error interno del servidor        | Cualquier otra excepción                                            |
| (4xx)  | Solicitud no válida               | `BadHttpRequestException` (400, 413…): sin `detail`                 |

Otros códigos no incluidos en la tabla conservan el título por defecto del framework hasta que una spec los añada.

### Decisión de traducción de excepciones

| Excepción (o derivada)      | Código                     | `detail`  | Título                     | Registro (`ILogger<ApiExceptionHandler>`)         |
| --------------------------- | -------------------------- | --------- | -------------------------- | ------------------------------------------------- |
| `NotFoundException`         | 404                        | mensaje   | Recurso no encontrado      | Information, mensaje, sin traza                   |
| `BusinessRuleException`     | 422                        | mensaje   | Regla de negocio incumplida | Information, mensaje, sin traza                  |
| `BadHttpRequestException`   | `StatusCode` de la excepción | ninguno | Solicitud no válida        | Information, mensaje, sin traza                   |
| cualquier otra              | 500                        | ninguno   | Error interno del servidor | Error, excepción completa                         |

## Mensajes de lectura del cuerpo (400)

| Situación                                                   | Clave en `errors` | Mensaje                                                  |
| ----------------------------------------------------------- | ----------------- | -------------------------------------------------------- |
| JSON mal formado, valor de tipo incorrecto, tipo raíz incorrecto | `$`, `$.campo`    | El cuerpo de la petición no tiene un formato válido.     |
| Cuerpo vacío, ausente o `null`                              | (vacía)           | Se requiere un cuerpo en la petición.                    |
| Incumple un atributo de DataAnnotations                     | nombre de la propiedad | El `ErrorMessage` del atributo en el DTO            |

## Configuración que usa la feature

Sin claves nuevas en `appsettings*.json`. El comportamiento depende solo de `ASPNETCORE_ENVIRONMENT` (Development o cualquier otro).
