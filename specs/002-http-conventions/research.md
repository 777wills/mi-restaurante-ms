# Investigación: Convenciones HTTP transversales

Todas las decisiones se verificaron en una copia desechable del repositorio (en `/tmp`, fuera del árbol de trabajo) con SDK 10.0.114, `Scalar.AspNetCore` 2.18.7 y `Microsoft.AspNetCore.OpenApi` 10.0.14, arrancando la API en Development y en Production y probándola con `curl`.

## D1. Excepciones de negocio en Application (FR-001, FR-002)

- **Decisión**: dos clases públicas no selladas, `NotFoundException` y `BusinessRuleException`, en `MiRestaurante.Application.Exceptions`, con un único constructor `(string message)` que hereda de `Exception`.
- **Justificación**: el principio IV las sitúa en Application. Solo usan `System.Exception`, así que la lista blanca de ensamblados del principio II sigue cumpliéndose y la prueba existente (`ApplicationDependencyTests`) cubre FR-002 sin cambios. No se sellan porque el caso límite «excepción derivada» exige que una subclase se trate como su base.
- **Alternativas**: constructores de conveniencia (recurso + id): descartados por YAGNI (supuesto de la spec); una clase base común: descartada, no aporta nada con dos tipos.

## D2. Un único `IExceptionHandler` que delega en `IProblemDetailsService` (FR-003 a FR-007, FR-011)

- **Decisión**: `ApiExceptionHandler` (en Api) implementa `IExceptionHandler`, decide solo el código y el `detail`, registra el log y escribe con `IProblemDetailsService.TryWriteAsync`, pasando `Exception` en el `ProblemDetailsContext`. Se registra con `AddExceptionHandler<ApiExceptionHandler>()` y se activa con `app.UseExceptionHandler()` (sin argumentos).
- **Hallazgo**: con `AddProblemDetails()`, `UseExceptionHandler()` sin argumentos usa el `IExceptionHandler` registrado y el formato ProblemDetails. El framework añade `traceId` solo y el `type` por defecto (`rfc9110#section-15.5.5` para 404, `rfc4918#section-11.2` para 422, `rfc9110#section-15.6.1` para 500, `rfc9110#section-15.5.14` para 413). No aparece `instance`, así que FR-007 y FR-007a se cumplen sin código.
- **Hallazgo**: la excepción manejada no se registra por duplicado: en los logs de Production solo aparece la línea del manejador (Information para negocio, Error con traza para el fallo inesperado). El `appsettings.json` ya filtra `Microsoft.AspNetCore` a Warning.
- **Hallazgo**: la respuesta 500 solo trae `type`, `title`, `status` y `traceId`, también en Development: el `DeveloperExceptionPageMiddleware` que `WebApplication` añade en Development no llega a ver la excepción porque el manejador devuelve `true`. No hace falta (ni se usa) `UseDeveloperExceptionPage`.
- **Caso límite «respuesta ya iniciada»**: el middleware del framework no invoca al manejador si la respuesta ya empezó; registra el hecho y relanza la excepción, de modo que sigue registrándose. Es comportamiento del framework; no se escribe código ni prueba específicos.
- **Alternativas**: `UseExceptionHandler(Action<IApplicationBuilder>)` con lambda: descartada, el principio VI exige `IExceptionHandler`; filtros de excepción MVC: descartados, no cubren excepciones fuera de MVC (por ejemplo, `BadHttpRequestException` del servidor).

## D3. Títulos fijos en español desde un único punto (FR-004a, FR-008, FR-009)

- **Decisión**: `ProblemDetailsOptions.CustomizeProblemDetails` asigna el título: «Solicitud no válida» si `context.Exception` es `BadHttpRequestException`; si no, el de la tabla de FR-008 según `Status`; si el código no está en la tabla, se deja el título por defecto del framework.
- **Hallazgo**: `CustomizeProblemDetails` se aplica a todos los caminos que producen ProblemDetails: el manejador de excepciones, `UseStatusCodePages()` (404 de ruta inexistente y 405 de rutas con otro método), el 415 de MVC, el 404 de `NotFound()` en una acción y el 400 de validación. Verificado con `curl`: los seis títulos salen en español con una sola función.
- **Hallazgo**: `UseStatusCodePages()` sin argumentos, junto con `AddProblemDetails()`, convierte las respuestas vacías 404/405 en ProblemDetails (FR-009). El 415 ya sale como ProblemDetails desde MVC (`[ApiController]`).
- **Orden del pipeline**: `UseExceptionHandler()` → `UseStatusCodePages()` → (Development: `MapOpenApi`, `MapScalarApiReference`, `UseCors`) → `UseHttpsRedirection` → `UseAuthorization` → `MapControllers`. El orden relativo de lo ya existente no cambia.
- **Alternativas**: títulos en el manejador y en `ApiBehaviorOptions.ClientErrorMapping` por separado: descartado, dos fuentes de verdad; recursos `.resx` de localización: descartado, hay una sola lengua.

## D4. Mensajes genéricos en español para errores de lectura del cuerpo (FR-010, FR-010a)

- **Hallazgo de partida**: el framework responde en inglés y con datos internos: `'b' is an invalid start of a property name… Path: $ | LineNumber: 0 | BytePositionInLine: 1.`, `The JSON value could not be converted to MiRestaurante.Api.Controllers.Dto. Path: $.price…` y `A non-empty request body is required.`; además añade `The dto field is required.` en inglés para el parámetro.
- **Decisión** (tres ajustes pequeños):
  1. `MvcOptions.ModelBindingMessageProvider.SetMissingRequestBodyRequiredValueAccessor(() => "Se requiere un cuerpo en la petición.")` para el cuerpo vacío, ausente o `null`.
  2. `ApiBehaviorOptions.InvalidModelStateResponseFactory` envuelve la fábrica por defecto: antes de llamarla, sustituye los errores de las entradas de `ModelState` cuya clave empieza por `$` (los errores del formateador JSON: JSON mal formado, tipo incorrecto) por «El cuerpo de la petición no tiene un formato válido.». La respuesta sigue generándola la fábrica por defecto (título, `type`, `traceId`).
  3. `MvcOptions.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`, para eliminar el error en inglés `The dto field is required.`, que el framework añade por tipo no anulable.
- **Verificado**: con esos tres ajustes, JSON mal formado, `"price":"abc"`, cuerpo vacío, `null` y `[1]` devuelven 400 con un único mensaje en español por error y sin tipos .NET, rutas internas ni posiciones; un DTO con `[Required(ErrorMessage = "El nombre es obligatorio")]` sigue devolviendo su mensaje.
- **Consecuencia aceptada**: con el punto 3, las propiedades de un DTO ya no son obligatorias por ser no anulables; cada campo obligatorio DEBE llevar `[Required(ErrorMessage = "…")]` en español. Eso ya lo exige el principio IV (validación con DataAnnotations y mensajes en español), y evita mensajes en inglés. Debe recordarse en las specs de las features de negocio.
- **Nota sobre claves**: las claves del diccionario `errors` (`$`, `$.price`) se conservan; identifican el campo del cliente y no forman parte del mensaje. FR-010a se refiere a los mensajes.
- **Trampa encontrada**: un DTO `record` con `[Required]` en la propiedad (no en el parámetro del constructor) lanza `InvalidOperationException` en MVC. Para las pruebas se usa una clase con propiedades.
- **Alternativas**: un `JsonConverter` o un formateador propio: descartado, demasiado código; no hacer nada: incumple FR-010a.

## D5. Scalar, OpenAPI y CORS solo en Development (FR-017 a FR-020)

- **Decisión**: añadir `Scalar.AspNetCore` 2.18.7 a Api. En Development: `app.MapOpenApi()` (ya existe), `app.MapScalarApiReference()` y `app.UseCors()`, con `AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))` registrado solo si el entorno es Development. Fuera de Development no se registra ni se usa nada de eso.
- **Hallazgo**: `GET /scalar` responde 302 a `/scalar/` y esa ruta devuelve 200 con la página de Scalar (que carga `/openapi/v1.json`). Una prueba con el cliente por defecto de `WebApplicationFactory` (sigue redirecciones) comprueba 200. `/openapi/v1.json` responde 200.
- **Hallazgo**: en Production, `/scalar`, `/scalar/v1` y `/openapi/v1.json` responden 404 con ProblemDetails (por `UseStatusCodePages`), y ni la petición con `Origin` ni el preflight llevan cabeceras `Access-Control-*`.
- **Hallazgo**: en Development, el preflight (`Origin`, `Access-Control-Request-Method: POST`, `Access-Control-Request-Headers: x-foo`) responde 204 con `Access-Control-Allow-Origin: *`, `Allow-Methods: POST` y `Allow-Headers: x-foo`. Con `AllowAnyOrigin` no se admiten credenciales (límite del framework), como pide la spec.
- **Alternativas**: política CORS con nombre y `UseCors("nombre")`: descartada, la política por defecto es lo más simple con una única política; condicionar con configuración (`appsettings`) en vez de entorno: descartado, el principio VI dice «solo en Development».

## D6. `TimeProvider` (FR-021)

- **Decisión**: `builder.Services.AddSingleton(TimeProvider.System);`.
- **Justificación**: registra la instancia estática, así que toda resolución devuelve `TimeProvider.System` (FR-021 y escenarios 1 y 2 de la historia 5). La prueba lo resuelve desde `factory.Services`.
- **Alternativa**: `AddSingleton<TimeProvider>(_ => TimeProvider.System)` o `TryAddSingleton`: sin diferencia práctica; se elige la forma más corta.

## D7. Reglas de convención por reflexión (FR-013 a FR-016)

- **Decisión**: una clase `ControllerConventionTests` en `MiRestaurante.Architecture.Tests`, con reflexión sobre `Layers.Load(Layers.Api)`. Controlador = tipo público, no abstracto, asignable a `ControllerBase`. Cuatro reglas, cada una con un método de prueba y una sola aserción (`HaveCount(0, …)` sobre la lista de incumplidores, para que el mensaje los nombre):
  1. **`[ApiController]`**: todo controlador lo lleva (`IsDefined(typeof(ApiControllerAttribute), inherit: true)`).
  2. **Prefijo de ruta**: todo controlador tiene al menos un `[Route]`/`IRouteTemplateProvider` de clase y todas las plantillas de clase empiezan por `api/v1/`.
  3. **Plantillas absolutas de acción**: ninguna plantilla de acción que empiece por `/` o `~/` deja de empezar por `api/v1/` una vez quitado ese prefijo (`~/api/v1/x` y `/api/v1/x` son válidas; `/health` no).
  4. **`[ProducesResponseType]`**: toda acción pública declarada en el controlador (método de instancia público, `DeclaredOnly`, sin `[NonAction]`, sin nombre especial) lleva al menos un `ProducesResponseTypeAttribute` (incluida la forma genérica, que hereda de él) **en la propia acción**; un atributo de clase no basta.
- **Hallazgo**: la API es un proyecto `Microsoft.NET.Sdk.Web` y el `FrameworkReference` a `Microsoft.AspNetCore.App` fluye por referencia de proyecto, así que el proyecto de arquitectura puede nombrar `ControllerBase` sin añadir paquetes.
- **Sobre el conjunto vacío**: con cero controladores las consultas LINQ devuelven una lista vacía y las pruebas pasan sin condicionales. Su valor se demuestra en la fase Red con controladores temporales que incumplen cada regla (SC-003).
- **Alternativa**: NetArchTest para estas reglas: descartada, no inspecciona atributos con argumentos ni plantillas de métodos; la reflexión es lo que pide FR-016.

## D8. Pruebas funcionales sin base de datos (FR-022, FR-023)

- **Decisión**:
  - `public partial class Program;` al final de `Program.cs` para que `WebApplicationFactory<Program>` pueda usarlo.
  - Una fábrica `ApiFactory : WebApplicationFactory<Program>` del proyecto de pruebas, con el entorno como parámetro (`Development` o `Production`) y el controlador de pruebas registrado con `AddApplicationPart(typeof(ErrorsController).Assembly)` solo en la fábrica. Así el controlador no está en el ensamblado de la API ni en su documento OpenAPI, y las pruebas de arquitectura (que cargan solo el ensamblado de la API) no lo ven.
  - Un `ILoggerProvider` en memoria de unas pocas líneas, añadido en la fábrica, para verificar FR-011 (nivel y presencia de la excepción) sin Moq, que el principio VII reserva a Application.
- **Controlador de pruebas**: `[ApiController]`, ruta `api/v1/test-errors`, acciones que lanzan `NotFoundException`, `BusinessRuleException`, `BadHttpRequestException` (413), una excepción genérica con un mensaje «secreto», más una acción `POST` con cuerpo (DTO con `[Required]`) para validación, 415 y errores de lectura. Cumple las reglas de convención aunque no las evalúe nadie.
- **Hallazgo**: en `WebApplicationFactory` no se aplica `launchSettings.json`; el entorno se fija con `UseEnvironment`. `UseHttpsRedirection` solo avisa («Failed to determine the https port») y no redirige, así que el cliente no recibe 307.
- **Alternativas**: un `Startup` o ensamblado aparte con el controlador: descartado, más piezas; `InternalsVisibleTo` en vez de `partial class Program`: descartado, la forma documentada es la clase parcial.

## D9. Cobertura y puerta 4

- **Situación**: esta feature añade pruebas reales (Application, Architecture y Api.FunctionalTests) pero Domain e Infrastructure siguen sin pruebas, por lo que se conserva `--ignore-exit-code 8` en `Directory.Build.props`.
- **Decisión**: la puerta de cobertura se ejecuta con los mismos comandos que en la feature 001 y la puerta es bloqueante (ReportGenerator con `minimumCoverageThresholds:lineCoverage=80` debe terminar con código 0), como exige el principio VII.
- **Riesgo**: Domain e Infrastructure no tienen código ni pruebas; si la cobertura global no llega al 80 %, se añaden pruebas de la propia feature antes de integrar (no se relaja la puerta).

## D10. Versiones y comprobaciones de paquetes (principio I)

- `Scalar.AspNetCore` 2.18.7: es la versión de la tabla de la constitución, activa (la restauración en la copia desechable se resolvió sin avisos) y obligatoria por el principio VI. No requiere justificación en Complexity Tracking.
- No se añaden paquetes a los proyectos de pruebas: `Microsoft.AspNetCore.Mvc.Testing` 10.0.14 y AwesomeAssertions ya están en Api.FunctionalTests, y Architecture.Tests ya tiene lo necesario.
