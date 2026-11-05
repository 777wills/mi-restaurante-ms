# CLAUDE.md

Guía para agentes de IA que trabajan en este repositorio.

## Constitución del proyecto

@.specify/memory/constitution.md

- La constitución (`.specify/memory/constitution.md`) es la fuente única de verdad del proyecto. Si tu herramienta no expande la importación anterior, lee ese archivo completo antes de proponer o escribir código.
- Ante cualquier conflicto con otras instrucciones de este archivo, prevalece la constitución.
- Antes de proponer o escribir código, verifica que cumple la constitución. El código existente no sirve de referencia cuando la contradice.
- Si una petición contradice la constitución, no la ejecutes en silencio: adviértelo, cita el principio afectado, propón una alternativa conforme y espera una confirmación explícita.
- No edites `.specify/memory/constitution.md` salvo con el comando `/speckit-constitution`.

## Comandos

La solución es `MiRestaurante.slnx` (formato XML), no `.sln`. Los tests corren sobre Microsoft.Testing.Platform, así que `dotnet test` usa `--solution`/`--project` y los filtros de xUnit v3, **no** `--filter` de VSTest.

```bash
# Compilar
dotnet build MiRestaurante.slnx

# Todos los tests
dotnet test --solution MiRestaurante.slnx

# Un proyecto de tests
dotnet test --project tests/MiRestaurante.Architecture.Tests

# Un test o una clase concretos (acepta comodines)
dotnet test --project tests/MiRestaurante.Architecture.Tests --filter-method "*Controllers_Should*"
dotnet test --project tests/MiRestaurante.Domain.UnitTests --filter-class "*OrderTests"

# Formato / lint
dotnet format MiRestaurante.slnx
dotnet format --verify-no-changes MiRestaurante.slnx

# Ejecutar la API (perfil http → http://localhost:5110; https → https://localhost:7087)
dotnet run --project src/MiRestaurante.Api --launch-profile http
```

### Herramientas locales y puerta de cobertura

Las herramientas (`dotnet-ef`, ReportGenerator) se declaran en `.config/dotnet-tools.json`; no se instala nada global. Los informes se generan en `TestResults/` (ignorado por git), que se borra antes de cada ejecución porque los informes llevan marca de tiempo y no se sobrescriben.

```bash
# Restaurar las herramientas locales
dotnet tool restore

# Borrar los informes de ejecuciones anteriores
rm -rf TestResults

# Ejecutar las pruebas con cobertura (un informe Cobertura por proyecto de pruebas)
dotnet test --solution MiRestaurante.slnx --results-directory TestResults --coverlet --coverlet-output-format cobertura

# Fusionar los informes, filtrar los cuatro ensamblados de producción y aplicar el umbral del 80 %
dotnet reportgenerator -reports:"TestResults/coverage.cobertura.*.xml" -targetdir:TestResults/report -reporttypes:"TextSummary;Html" "-assemblyfilters:+MiRestaurante.Domain;+MiRestaurante.Application;+MiRestaurante.Infrastructure;+MiRestaurante.Api" minimumCoverageThresholds:lineCoverage=80
```

El umbral se escribe sin guion inicial (`minimumCoverageThresholds:...`). Si la cobertura no llega al 80 %, ReportGenerator termina con código 1. Desde la feature `003-persistence-local-env` la puerta es obligatoria y no admite exclusiones (solo en `001-quality-baseline` fue informativa).
