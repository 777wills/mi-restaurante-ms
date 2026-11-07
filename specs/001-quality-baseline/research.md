# Investigación: Base de calidad y limpieza del esqueleto

Todas las decisiones se verificaron en una copia desechable del repositorio (fuera del árbol de trabajo) con SDK 10.0.114: compilación, suite con cobertura y fusión.

## D1. Tolerar proyectos sin pruebas (FR-005)

- **Decisión**: `<TestingPlatformCommandLineArguments>--ignore-exit-code 8</TestingPlatformCommandLineArguments>` en `Directory.Build.props`.
- **Justificación**: MTP termina con código 8 cuando un módulo no descubre pruebas. `--ignore-exit-code` es una opción de plataforma (la acepta cualquier proyecto de pruebas) y la propiedad `TestingPlatformCommandLineArguments` es el mecanismo documentado para pasar argumentos desde MSBuild. Verificado: con 4 proyectos vacíos la suite termina con código 0, y coexiste con `--coverlet`.
- **Alternativas**: variable de entorno `TESTINGPLATFORM_EXITCODE_IGNORE` (no queda versionada ni garantiza «un solo lugar»); `--ignore-exit-code` en cada comando (obliga a recordarlo); mantener pruebas vacías (contradice el borrado de las plantillas).
- **Efecto secundario**: el documento de MTP indica que, en una ejecución con varios módulos, el SDK 11 ya no falla por un módulo vacío. Cuando se suba de SDK la propiedad podría sobrar. Se retira de todos modos al tener pruebas en los cinco proyectos.

## D2. Alcance de `Directory.Build.props` (FR-006, FR-007)

- **Decisión**: solo `TargetFramework`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors` y la tolerancia de D1.
- **Justificación**: es lo acordado en la clarificación. Los ajustes de pruebas (`OutputType`, `IsPackable`, `using Xunit`, paquetes) se quedan en cada `.csproj` de pruebas, sin un segundo archivo común. Con esos valores heredados, la copia de verificación compila con 0 advertencias y 0 errores (FR-008).
- **Alternativas**: segundo `Directory.Build.props` en `tests/` y gestión central de paquetes (`Directory.Packages.props`): descartadas por simplicidad.

## D3. Migración a NetArchTest.eNhancedEdition 1.4.5 (FR-002)

- **Decisión**: cambiar la referencia y ajustar dos llamadas.
- **Hallazgo**: el paquete conserva el espacio de nombres `NetArchTest.Rules`, pero no es compatible a nivel de API. Con el código actual el compilador falla: `HaveDependencyOn` ahora es `HaveDependencyOnAny` (también existen `NotHaveDependencyOnAny`, `OnlyHaveDependencyOn`, `HaveDependencyOtherThan`) y `TestResult.FailingTypeNames` no existe (se usa `FailingTypes`, una lista de `IType` con `Explanation`).
- **Verificación**: tras el cambio las 8 pruebas existentes (6 de la teoría de capas, 1 de controladores y 1 de Domain) pasan.
- **Alternativa**: `TngTech.ArchUnitNET.xUnitV3`, reservada por la constitución para el caso de que eNhancedEdition deje de funcionar.

## D4. Informes de cobertura por proyecto (FR-012, FR-014, FR-015)

- **Decisión**: `dotnet test --solution MiRestaurante.slnx --results-directory TestResults --coverlet --coverlet-output-format cobertura`.
- **Hallazgo**: con `--results-directory` los cinco informes quedan en `TestResults/` con nombre `coverage.cobertura.<marca de tiempo>.xml`, sin colisiones entre proyectos, así que no hace falta `--coverlet-file-prefix`.
- **Limpieza**: los informes llevan marca de tiempo, por lo que no se sobrescriben entre ejecuciones; hay que borrar `TestResults/` antes de cada ejecución (FR-014). Se documenta como paso explícito.
- **Ignorado por git**: `.gitignore` ya contiene `[Tt]est[Rr]esult*/`. No hay que modificarlo.

## D5. Fusión, filtro de ensamblados y umbral (FR-013, FR-017)

- **Decisión**: `dotnet reportgenerator -reports:"TestResults/coverage.cobertura.*.xml" -targetdir:TestResults/report -reporttypes:"TextSummary;Html" "-assemblyfilters:+MiRestaurante.Domain;+MiRestaurante.Application;+MiRestaurante.Infrastructure;+MiRestaurante.Api" minimumCoverageThresholds:lineCoverage=80`.
- **Hallazgo importante**: el umbral no es un parámetro con guion. `-minimumCoverageThresholds:lineCoverage=80` (la forma que figuraba en la constitución 1.0.0) da «Unknown command line parameter» y se ignora en silencio. La forma `minimumCoverageThresholds:lineCoverage=80` funciona: con una cobertura del 0 % imprime «The line coverage of 0% is below the minimum threshold of 80%» y termina con código 1.
- **Filtro**: `-assemblyfilters` con inclusiones explícitas de los cuatro ensamblados de producción se aplica en un único sitio (ReportGenerator), sin excluir clases ni archivos. Con el esqueleto limpio el informe fusionado solo contiene `MiRestaurante.Api` (12 líneas cubribles), porque los demás ensamblados no tienen código.
- **Directorio del informe**: dentro de `TestResults/report` para que el patrón de entrada (`TestResults/coverage.cobertura.*.xml`) no recoja el informe fusionado y la limpieza sea un único borrado.
- **Constitución**: corregida en la enmienda PATCH 1.0.1, que ya usa la forma sin guion.

## D6. Prueba de lista blanca de Application (FR-023 a FR-025)

- **Decisión**: igual que la prueba existente de Domain, inspeccionar `Assembly.GetReferencedAssemblies()` del ensamblado compilado, sin NetArchTest. Descartar framework (`System*`, `netstandard`, `mscorlib`) y la lista exacta `MiRestaurante.Domain`, `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.Logging.Abstractions`.
- **Justificación**: el análisis por espacios de nombres no distingue el núcleo de EF Core de su parte relacional (ambos usan `Microsoft.EntityFrameworkCore`). Los ensamblados referenciados sí.
- **Fase Red (VII)**: con Application vacío la prueba pasa por falta de referencias. Para ver el fallo se añade un archivo temporal en Application que use un tipo de `Microsoft.EntityFrameworkCore.Relational` y otro de Npgsql (para que el compilador emita la referencia hace falta que el paquete esté referenciado temporalmente), se comprueba el fallo y el mensaje, y se revierte todo antes de integrar.
- **Riesgo conocido**: cuando Application use `Microsoft.EntityFrameworkCore`, el compilador puede emitir referencia a ensamblados auxiliares (por ejemplo `Microsoft.EntityFrameworkCore.Abstractions` o `Microsoft.Extensions.*`). Según el supuesto de la spec, ampliar la lista exige primero enmendar el principio II; no se amplía por adelantado (YAGNI).

## D7. Limpieza de plantillas (FR-018 a FR-022)

- **Decisión**: borrar los 9 archivos y dejar `MiRestaurante.Api.http` con solo la variable del host.
- **Verificación**: tras borrar, la solución compila y la prueba de controladores sigue ejecutándose sobre un espacio de nombres vacío y pasa (no se desactiva). `Program.cs` no cambia: sigue registrando `AddControllers()` y `AddOpenApi()` y sirviendo `MapOpenApi()` en Development.

## D8. Versiones de paquetes (FR-009, FR-010)

- `Microsoft.AspNetCore.OpenApi` pasa de 10.0.12 a 10.0.14 y `Microsoft.AspNetCore.Mvc.Testing` de 10.0.13 a 10.0.14, como fija la tabla de stack. `dotnet-ef` y ReportGenerator se restauran con `dotnet tool restore` desde `.config/dotnet-tools.json` (comandos `dotnet-ef` y `reportgenerator`), verificado.
