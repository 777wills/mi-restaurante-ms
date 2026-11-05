# Guía de validación: Persistencia y entorno local

Escenarios para comprobar, desde la raíz del repositorio, que la feature está completa. No contiene implementación; las decisiones están en [research.md](research.md), el diseño en [plan.md](plan.md) y los contratos en [contracts/persistence-local-env.md](contracts/persistence-local-env.md).

## Requisitos previos

- SDK de .NET indicado en `global.json`.
- Docker disponible, con el puerto 5432 libre.
- `dotnet tool restore` ejecutado una vez.

## 1. Compilación y formato

```bash
dotnet build MiRestaurante.slnx
dotnet format --verify-no-changes MiRestaurante.slnx
```

**Esperado**: 0 advertencias y 0 errores; sin cambios de formato pendientes (incluidos los archivos de la migración).

## 2. Persistencia del volumen (US1, escenario 4)

```bash
docker compose up -d
dotnet run --project src/MiRestaurante.Api --launch-profile http
# en otra terminal, tras el arranque
docker compose exec postgres psql -U mirestaurante -d mirestaurante -c 'select "MigrationId" from "__EFMigrationsHistory"'
```

**Esperado**: un único contenedor `postgres:18` en el puerto 5432 (`docker compose ps`); la API arranca sin configuración adicional y la consulta devuelve una fila con `…_InitialCreate`. En el primer arranque EF puede registrar un `fail` por el `SELECT` a `__EFMigrationsHistory` antes de crearla: es normal.

Detener la API, ejecutar `docker compose down`, volver a `docker compose up -d` y repetir la consulta.

**Esperado**: la fila sigue presente. Arrancar la API otra vez: no vuelve a aplicar `InitialCreate` (US1, escenario 3).

## 3. Endpoint de salud (US2)

Con la API en marcha (paso 2) y la base de datos arriba:

```bash
curl -si http://localhost:5110/health
docker compose stop postgres
curl -si http://localhost:5110/health
docker compose start postgres
```

**Esperado**: la primera respuesta es `200` con cuerpo `Healthy`; la segunda, `503` con cuerpo `Unhealthy`, ambas `text/plain` y sin detalles internos.

## 4. Migraciones por entorno y fallo de arranque (US3 y US1, escenario 5)

Con la base de datos recién recreada (`docker compose down -v && docker compose up -d`), arrancar fuera de Development:

```bash
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://localhost:5198 ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=mirestaurante;Username=mirestaurante;Password=mirestaurante_dev" dotnet run --project src/MiRestaurante.Api --no-launch-profile
# en otra terminal
docker compose exec postgres psql -U mirestaurante -d mirestaurante -c '\dt'
curl -si http://localhost:5198/health
```

**Esperado**: la base no tiene tablas (`Did not find any relations`) y `/health` responde `200 Healthy`. Detener la API.

Después, con `docker compose stop postgres`, arrancar en Development (`--launch-profile http`).

**Esperado**: el arranque falla con una excepción no controlada y un registro `fail: Microsoft.EntityFrameworkCore.Database.Connection`. Volver a levantar la base con `docker compose start postgres`.

## 5. Script idempotente (US3, escenario 3)

```bash
dotnet ef migrations script --idempotent --project src/MiRestaurante.Infrastructure --startup-project src/MiRestaurante.Api
```

**Esperado**: código de salida 0 y un script SQL (con la migración vacía, solo crea `__EFMigrationsHistory` si no existe).

## 6. Regla de arquitectura en Red (US5)

Añadir temporalmente a `src/MiRestaurante.Api/` un controlador que herede de `ControllerBase`, en un namespace cualquiera, con un constructor que reciba `IApplicationDbContext`, y ejecutar:

```bash
dotnet test --project tests/MiRestaurante.Architecture.Tests --filter-class "*ControllerDataAccessTests"
```

**Esperado**: falla y el mensaje nombra el controlador. Borrar el controlador y repetir: pasa (también con cero controladores).

## 7. Suite completa y contenedores de prueba (US4)

Con `docker compose down` ejecutado (ningún contenedor levantado a mano):

```bash
dotnet test --solution MiRestaurante.slnx
docker ps --filter ancestor=postgres:18
```

**Esperado**: código de salida 0; las pruebas de integración y funcionales crean su propio PostgreSQL 18 y, al terminar, `docker ps` no lista ningún contenedor.

## 8. Puerta de cobertura

```bash
dotnet tool restore
rm -rf TestResults
dotnet test --solution MiRestaurante.slnx --results-directory TestResults --coverlet --coverlet-output-format cobertura
dotnet reportgenerator -reports:"TestResults/coverage.cobertura.*.xml" -targetdir:TestResults/report -reporttypes:"TextSummary;Html" "-assemblyfilters:+MiRestaurante.Domain;+MiRestaurante.Application;+MiRestaurante.Infrastructure;+MiRestaurante.Api" minimumCoverageThresholds:lineCoverage=80
```

**Esperado**: ReportGenerator termina con código 0 y `TestResults/report/Summary.txt` indica ≥ 80 % de líneas, sin exclusiones (el prototipo midió 98,4 %). La puerta es obligatoria en esta feature.
