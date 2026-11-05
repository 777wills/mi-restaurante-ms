# Plan de implementación: Convenciones HTTP transversales

**Rama**: `speckit-test` (feature `002-http-conventions`) | **Fecha**: 2026-11-04 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la feature en `specs/002-http-conventions/spec.md`

## Resumen

Dejar la API conforme a las convenciones HTTP del principio VI antes de escribir el primer endpoint de negocio:

1. Application define `NotFoundException` y `BusinessRuleException`; un único `IExceptionHandler` en Api las traduce a 404 y 422, `BadHttpRequestException` a su propio 4xx y cualquier otra excepción a 500 sin detalles internos, todo como ProblemDetails.
2. Un solo punto (`CustomizeProblemDetails`) pone los títulos fijos en español a todas las respuestas de error, incluidas las del framework (404 de ruta, 405, 415, 400); `UseStatusCodePages` convierte las respuestas vacías en ProblemDetails, y los errores de lectura del cuerpo se sustituyen por mensajes genéricos en español.
3. Scalar (`Scalar.AspNetCore` 2.18.7), el documento OpenAPI y CORS abierto existen solo en Development.
4. `TimeProvider.System` queda registrado como singleton.
5. Pruebas de arquitectura por reflexión garantizan `[ApiController]`, prefijo `api/v1/` y `[ProducesResponseType]` en todo controlador futuro; pruebas funcionales con un controlador que vive solo en el proyecto de pruebas verifican todo lo anterior de extremo a extremo.

El diseño se validó con un prototipo desechable (ver [research.md](research.md)).

## Contexto técnico

**Lenguaje/Versión**: C# sobre .NET 10 (SDK 10.0.112 de `global.json` con `latestFeature`)

**Dependencias principales**: ASP.NET Core 10 (MVC con `[ApiController]`, `Microsoft.AspNetCore.OpenApi` 10.0.14 ya instalado) y **nuevo** `Scalar.AspNetCore` 2.18.7 en Api

**Almacenamiento**: N/A (sin base de datos en esta feature)

**Pruebas**: xUnit v3 sobre Microsoft.Testing.Platform, AwesomeAssertions 9.6.0, `Microsoft.AspNetCore.Mvc.Testing` 10.0.14 y NetArchTest.eNhancedEdition 1.4.5 (todo ya referenciado); sin paquetes nuevos de pruebas

**Plataforma de destino**: API web REST ejecutada en el equipo del desarrollador (`dotnet run`)

**Tipo de proyecto**: web-service (API REST en cuatro capas)

**Objetivos de rendimiento**: N/A

**Restricciones**: cero advertencias (`TreatWarningsAsErrors`); sin `try`/`catch` en controladores; un único `IExceptionHandler`; ningún dato interno en las respuestas 500; el controlador de pruebas no puede aparecer en el ensamblado de la API; el código en inglés y los mensajes y títulos al usuario en español

**Escala/Alcance**: 2 clases en Application, 3 archivos de manejo de errores y cambios en `Program.cs` en Api, 1 paquete NuGet, 1 clase de pruebas de arquitectura, ~6 clases de pruebas funcionales y 1 de unidad

No quedan marcadores `NEEDS CLARIFICATION`.

## Constitution Check

_Puerta: debe pasar antes de la Fase 0. Se reevalúa tras el diseño de la Fase 1._

| Principio                        | Evaluación                                                                                                                                                                                                                                                                                                                                                        | Resultado                         |
| -------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------- |
| I. Simplicidad                   | Cero proyectos, capas o patrones nuevos. Un único paquete (`Scalar.AspNetCore`), exigido por el principio VI y por la tabla del stack, activo y sin vulnerabilidades conocidas en la restauración. El logger de pruebas en memoria son unas líneas y evita añadir un paquete de dobles a las pruebas funcionales.                                                  | Pasa                              |
| II. Arquitectura en cuatro capas | Las excepciones viven en Application y solo usan `System`, así que la prueba de lista blanca existente sigue pasando y cubre FR-002. El manejador y la configuración están en Api, que ya referencia Application. El controlador de pruebas está en el proyecto de pruebas funcionales.                                                                              | Pasa                              |
| III. Modelo de dominio simple    | Se registra `TimeProvider.System` en el contenedor (FR-021). No hay entidades, fechas ni dinero.                                                                                                                                                                                                                                                                  | Pasa                              |
| IV. Capa de aplicación           | Las excepciones de negocio son las definidas por el principio. No hay servicios todavía. Con `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes` los DTO futuros declaran `[Required]` con mensaje en español, como ya exige el principio.                                                                                                              | Pasa                              |
| V. Acceso a datos                | No aplica: sin persistencia.                                                                                                                                                                                                                                                                                                                                      | N/A                               |
| VI. API HTTP                     | Cubre `AddProblemDetails()` con un único `IExceptionHandler`, mapeo 404/422/500, Scalar y OpenAPI solo en Development, CORS abierto solo en Development, prefijo `/api/v1/` y `[ProducesResponseType]` verificados por pruebas. No hay `try`/`catch` en controladores. `/health` queda fuera por decisión de la spec (llega con la persistencia). `BadHttpRequestException` → su propio 4xx se añade al mapeo del principio (ver Complejidad).                      | Pasa con desviación (ver Complejidad) |
| VII. TDD (no negociable)         | Cada tarea de implementación va precedida de su prueba en Red. Las pruebas siguen la nomenclatura, una sola aserción lógica y sin condicionales; sin Moq en las funcionales. La puerta 4 de cobertura (≥ 80 %) se mantiene bloqueante, sin excepción. Las pruebas funcionales arrancan la API sin PostgreSQL porque la feature no tiene persistencia (D8); desde la primera feature con base de datos usarán PostgreSQL real con Testcontainers (desviación registrada en Complejidad).                                  | Pasa con desviación (ver Complejidad) |
| VIII. Convenciones de código     | Archivos con namespace de ámbito de archivo, `Nullable`, `TreatWarningsAsErrors`, `dotnet format`. Identificadores y rutas en inglés; títulos, mensajes y artefactos en español.                                                                                                                                                                                   | Pasa                              |
| IX. Entorno local                | No aplica: sin `compose.yaml` ni cadena de conexión nuevos (aplazados con la persistencia; ver Complejidad).                                                                                                                                                                                                                                                                                                       | N/A                               |

**Reevaluación tras la Fase 1**: el diseño no añade proyectos, paquetes de pruebas ni abstracciones. Las tres decisiones con efecto transversal (títulos en un único punto, sanitización de `ModelState` envolviendo la fábrica por defecto y `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes`) son ajustes de configuración, no capas. Resultado: **Pasa**, con las desviaciones registradas en Seguimiento de complejidad.

## Estructura del proyecto

### Documentación (esta feature)

```text
specs/002-http-conventions/
├── plan.md              # Este archivo
├── research.md          # Fase 0
├── data-model.md        # Fase 1
├── quickstart.md        # Fase 1
├── contracts/
│   └── http-conventions.md  # Fase 1
├── checklists/
└── tasks.md             # Fase 2 (/speckit-tasks; no lo crea /speckit-plan)
```

### Código fuente (raíz del repositorio)

```text
src/
├── MiRestaurante.Application/
│   └── Exceptions/
│       ├── NotFoundException.cs          # NUEVO
│       └── BusinessRuleException.cs      # NUEVO
└── MiRestaurante.Api/
    ├── MiRestaurante.Api.csproj          # MODIFICADO: + Scalar.AspNetCore 2.18.7
    ├── Program.cs                        # MODIFICADO: registro y pipeline; + public partial class Program
    └── ErrorHandling/
        ├── ApiExceptionHandler.cs        # NUEVO: único IExceptionHandler
        ├── ProblemDetailsTitles.cs       # NUEVO: tabla de títulos y CustomizeProblemDetails
        └── RequestBodyMessages.cs        # NUEVO: sanitización de errores de lectura del cuerpo

tests/
├── MiRestaurante.Application.UnitTests/
│   └── Exceptions/ExceptionsTests.cs                 # NUEVO
├── MiRestaurante.Architecture.Tests/
│   └── ControllerConventionTests.cs                  # NUEVO (4 reglas)
└── MiRestaurante.Api.FunctionalTests/
    ├── ApiFactory.cs                                 # NUEVO: entorno parametrizable + parte de aplicación de pruebas
    ├── CapturingLoggerProvider.cs                    # NUEVO: logger en memoria
    ├── TestControllers/ErrorsController.cs           # NUEVO: solo existe en este proyecto
    ├── ExceptionHandlingTests.cs                     # NUEVO: historia 1
    ├── FrameworkErrorTests.cs                        # NUEVO: historia 2
    ├── DevelopmentToolsTests.cs                      # NUEVO: historia 4 (Scalar, OpenAPI, CORS en Development)
    ├── ProductionEnvironmentTests.cs                 # NUEVO: historia 4 (fuera de Development)
    └── TimeProviderTests.cs                          # NUEVO: historia 5
```

**Decisión de estructura**: se conserva la estructura de cuatro capas y cinco proyectos de pruebas. No se crean proyectos. Los tres archivos de `ErrorHandling/` agrupan lo que se parece (traducción de errores) sin crear un ensamblado ni una abstracción; el resto de la configuración (CORS, Scalar, `TimeProvider`) son pocas líneas y se quedan en `Program.cs`. No se crea la carpeta `Controllers/` de Api: esta feature no añade controladores de producción.

## Diseño técnico

### Application: excepciones

`NotFoundException(string message)` y `BusinessRuleException(string message)` en `MiRestaurante.Application.Exceptions`, públicas, no selladas, heredan de `Exception` (D1). Sin referencias nuevas.

### Api: `ApiExceptionHandler`

- Implementa `IExceptionHandler`; inyecta `IProblemDetailsService` y `ILogger<ApiExceptionHandler>` (constructor primario).
- Una única decisión por tipo de excepción (tabla de [data-model.md](data-model.md#decisión-de-traducción-de-excepciones)): `Status`, `Detail` (solo negocio) y nivel de registro con plantilla de mensaje. Fija `Response.StatusCode`, escribe con `TryWriteAsync` pasando `Exception` en el contexto y devuelve el resultado.
- No fija el título: lo pone `CustomizeProblemDetails`.
- Detalle: `case` por tipo, `BadHttpRequestException` usa su `StatusCode`.

### Api: títulos y mensajes del cuerpo

- `ProblemDetailsTitles`: diccionario de FR-008 y el método de personalización (D3): `BadHttpRequestException` → «Solicitud no válida»; si no, título por `Status`.
- `RequestBodyMessages`: constantes de los dos mensajes en español y el método que envuelve `InvalidModelStateResponseFactory` (D4). Sin lógica condicional en las pruebas: se verifica por HTTP.

### Api: `Program.cs`

Servicios: `AddControllers(o => …)` con `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true` y el accesor del cuerpo requerido; `ConfigureApiBehaviorOptions` con la fábrica envuelta; `AddOpenApi()`; `AddProblemDetails(o => o.CustomizeProblemDetails = …)`; `AddExceptionHandler<ApiExceptionHandler>()`; `AddSingleton(TimeProvider.System)`; y, solo en Development, `AddCors` con la política por defecto abierta.

Pipeline: `UseExceptionHandler()` → `UseStatusCodePages()` → solo en Development `MapOpenApi()`, `MapScalarApiReference()` y `UseCors()` → `UseHttpsRedirection()` → `UseAuthorization()` → `MapControllers()`. Se añade `public partial class Program;` para `WebApplicationFactory`. No se tocan la redirección HTTPS ni `UseAuthorization`, ni se limpian los comentarios de plantilla (fuera de alcance).

### Pruebas

| Proyecto             | Clase                          | Cubre                                                                                     | FR                    |
| -------------------- | ------------------------------ | ----------------------------------------------------------------------------------------- | --------------------- |
| Application.UnitTests | `ExceptionsTests`              | El mensaje se conserva; una derivada sigue siendo `NotFoundException`/`BusinessRuleException` | FR-001                |
| Architecture.Tests   | `ControllerConventionTests`    | Reglas 1 a 4 de D7, sobre el conjunto vacío                                               | FR-013 a FR-016       |
| Api.FunctionalTests  | `ExceptionHandlingTests`       | 404, 422, 500 sin datos internos, `BadHttpRequestException` 413, derivadas, logs, `traceId`, ausencia de `instance` | FR-003 a FR-007a, FR-011 |
| Api.FunctionalTests  | `FrameworkErrorTests`          | 404 de ruta, 405, 415, 400 de validación, cuerpo mal formado/tipo incorrecto/vacío en español | FR-008 a FR-010a    |
| Api.FunctionalTests  | `DevelopmentToolsTests`        | `/scalar` (200 tras redirección), OpenAPI 200, preflight abierto                          | FR-017, FR-019        |
| Api.FunctionalTests  | `ProductionEnvironmentTests`   | `/scalar` y OpenAPI 404 ProblemDetails, sin cabeceras CORS                                | FR-018, FR-020        |
| Api.FunctionalTests  | `TimeProviderTests`            | Es `TimeProvider.System` y la misma instancia                                             | FR-021                |

FR-012 (sin `try`/`catch`) y SC-005 se verifican por revisión de código: no hay controladores de producción; el controlador de pruebas no contiene `try`/`catch`. «Exactamente 1 manejador registrado» se comprueba en `ExceptionHandlingTests` contando los `IExceptionHandler` resueltos.

**Orden TDD**: en `tasks.md` cada prueba precede a su implementación y se ejecuta en Red antes de implementar. Las reglas de arquitectura se demuestran en Red con controladores temporales (SC-003); las pruebas funcionales fallan primero por 404/500 por defecto o por la ausencia de `Program` público.

### Puerta de cobertura

Mismos comandos que en la feature 001 (ver [quickstart.md](quickstart.md#7-puerta-de-cobertura)). La puerta es bloqueante (≥ 80 %, sin excepción). `--ignore-exit-code 8` se mantiene: Domain e Infrastructure siguen sin pruebas.

## Seguimiento de complejidad

| Violación                                                                                                                                              | Por qué se necesita                                                                                                                                                                                     | Alternativa más simple descartada y por qué                                                                                                                                                     |
| ------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `/health` y `compose.yaml` (principios VI e IX) no se implementan en esta feature | La feature no introduce persistencia; `/health` verifica la conexión a la base de datos con `AddDbContextCheck` y aún no hay nada que comprobar | Añadir `/health` sin base de datos: no verificaría nada y habría que rehacerlo. Llegan con la primera feature que use persistencia |
| `BadHttpRequestException` → su propio 4xx en el `IExceptionHandler` (el principio VI fija solo 404/422/500) | Sin ello, un cuerpo mayor que el límite (413) o una petición mal formada respondería 500, exponiendo como fallo del servidor un error del cliente | No tratarla y dejar el 500: descartada porque oculta errores del cliente como fallos del servidor |
| Pruebas funcionales sin PostgreSQL real (principio VII) | La feature no tiene persistencia; no hay nada que probar contra la base de datos (D8) | Levantar Testcontainers sin usarlo: añade tiempo y dependencias sin valor. Desde la primera feature con base de datos se usa PostgreSQL real |
| `TestingPlatformCommandLineArguments=--ignore-exit-code 8` en `Directory.Build.props` (ya existente de la feature 001; se mantiene)                    | Domain.UnitTests e Infrastructure.IntegrationTests siguen sin pruebas y MTP termina con código 8 en ese caso.                                                                                           | Añadir una prueba de relleno en cada uno: contradice la limpieza de plantillas de 001 y añade pruebas sin valor. Se retira cuando esos proyectos tengan pruebas.                                |

No se añaden otros paquetes ni proyectos que requieran justificación.
