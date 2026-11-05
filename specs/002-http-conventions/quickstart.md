# Guía de validación: Convenciones HTTP transversales

Escenarios para comprobar, desde la raíz del repositorio, que la feature está completa. No contiene implementación; las decisiones están en [research.md](research.md), el diseño en [plan.md](plan.md) y el contrato en [contracts/http-conventions.md](contracts/http-conventions.md).

## Requisitos previos

- SDK de .NET indicado en `global.json`.
- Docker no es necesario (la feature no usa base de datos).
- `dotnet tool restore` ejecutado una vez.

## 1. Compilación y formato

```bash
dotnet build MiRestaurante.slnx
dotnet format --verify-no-changes MiRestaurante.slnx
```

**Esperado**: 0 advertencias y 0 errores; sin cambios de formato pendientes.

## 2. Suite completa (historias 1, 2, 3, 4 y 5)

```bash
dotnet test --solution MiRestaurante.slnx
```

**Esperado**: código de salida 0. Las pruebas funcionales cubren el mapeo de excepciones, los errores del framework, Scalar/OpenAPI/CORS en Development y en Production, y `TimeProvider`; las de arquitectura cubren las reglas de controladores; las de Application cubren las excepciones.

## 3. Convenciones de controladores: fase Red (historia 3)

Añadir temporalmente a `src/MiRestaurante.Api/Controllers/` cuatro controladores que incumplan, cada uno, una regla (sin `[ApiController]`; con `[Route("v2/x")]`; con una acción `[HttpGet("/health")]`; con una acción pública sin `[ProducesResponseType]`) y ejecutar:

```bash
dotnet test --project tests/MiRestaurante.Architecture.Tests --filter-class "*ControllerConventionTests"
```

**Esperado**: falla una prueba por regla y cada mensaje nombra el controlador o la acción. Borrar los controladores temporales y repetir: **Esperado**: pasan (con cero controladores también).

## 4. Errores en la API arrancada (historias 1 y 2)

No hay controladores de negocio, así que solo se comprueban los errores del framework:

```bash
dotnet run --project src/MiRestaurante.Api --launch-profile http
# en otra terminal
curl -i http://localhost:5110/no-existe
curl -i -X DELETE http://localhost:5110/openapi/v1.json
```

**Esperado**: ambas respuestas son `application/problem+json`; la primera 404 con el título «Recurso no encontrado» y `traceId`; la segunda 405 con «Método no permitido». La traducción de `NotFoundException`, `BusinessRuleException`, `BadHttpRequestException` y 500 se verifica en las pruebas funcionales con el controlador de pruebas.

## 5. Scalar, OpenAPI y CORS (historia 4)

Con la API en Development (paso 4):

```bash
curl -sL -o /dev/null -w "%{http_code} %{url_effective}\n" http://localhost:5110/scalar
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5110/openapi/v1.json
curl -si -X OPTIONS -H "Origin: http://x.com" -H "Access-Control-Request-Method: POST" -H "Access-Control-Request-Headers: x-foo" http://localhost:5110/no-existe | grep -i "^access-control"
```

**Esperado**: `/scalar` termina en `200 http://localhost:5110/scalar/` (tras una redirección 302); el documento OpenAPI responde 200; el preflight devuelve `Access-Control-Allow-Origin: *` y permite el método y la cabecera pedidos.

Fuera de Development:

```bash
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://localhost:5198 dotnet run --project src/MiRestaurante.Api --no-launch-profile
# en otra terminal
curl -i http://localhost:5198/scalar
curl -i http://localhost:5198/openapi/v1.json
curl -i -H "Origin: http://x.com" http://localhost:5198/no-existe
```

**Esperado**: las dos primeras responden 404 ProblemDetails; la tercera no incluye cabeceras `Access-Control-*`.

## 6. `TimeProvider` (historia 5)

Cubierto por la prueba funcional que resuelve `TimeProvider` dos veces desde el contenedor y comprueba que es `TimeProvider.System` y la misma instancia.

```bash
dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*TimeProviderTests"
```

## 7. Puerta de cobertura

```bash
rm -rf TestResults
dotnet test --solution MiRestaurante.slnx --results-directory TestResults --coverlet --coverlet-output-format cobertura
dotnet reportgenerator -reports:"TestResults/coverage.cobertura.*.xml" -targetdir:TestResults/report -reporttypes:"TextSummary;Html" "-assemblyfilters:+MiRestaurante.Domain;+MiRestaurante.Application;+MiRestaurante.Infrastructure;+MiRestaurante.Api" minimumCoverageThresholds:lineCoverage=80
cat TestResults/report/Summary.txt
```

**Esperado**: la cobertura de líneas es ≥ 80 % y ReportGenerator termina con código 0; si termina con código 1, la integración queda bloqueada. `git status` no muestra `TestResults/`.
