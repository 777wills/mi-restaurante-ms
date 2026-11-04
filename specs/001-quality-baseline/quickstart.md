# Guía de validación: Base de calidad y limpieza del esqueleto

Escenarios para comprobar, desde la raíz del repositorio, que la feature está completa. No contiene implementación; las decisiones están en [research.md](research.md) y el diseño en [plan.md](plan.md).

## Requisitos previos

- SDK de .NET indicado en `global.json` (10.0.112 o un parche de característica posterior).
- Docker no es necesario en esta feature.

## 1. Compilación estricta (historia 2)

```bash
dotnet build MiRestaurante.slnx
```

**Esperado**: 0 advertencias y 0 errores.

Prueba negativa: añadir temporalmente una variable sin usar en cualquiera de los 9 proyectos y volver a compilar. **Esperado**: la compilación falla. Revertir el cambio.

## 2. Paquetes conformes (historia 1)

```bash
grep -rEl "Microsoft.NET.Test.Sdk|xunit.runner.visualstudio|NetArchTest.Rules" --include=*.csproj .
grep -rl "Moq" --include=*.csproj tests
grep -rL "AwesomeAssertions" --include=*.csproj tests
```

**Esperado**: la primera no devuelve nada; la segunda devuelve solo `MiRestaurante.Application.UnitTests`; la tercera no devuelve nada.

## 3. Suite completa con proyectos vacíos (historias 1 y 4)

```bash
dotnet test --solution MiRestaurante.slnx
```

**Esperado**: código de salida 0. Las pruebas de arquitectura pasan (6 de capas, 1 de controladores, 1 de Domain y 1 de lista blanca de Application) y los otros cuatro proyectos no hacen fallar la ejecución.

## 4. Regla de arquitectura de Application (historia 4, fase Red)

Con Application referenciando temporalmente un tipo de `Microsoft.EntityFrameworkCore.Relational`, otro de Npgsql y otro de `Microsoft.Extensions.DependencyInjection.Abstractions`:

```bash
dotnet test --project tests/MiRestaurante.Architecture.Tests --filter-method "*Application_Should_ReferenceOnlyAllowedAssemblies*"
```

**Esperado**: la prueba falla y el mensaje nombra los ensamblados no permitidos. Revertir los cambios temporales y repetir: **Esperado**: pasa. Una referencia a `Microsoft.Extensions.Logging.Abstractions` (`ILogger<T>`) no debe hacerla fallar.

## 5. Plantillas eliminadas (historia 3)

```bash
grep -rEl "Class1|UnitTest1|WeatherForecast" --include=*.cs --include=*.csproj --include=*.http --include=*.json src tests
dotnet run --project src/MiRestaurante.Api --launch-profile http
```

**Esperado**: la búsqueda no devuelve nada (se excluyen `bin/` y `obj/` al revisar a mano). La API arranca sin errores y, en otra terminal, `curl http://localhost:5110/openapi/v1.json` devuelve el documento OpenAPI.

## 6. Herramientas locales y puerta de cobertura (historia 5)

```bash
dotnet tool restore
dotnet ef --version
dotnet reportgenerator --help

rm -rf TestResults
dotnet test --solution MiRestaurante.slnx --results-directory TestResults --coverlet --coverlet-output-format cobertura
dotnet reportgenerator -reports:"TestResults/coverage.cobertura.*.xml" -targetdir:TestResults/report -reporttypes:"TextSummary;Html" "-assemblyfilters:+MiRestaurante.Domain;+MiRestaurante.Application;+MiRestaurante.Infrastructure;+MiRestaurante.Api" minimumCoverageThresholds:lineCoverage=80
cat TestResults/report/Summary.txt
git status --short
```

**Esperado**:

- Se restauran `dotnet-ef` 10.0.14 y ReportGenerator 5.5.12 sin instalar nada global.
- Se generan cinco informes Cobertura en `TestResults/` y uno fusionado en `TestResults/report/`.
- ReportGenerator informa que la cobertura de líneas está por debajo del 80 % y termina con código 1. En esta feature eso es informativo y no bloquea (excepción aceptada).
- `git status` no muestra `TestResults/` como pendiente.

Repetir el bloque completo: el resumen solo debe reflejar la última ejecución.
