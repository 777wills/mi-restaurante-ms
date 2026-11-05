# Tasks: Convenciones HTTP transversales

**Entrada**: documentos de diseño en `/specs/002-http-conventions/`

**Prerequisitos**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/http-conventions.md`, `quickstart.md`

**Pruebas**: incluidas. El principio VII (TDD, no negociable) y FR-022 a FR-024 las exigen: cada prueba se escribe antes de su implementación y se ejecuta en Red. Nomenclatura `{Metodo}_Should_{Resultado}_When_{Escenario}`, una sola aserción lógica y ninguna lógica condicional (`if`, `switch`, bucles) en las pruebas; sin Moq en las funcionales.

**Organización**: tareas agrupadas por historia de usuario. Todo el código, los identificadores y las rutas van en inglés; los títulos, los mensajes al usuario y los artefactos, en español.

## Formato: `[ID] [P?] [Story] Descripción`

- **[P]**: se puede ejecutar en paralelo (archivos distintos, sin dependencia de tareas incompletas).
- **[Story]**: historia de usuario a la que pertenece (US1 a US5).
- Todas las rutas son relativas a la raíz del repositorio.

## Fase 1: Preparación

**Propósito**: dependencias y línea base.

- [X] T001 Añadir `<PackageReference Include="Scalar.AspNetCore" Version="2.18.7" />` a `src/MiRestaurante.Api/MiRestaurante.Api.csproj` (única dependencia nueva, exigida por el principio VI y la tabla del stack) y restaurar con `dotnet restore MiRestaurante.slnx` sin advertencias de vulnerabilidades.
- [X] T002 Ejecutar `dotnet build MiRestaurante.slnx` y `dotnet test --solution MiRestaurante.slnx` para confirmar la línea base en verde (0 advertencias, código de salida 0) antes de modificar código.

## Fase 2: Fundamentos

**Propósito**: excepciones de negocio y andamiaje de pruebas funcionales que necesitan las historias 1, 2, 4 y 5.

**⚠️ CRÍTICO**: ninguna historia puede empezar hasta completar esta fase.

- [X] T003 Escribir `tests/MiRestaurante.Application.UnitTests/Exceptions/ExceptionsTests.cs` (Red, FR-001): `Constructor_Should_KeepMessage_When_NotFoundExceptionIsCreated`, `Constructor_Should_KeepMessage_When_BusinessRuleExceptionIsCreated`, `NotFoundException_Should_BeAssignableToNotFoundException_When_DerivedTypeIsUsed` y su equivalente para `BusinessRuleException`, con una clase derivada definida dentro del archivo de pruebas. Ejecutar `dotnet test --project tests/MiRestaurante.Application.UnitTests` y confirmar que falla por compilación (los tipos no existen).
- [X] T004 Crear `src/MiRestaurante.Application/Exceptions/NotFoundException.cs` y `src/MiRestaurante.Application/Exceptions/BusinessRuleException.cs` (FR-001, FR-002): clases públicas **no selladas** en `MiRestaurante.Application.Exceptions`, base `Exception`, único constructor `(string message)`, namespace de ámbito de archivo y sin referencias fuera de `System`. Confirmar Green de T003 y de `ApplicationDependencyTests` en `tests/MiRestaurante.Architecture.Tests`.
- [X] T005 Añadir `public partial class Program;` al final de `src/MiRestaurante.Api/Program.cs` para que `WebApplicationFactory<Program>` pueda usarlo (D8). No tocar `UseHttpsRedirection`, `UseAuthorization` ni los comentarios de plantilla (fuera de alcance).
- [X] T006 Crear `tests/MiRestaurante.Api.FunctionalTests/ApiFactory.cs` (depende de T008, cuyo `ErrorsController` registra): `ApiFactory : WebApplicationFactory<Program>` con el nombre del entorno como parámetro (fijado con `UseEnvironment`), que registra el controlador de pruebas solo en la fábrica con `AddApplicationPart(typeof(ErrorsController).Assembly)` y añade `CapturingLoggerProvider`; más dos clases derivadas mínimas `DevelopmentApiFactory` y `ProductionApiFactory` para usarlas con `IClassFixture<T>`. No requiere PostgreSQL ni `compose.yaml`.
- [X] T007 [P] Crear `tests/MiRestaurante.Api.FunctionalTests/CapturingLoggerProvider.cs`: `ILoggerProvider` en memoria que conserva por entrada el nivel, la categoría, el mensaje formateado y la excepción (D8, FR-011), de unas pocas líneas y sin paquetes nuevos.
- [X] T008 [P] Crear `tests/MiRestaurante.Api.FunctionalTests/TestControllers/ErrorsController.cs`: controlador `[ApiController]` con `[Route("api/v1/test-errors")]` que solo existe en este proyecto. Acciones GET que lanzan `NotFoundException`, `BusinessRuleException`, una derivada de cada una (clases anidadas del propio archivo), `BadHttpRequestException` con 413 y `InvalidOperationException` con el mensaje «secreto-interno»; una acción POST que recibe una **clase** (no `record`) con `[Required(ErrorMessage = "El nombre es obligatorio")]` en un `string` y una propiedad `decimal` para provocar errores de tipo (D4). Cada acción lleva `[ProducesResponseType]` y ninguna contiene `try`/`catch` (FR-012). Incluir también una acción GET que lance `NotFoundException("")` (mensaje vacío, caso límite).
- [X] T009 Ejecutar `dotnet build MiRestaurante.slnx` y confirmar 0 advertencias y que `tests/MiRestaurante.Api.FunctionalTests` compila (la referencia a `ControllerBase` fluye por la referencia de proyecto a la API).

**Punto de control**: excepciones disponibles y andamiaje de pruebas funcionales compilando.

## Fase 3: Historia de usuario 1 — Errores de negocio y fallos como ProblemDetails (P1) 🎯 MVP

**Objetivo**: un único `IExceptionHandler` traduce `NotFoundException` a 404, `BusinessRuleException` a 422, `BadHttpRequestException` a su propio 4xx y cualquier otra excepción a 500 sin datos internos, siempre como ProblemDetails con `traceId`.

**Prueba independiente**: con `DevelopmentApiFactory` y `ProductionApiFactory` y el controlador de pruebas, provocar cada excepción y comprobar código, formato, título, `detail`, `traceId` y registro.

### Pruebas (Red)

- [X] T010 [US1] Escribir `tests/MiRestaurante.Api.FunctionalTests/ExceptionHandlingTests.cs` con un caso por escenario, cada uno con una sola aserción: 404 (código), título «Recurso no encontrado», `detail` igual al mensaje; 422 (código), título «Regla de negocio incumplida», `detail` igual al mensaje; 500 con título «Error interno del servidor»; el cuerpo 500 no contiene «secreto-interno» ni `InvalidOperationException` ni traza de pila ni `detail`, en Development y en Production (SC-002); `BadHttpRequestException` 413 devuelve el código 413, título «Solicitud no válida» y sin `detail`; derivadas de `NotFoundException` y `BusinessRuleException` devuelven 404 y 422; todas las respuestas llevan `traceId` y ninguna `instance` (FR-007, FR-007a); `Content-Type` `application/problem+json`; logs vía `CapturingLoggerProvider`: 500 a nivel Error con la excepción, 404, 422 y 413 a nivel Information sin excepción adjunta (FR-011); exactamente un `IExceptionHandler` registrado, resuelto desde `factory.Services` (SC-005); `NotFoundException("")` devuelve 404 con el título «Recurso no encontrado» y sin datos internos en el cuerpo (caso límite «Mensaje vacío»). Cada comprobación enumerada es un `[Fact]` o `[Theory]` independiente con una sola aserción lógica (principio VII).
- [X] T011 [US1] Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*ExceptionHandlingTests"` y confirmar Red (las excepciones llegan como 500 por defecto o sin ProblemDetails).

### Implementación

- [X] T012 [P] [US1] Crear `src/MiRestaurante.Api/ErrorHandling/ProblemDetailsTitles.cs` con la tabla de FR-008 (400 «Uno o más errores de validación», 404 «Recurso no encontrado», 405 «Método no permitido», 415 «Tipo de contenido no admitido», 422 «Regla de negocio incumplida», 500 «Error interno del servidor») y el método de personalización para `ProblemDetailsOptions.CustomizeProblemDetails`: si `context.Exception` es `BadHttpRequestException`, título «Solicitud no válida»; si no, el título de la tabla por `Status`; si el código no está en la tabla, no se toca. No se personaliza `type` ni se añade `instance` (D3, FR-004a, FR-007a).
- [X] T013 [P] [US1] Crear `src/MiRestaurante.Api/ErrorHandling/ApiExceptionHandler.cs`: `IExceptionHandler` con constructor primario que inyecta `IProblemDetailsService` y `ILogger<ApiExceptionHandler>`. Decisión por tipo (D2, FR-004 a FR-006, FR-011): `NotFoundException` → 404 con `detail` = mensaje, Information; `BusinessRuleException` → 422 con `detail` = mensaje, Information; `BadHttpRequestException` → su `StatusCode`, sin `detail`, Information sin traza; cualquier otra → 500 sin `detail`, Error con la excepción completa. Registro con plantillas de mensaje (sin interpolación), fija `Response.StatusCode`, escribe con `TryWriteAsync` pasando `Exception` en el `ProblemDetailsContext`, no fija el título y devuelve su resultado. Namespace de ámbito de archivo.
- [X] T014 [US1] Editar `src/MiRestaurante.Api/Program.cs` (depende de T012 y T013): `builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ProblemDetailsTitles.…)`, `builder.Services.AddExceptionHandler<ApiExceptionHandler>()` y `app.UseExceptionHandler()` sin argumentos como primer middleware del pipeline. No usar `UseDeveloperExceptionPage`.
- [X] T015 [US1] Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*ExceptionHandlingTests"` y confirmar Green; comprobar que `ErrorsController` no aparece en el ensamblado de la API.

**Punto de control**: la historia 1 es funcional y verificable por sí sola (MVP).

## Fase 4: Historia de usuario 2 — Contrato de errores uniforme y en español (P1)

**Objetivo**: los errores que genera el framework (ruta inexistente, 405, 415 y validación, incluidos los errores de lectura del cuerpo) llegan como ProblemDetails con el título fijo en español y mensajes genéricos sin datos internos.

**Prueba independiente**: pedir una ruta inexistente, un método no admitido, un tipo de contenido no admitido y cuerpos inválidos, y comprobar formato y título de cada respuesta.

**Dependencia**: usa `ProblemDetailsTitles` (T012) y `Program.cs` de la historia 1.

### Pruebas (Red)

- [X] T016 [US2] Escribir `tests/MiRestaurante.Api.FunctionalTests/FrameworkErrorTests.cs` con una aserción por caso: ruta inexistente → 404, `application/problem+json`, título «Recurso no encontrado» y `traceId`; también fuera de `/api/v1/`; `DELETE` sobre una ruta GET existente → 405 con título «Método no permitido»; POST con `Content-Type: text/plain` → 415 con título «Tipo de contenido no admitido»; cuerpo que incumple `[Required]` → 400 con título «Uno o más errores de validación», `errors` por campo con «El nombre es obligatorio» y `traceId`; JSON mal formado, valor de tipo incorrecto (`"price":"abc"`), cuerpo vacío, `null` y raíz de tipo incorrecto (`[1]`) → 400 con título «Uno o más errores de validación» y mensajes «El cuerpo de la petición no tiene un formato válido.» o «Se requiere un cuerpo en la petición.»; esos mensajes no contienen «System.», «MiRestaurante», «Path:», «LineNumber» ni «BytePositionInLine»; ningún error en inglés «is required» (FR-009, FR-010, FR-010a). Cada comprobación enumerada es un `[Fact]` o `[Theory]` independiente; las listas de textos prohibidos se expresan como `[Theory]` con `[InlineData]` y una aserción `NotContain` (principio VII).
- [X] T017 [US2] Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*FrameworkErrorTests"` y confirmar Red (404 y 405 sin cuerpo, mensajes del framework en inglés).

### Implementación

- [X] T018 [P] [US2] Crear `src/MiRestaurante.Api/ErrorHandling/RequestBodyMessages.cs` (D4, FR-010a): constantes «El cuerpo de la petición no tiene un formato válido.» y «Se requiere un cuerpo en la petición.», y el método que envuelve `ApiBehaviorOptions.InvalidModelStateResponseFactory`: antes de invocar la fábrica por defecto sustituye los errores de las entradas de `ModelState` cuya clave empieza por `$` por el primer mensaje, conservando las claves; la respuesta (título, `type`, `traceId`) la sigue generando la fábrica por defecto.
- [X] T019 [US2] Editar `src/MiRestaurante.Api/Program.cs` (depende de T018): `AddControllers(o => …)` con `o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true` y `o.ModelBindingMessageProvider.SetMissingRequestBodyRequiredValueAccessor(() => "Se requiere un cuerpo en la petición.")`; `.ConfigureApiBehaviorOptions(…)` con la fábrica envuelta; y `app.UseStatusCodePages()` justo después de `UseExceptionHandler()` (FR-009).
- [X] T020 [US2] Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*FrameworkErrorTests"` y `--filter-class "*ExceptionHandlingTests"`; confirmar Green de ambas.

**Punto de control**: las historias 1 y 2 responden todo error como ProblemDetails en español.

## Fase 5: Historia de usuario 3 — Convención de rutas y contrato verificada automáticamente (P2)

**Objetivo**: la suite de arquitectura falla si un controlador incumple `[ApiController]`, el prefijo `api/v1/`, las plantillas absolutas de acción o `[ProducesResponseType]`.

**Prueba independiente**: añadir temporalmente un controlador que incumple cada regla y comprobar que la suite falla nombrándolo; al quitarlo, pasa (también con cero controladores).

**Dependencia**: solo la Preparación; no usa el andamiaje funcional.

- [X] T021 [US3] Crear `tests/MiRestaurante.Architecture.Tests/ControllerConventionTests.cs` con reflexión sobre `Layers.Load(Layers.Api)` (D7). Controlador = tipo público, no abstracto, asignable a `ControllerBase`. Cuatro pruebas, una por regla, con una sola aserción `HaveCount(0, …)` sobre la lista de incumplidores para que el mensaje los nombre y sin condicionales (LINQ): `Controllers_Should_HaveApiControllerAttribute_When_Compiled`; `Controllers_Should_HaveRouteStartingWithApiV1_When_Compiled` (al menos un `IRouteTemplateProvider` de clase y todas las plantillas empiezan por `api/v1/`); `Actions_Should_NotDeclareAbsoluteRouteOutsideApiV1_When_Compiled` (tras quitar `/` o `~/`, la plantilla empieza por `api/v1/`; `/health` incumple); `Actions_Should_DeclareProducesResponseType_When_Public` (acciones públicas de instancia, `DeclaredOnly`, sin `[NonAction]`, sin nombre especial, con al menos un `ProducesResponseTypeAttribute` en la propia acción, incluida la forma genérica). Las pruebas deben pasar sobre el conjunto vacío.
- [X] T022 [US3] Demostrar Red (SC-003): crear temporalmente en `src/MiRestaurante.Api/Controllers/` cuatro controladores que incumplan cada uno una regla (sin `[ApiController]`; con `[Route("v2/x")]`; con una acción `[HttpGet("/health")]`; con una acción pública sin `[ProducesResponseType]`), ejecutar `dotnet test --project tests/MiRestaurante.Architecture.Tests --filter-class "*ControllerConventionTests"` y confirmar que falla una prueba por regla y que cada mensaje nombra el controlador o la acción.
- [X] T023 [US3] Borrar `src/MiRestaurante.Api/Controllers/` y los controladores temporales, y ejecutar `dotnet test --project tests/MiRestaurante.Architecture.Tests` para confirmar Green de las cuatro pruebas y de las reglas existentes; comprobar con `git status` que no queda rastro.

**Punto de control**: las reglas de controladores están activas y probadas en Red y Green.

## Fase 6: Historia de usuario 4 — Documentación interactiva y CORS solo en Development (P2)

**Objetivo**: Scalar, OpenAPI y CORS abierto existen solo en Development; fuera de él, `/scalar` y el documento OpenAPI devuelven 404 ProblemDetails y no hay cabeceras CORS.

**Prueba independiente**: arrancar la API en Development y en Production y comprobar en cada caso Scalar, OpenAPI y CORS.

**Dependencia**: T001 (paquete), T006 (fábricas) y T019 (`UseStatusCodePages`, necesario para el 404 ProblemDetails fuera de Development).

### Pruebas (Red)

- [X] T024 [P] [US4] Crear `tests/MiRestaurante.Api.FunctionalTests/DevelopmentToolsTests.cs` con `DevelopmentApiFactory`: `GET /scalar` devuelve 200 siguiendo la redirección a `/scalar/` y la página referencia `/openapi/v1.json` (FR-017); `GET /openapi/v1.json` devuelve 200; el preflight (`OPTIONS` con `Origin`, `Access-Control-Request-Method: POST` y `Access-Control-Request-Headers: x-foo`) devuelve `Access-Control-Allow-Origin: *`, el método y la cabecera pedidos, y ninguna cabecera `Access-Control-Allow-Credentials` (FR-019).
- [X] T025 [P] [US4] Crear `tests/MiRestaurante.Api.FunctionalTests/ProductionEnvironmentTests.cs` con `ProductionApiFactory`: `/scalar` y `/openapi/v1.json` devuelven 404 con `application/problem+json` (FR-018); una petición con `Origin` y un preflight no incluyen ninguna cabecera `Access-Control-*` (FR-020).
- [X] T026 [US4] Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*DevelopmentToolsTests"` y `--filter-class "*ProductionEnvironmentTests"`; confirmar Red de la primera (`/scalar` 404, sin CORS). Registrar el resultado de la segunda como línea base.

### Implementación

- [X] T027 [US4] Editar `src/MiRestaurante.Api/Program.cs` (D5): si el entorno es Development, `AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))` sin credenciales; en el pipeline, dentro del bloque Development existente, mantener `app.MapOpenApi()`, añadir `app.MapScalarApiReference()` (ruta por defecto `/scalar`, sin personalización visual) y `app.UseCors()`, antes de `UseHttpsRedirection`. No tocar los perfiles de `launchSettings.json`. Fuera de Development no se registra ni se usa nada de esto.
- [X] T028 [US4] Ejecutar ambas clases de pruebas y confirmar Green.

**Punto de control**: las herramientas de desarrollo solo existen en Development.

## Fase 7: Historia de usuario 5 — Hora actual inyectable (P3)

**Objetivo**: el contenedor proporciona `TimeProvider.System` como singleton.

**Prueba independiente**: resolver `TimeProvider` dos veces desde `factory.Services` y comprobarlo.

**Dependencia**: T006.

- [X] T029 [US5] Crear `tests/MiRestaurante.Api.FunctionalTests/TimeProviderTests.cs` (FR-021): `Resolve_Should_ReturnSystemTimeProvider_When_ServiceIsRequested` (`BeSameAs(TimeProvider.System)`) y `Resolve_Should_ReturnSameInstance_When_ServiceIsRequestedTwice`, una aserción cada una.
- [X] T030 [US5] Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*TimeProviderTests"` y confirmar Red (el servicio no está registrado).
- [X] T031 [US5] Editar `src/MiRestaurante.Api/Program.cs`: añadir `builder.Services.AddSingleton(TimeProvider.System);` (D6).
- [X] T032 [US5] Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*TimeProviderTests"` y confirmar Green.

**Punto de control**: las cinco historias son funcionales.

## Fase final: Pulido y validación transversal

**Propósito**: puertas de calidad y revisión estática de la feature.

- [X] T033 Ejecutar `dotnet format MiRestaurante.slnx` y luego `dotnet format --verify-no-changes MiRestaurante.slnx`; debe terminar sin cambios pendientes.
- [X] T034 Ejecutar `dotnet build MiRestaurante.slnx` (0 advertencias, 0 errores) y `dotnet test --solution MiRestaurante.slnx` (código de salida 0) (SC-006).
- [X] T035 Ejecutar la puerta de cobertura de `CLAUDE.md` (`dotnet tool restore`, `rm -rf TestResults`, prueba con `--coverlet`, ReportGenerator con `minimumCoverageThresholds:lineCoverage=80`) e informar el resultado de `TestResults/report/Summary.txt`. La puerta es bloqueante (principio VII): ReportGenerator DEBE terminar con código 0; si no alcanza el 80 %, añadir pruebas antes de integrar. Comprobar que `git status` no muestra `TestResults/`.
- [X] T036 Revisión estática (FR-012, SC-005): `grep -rnw "try\|catch" src/MiRestaurante.Api tests/MiRestaurante.Api.FunctionalTests/TestControllers` sin coincidencias (comprobación manual deliberada, no una regla automática) en controladores, un único `IExceptionHandler` en `src`, y el ensamblado de la API sin `ErrorsController` (ni en su `openapi/v1.json`).
- [X] T037 Ejecutar los escenarios 1 a 6 de `specs/002-http-conventions/quickstart.md` contra la API arrancada (`curl` en Development y Production) y confirmar los resultados esperados.

## Dependencias y orden de ejecución

### Dependencias entre fases

- **Preparación (Fase 1)**: sin dependencias.
- **Fundamentos (Fase 2)**: depende de la Preparación y **bloquea US1, US2, US4 y US5**. US3 solo necesita la Preparación (T001/T002).
- **US1 (P1)**: depende de los Fundamentos. Es el MVP.
- **US2 (P1)**: depende de US1 (reutiliza `ProblemDetailsTitles` y el registro de `AddProblemDetails` en `Program.cs`).
- **US3 (P2)**: independiente del resto; puede hacerse en cualquier momento tras la Preparación.
- **US4 (P2)**: depende de los Fundamentos y de US2 (`UseStatusCodePages` para el 404 ProblemDetails fuera de Development).
- **US5 (P3)**: depende solo de los Fundamentos (T006).
- **Pulido**: depende de las historias completadas.

### Dentro de cada historia

- La prueba se escribe y se ejecuta en Red antes de implementar.
- Las tareas que editan `src/MiRestaurante.Api/Program.cs` (T005, T014, T019, T027, T031) NO son paralelizables entre sí: aplicar en orden de fase aunque las historias se repartan.

### Oportunidades de paralelismo

- Fundamentos: T007 y T008 (archivos distintos) en paralelo tras T004 y T005; T006 después de T008.
- US1: T012 y T013 (archivos distintos).
- US3 completa en paralelo con US1/US2 (proyecto de pruebas distinto y sin tocar `Program.cs`).
- US4: T024 y T025 (archivos distintos).
- US5 en paralelo con US1 y US2 si se coordina la edición de `Program.cs`.

## Ejemplos de ejecución en paralelo

```text
# Fundamentos, tras T004 y T005 (T006 después de T008):
Tarea: "CapturingLoggerProvider.cs en tests/MiRestaurante.Api.FunctionalTests"  (T007)
Tarea: "ErrorsController.cs en tests/MiRestaurante.Api.FunctionalTests/TestControllers"  (T008)
Tarea: "ApiFactory.cs en tests/MiRestaurante.Api.FunctionalTests"          (T006, tras T008)

# US1, implementación:
Tarea: "ProblemDetailsTitles.cs en src/MiRestaurante.Api/ErrorHandling"   (T012)
Tarea: "ApiExceptionHandler.cs en src/MiRestaurante.Api/ErrorHandling"    (T013)

# US4, pruebas:
Tarea: "DevelopmentToolsTests.cs"          (T024)
Tarea: "ProductionEnvironmentTests.cs"     (T025)
```

## Estrategia de implementación

### MVP primero (solo historia 1)

1. Fase 1: Preparación.
2. Fase 2: Fundamentos.
3. Fase 3: US1.
4. **Parar y validar**: el mapeo 404/422/500/4xx funciona de extremo a extremo.

### Entrega incremental

1. Preparación y Fundamentos.
2. US1 (MVP), luego US2 para completar el contrato de errores.
3. US3 (reglas de arquitectura) en cualquier punto.
4. US4 (Scalar, OpenAPI y CORS), luego US5 (`TimeProvider`).
5. Pulido y puertas de calidad; la cobertura informa, sin bloquear (excepción solo para esta feature).

## Notas

- Cada DTO futuro debe llevar `[Required(ErrorMessage = "…")]` en español porque `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes` queda activo (D4). El DTO de pruebas usa una clase con propiedades, no un `record` (trampa de D4).
- Los títulos de FR-008 son valores exactos del contrato; las pruebas los comparan literalmente.
- No introducir `try`/`catch` en controladores, ni paquetes nuevos de pruebas, ni proyectos nuevos.
- Confirmar la fase Red antes de cada implementación y hacer commit tras cada tarea o grupo lógico (Conventional Commits).

## Phase 8: Convergence

- [X] T038 CRITICAL: Sustituir en `tests/MiRestaurante.Api.FunctionalTests/FrameworkErrorTests.cs` el generador `ForbiddenTextsPerBody` (dos `foreach` anidados + `[MemberData]`) por `[Theory]` con `[InlineData(body, forbidden)]` explícito, sin bucles, para cumplir el principio VII y T016 per Constitution VII (contradicts)
- [X] T039 CRITICAL: Eliminar el operador condicional `? :` del helper `WithoutAbsolutePrefix` en `tests/MiRestaurante.Architecture.Tests/ControllerConventionTests.cs` (por ejemplo, con `TrimStart('~').TrimStart('/')` o `Regex`) para que la prueba no contenga lógica condicional per Constitution VII / FR-024 (contradicts)
