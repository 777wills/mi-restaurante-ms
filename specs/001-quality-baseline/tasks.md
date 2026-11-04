# Tasks: Base de calidad y limpieza del esqueleto

**Entrada**: documentos de diseño en `/specs/001-quality-baseline/`

**Prerequisitos**: `plan.md`, `spec.md`, `research.md`, `quickstart.md`

**Pruebas**: incluidas cuando la especificación o la constitución las requieren. Los cambios de código siguen TDD; la fase Red de la regla de Application se demuestra con dependencias temporales que se revierten.

## Fase 1: Preparación

**Propósito**: preparar el trabajo inicial.

No se requieren tareas de preparación: la solución, los nueve proyectos y la estructura de pruebas ya existen.

## Fase 2: Fundamentos

**Propósito**: establecer prerrequisitos compartidos que bloqueen todas las historias.

No hay prerrequisitos transversales que deban completarse antes de iniciar una historia. La configuración común de compilación pertenece a la Historia 2.

## Fase 3: Historia de usuario 1 — Ejecución de pruebas conforme a la constitución (P1)

**Objetivo**: migrar los cinco proyectos de prueba a xUnit v3 sobre Microsoft.Testing.Platform y a los paquetes de prueba permitidos.

**Prueba independiente**: revisar los cinco `.csproj` para confirmar que no referencian `Microsoft.NET.Test.Sdk` ni `xunit.runner.visualstudio`, que usan las versiones requeridas, y ejecutar `dotnet test --solution MiRestaurante.slnx`.

### Implementación

- [x] T001 [P] [US1] Actualizar `tests/MiRestaurante.Domain.UnitTests/MiRestaurante.Domain.UnitTests.csproj`: quitar `Microsoft.NET.Test.Sdk` y `xunit.runner.visualstudio`, y asegurar `xunit.v3` 4.0.2, `coverlet.MTP` 10.1.0 y `AwesomeAssertions` 9.6.0.
- [x] T002 [P] [US1] Actualizar `tests/MiRestaurante.Application.UnitTests/MiRestaurante.Application.UnitTests.csproj`: quitar `Microsoft.NET.Test.Sdk` y `xunit.runner.visualstudio`, y asegurar `xunit.v3` 4.0.2, `coverlet.MTP` 10.1.0, `AwesomeAssertions` 9.6.0 y `Moq` 4.21.0.
- [x] T003 [P] [US1] Actualizar `tests/MiRestaurante.Infrastructure.IntegrationTests/MiRestaurante.Infrastructure.IntegrationTests.csproj`: quitar `Microsoft.NET.Test.Sdk` y `xunit.runner.visualstudio`, y asegurar `xunit.v3` 4.0.2, `coverlet.MTP` 10.1.0 y `AwesomeAssertions` 9.6.0.
- [x] T004 [P] [US1] Actualizar `tests/MiRestaurante.Api.FunctionalTests/MiRestaurante.Api.FunctionalTests.csproj`: quitar `Microsoft.NET.Test.Sdk` y `xunit.runner.visualstudio`, y asegurar `xunit.v3` 4.0.2, `coverlet.MTP` 10.1.0, `AwesomeAssertions` 9.6.0 y `Microsoft.AspNetCore.Mvc.Testing` 10.0.14.
- [x] T005 [P] [US1] Actualizar `tests/MiRestaurante.Architecture.Tests/MiRestaurante.Architecture.Tests.csproj`: quitar `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio` y `NetArchTest.Rules`; usar `xunit.v3` 4.0.2, `coverlet.MTP` 10.1.0, `AwesomeAssertions` 9.6.0 y `NetArchTest.eNhancedEdition` 1.4.5.
- [x] T006 [US1] Migrar `tests/MiRestaurante.Architecture.Tests/LayerDependencyTests.cs` a la API de NetArchTest.eNhancedEdition, sustituyendo `HaveDependencyOn` por `HaveDependencyOnAny` y adaptando `FailingTypeNames` a `FailingTypes` sin cambiar la intención ni los nombres de las pruebas.
- [x] T007 [US1] Ejecutar `dotnet test --solution MiRestaurante.slnx` y comprobar las referencias de los cinco proyectos de `tests/` para confirmar que no quedan paquetes prohibidos y que la suite existente pasa.

**Checkpoint**: los cinco proyectos usan MTP y las pruebas de arquitectura existentes conservan su comportamiento.

## Fase 4: Historia de usuario 2 — Compilación estricta y configuración común (P1)

**Objetivo**: centralizar solo las propiedades comunes requeridas y tratar las advertencias como errores en los nueve proyectos.

**Prueba independiente**: `dotnet build MiRestaurante.slnx` termina con cero advertencias y errores; una advertencia introducida temporalmente en un proyecto de producción o de pruebas hace fallar la compilación.

### Implementación

- [x] T008 [US2] Crear `Directory.Build.props` en la raíz con `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true` y `TestingPlatformCommandLineArguments=--ignore-exit-code 8`.
- [x] T009 [US2] Quitar las propiedades repetidas `TargetFramework`, `Nullable` e `ImplicitUsings` de los nueve proyectos: `src/MiRestaurante.Api/MiRestaurante.Api.csproj`, `src/MiRestaurante.Application/MiRestaurante.Application.csproj`, `src/MiRestaurante.Domain/MiRestaurante.Domain.csproj`, `src/MiRestaurante.Infrastructure/MiRestaurante.Infrastructure.csproj`, `tests/MiRestaurante.Api.FunctionalTests/MiRestaurante.Api.FunctionalTests.csproj`, `tests/MiRestaurante.Application.UnitTests/MiRestaurante.Application.UnitTests.csproj`, `tests/MiRestaurante.Architecture.Tests/MiRestaurante.Architecture.Tests.csproj`, `tests/MiRestaurante.Domain.UnitTests/MiRestaurante.Domain.UnitTests.csproj` y `tests/MiRestaurante.Infrastructure.IntegrationTests/MiRestaurante.Infrastructure.IntegrationTests.csproj`, conservando en cada proyecto sus ajustes específicos. En `src/MiRestaurante.Api/MiRestaurante.Api.csproj`, subir además `Microsoft.AspNetCore.OpenApi` de 10.0.12 a 10.0.14 (FR-009).
- [x] T010 [US2] Validar `Directory.Build.props` compilando con `dotnet build MiRestaurante.slnx` y comprobando la elevación de una advertencia a error mediante un cambio temporal en un archivo `.cs` de producción o pruebas; revertir el cambio temporal.

**Checkpoint**: los nueve proyectos heredan la configuración común y compilan sin advertencias.

## Fase 5: Historia de usuario 3 — Esqueleto sin código de plantilla (P2)

**Objetivo**: eliminar los nueve archivos de plantilla y las referencias a `WeatherForecast`, dejando el archivo HTTP sin solicitudes de ejemplo.

**Prueba independiente**: buscar `Class1`, `UnitTest1` y `WeatherForecast` en el código fuente y los proyectos; compilar, ejecutar la suite y verificar que la API en Development publica OpenAPI.

### Implementación

- [x] T011 [P] [US3] Eliminar `src/MiRestaurante.Domain/Class1.cs`, `src/MiRestaurante.Application/Class1.cs`, `src/MiRestaurante.Infrastructure/Class1.cs`, `tests/MiRestaurante.Api.FunctionalTests/UnitTest1.cs`, `tests/MiRestaurante.Application.UnitTests/UnitTest1.cs`, `tests/MiRestaurante.Domain.UnitTests/UnitTest1.cs`, `tests/MiRestaurante.Infrastructure.IntegrationTests/UnitTest1.cs`, `src/MiRestaurante.Api/WeatherForecast.cs` y `src/MiRestaurante.Api/Controllers/WeatherForecastController.cs`; quitar cualquier referencia restante a esos tipos o nombres.
- [x] T012 [P] [US3] Editar `src/MiRestaurante.Api/MiRestaurante.Api.http` para conservar únicamente la variable de dirección del host y ninguna solicitud.
- [x] T013 [US3] Validar la limpieza compilando y ejecutando `dotnet test --solution MiRestaurante.slnx`, que DEBE terminar con código 0 aunque cuatro proyectos de pruebas queden sin pruebas (FR-005, SC-003), y arrancar `src/MiRestaurante.Api` en Development para confirmar que sigue sirviendo `/openapi/v1.json`.

**Checkpoint**: no queda código de plantilla y la API mantiene el documento OpenAPI.

## Fase 6: Historia de usuario 4 — Regla de arquitectura para dependencias de Application (P2)

**Objetivo**: verificar mediante lista blanca que Application solo referencia framework, Domain, EF Core y `Microsoft.Extensions.Logging.Abstractions`.

**Prueba independiente**: la prueba de arquitectura pasa con las dependencias permitidas y falla nombrando ensamblados no permitidos cuando se usan temporalmente referencias a EF Core Relational y Npgsql.

### Pruebas e implementación (TDD)

- [x] T014 [US4] Extraer `IsFrameworkAssembly` de `tests/MiRestaurante.Architecture.Tests/LayerDependencyTests.cs` a `tests/MiRestaurante.Architecture.Tests/Layers.cs` y reutilizarlo desde `LayerDependencyTests`; ejecutar las pruebas de arquitectura para confirmar que siguen pasando (refactor sin cambio de comportamiento).
- [x] T015 [US4] Escribir `Application_Should_ReferenceOnlyAllowedAssemblies_When_Compiled` en `tests/MiRestaurante.Architecture.Tests/ApplicationDependencyTests.cs` con la lista blanca `MiRestaurante.Domain`, `Microsoft.EntityFrameworkCore` y el nombre exacto `Microsoft.Extensions.Logging.Abstractions`. Demostrar Red agregando temporalmente en `src/MiRestaurante.Application/MiRestaurante.Application.csproj` referencias a `Microsoft.EntityFrameworkCore.Relational`, Npgsql y `Microsoft.Extensions.DependencyInjection.Abstractions`, y un probe temporal `src/MiRestaurante.Application/TemporaryDependencyProbe.cs` que use un tipo de cada uno; verificar que la prueba falla y que el mensaje nombra los tres ensamblados. Comprobar también que un uso temporal de `ILogger<T>` no la hace fallar. Revertir todos los cambios temporales.
- [x] T016 [US4] Con Application sin cambios temporales, ejecutar `dotnet test --project tests/MiRestaurante.Architecture.Tests` y confirmar Green de la nueva regla y de las reglas existentes de capas, controladores y Domain.

**Checkpoint**: la regla de Application detecta dependencias fuera de lista blanca y todas las reglas de arquitectura siguen activas.

## Fase 7: Historia de usuario 5 — Puerta de cobertura y herramientas locales reproducibles (P3)

**Objetivo**: permitir restaurar herramientas locales y generar informes Cobertura por proyecto y un informe fusionado limitado a ensamblados de producción.

**Prueba independiente**: seguir únicamente los comandos de `CLAUDE.md` en dos ejecuciones consecutivas y comprobar el informe, el umbral, la ausencia de resultados residuales y el estado limpio de git.

### Implementación

- [x] T017 [P] [US5] Crear `.config/dotnet-tools.json` con `dotnet-ef` 10.0.14 y `dotnet-reportgenerator-globaltool` 5.5.12 como herramientas locales restaurables.
- [x] T018 [P] [US5] Documentar en la sección «Comandos» de `CLAUDE.md` `dotnet tool restore`, la limpieza de `TestResults/`, la ejecución con cobertura Cobertura y la fusión con ReportGenerator, filtro explícito de los cuatro ensamblados de producción y `minimumCoverageThresholds:lineCoverage=80`; indicar que el resultado bajo el umbral es informativo solo para esta feature.
- [x] T019 [US5] Comprobar que `.gitignore` ignora `TestResults/` y que los cinco proyectos de `tests/` producen informes Cobertura mediante `dotnet test --solution MiRestaurante.slnx --results-directory TestResults --coverlet --coverlet-output-format cobertura`.
- [x] T020 [US5] Restaurar las herramientas desde `.config/dotnet-tools.json` y ejecutar dos veces la puerta documentada en `CLAUDE.md`; comprobar que ReportGenerator fusiona solo `TestResults/coverage.cobertura.*.xml`, aplica el umbral a `MiRestaurante.Domain`, `MiRestaurante.Application`, `MiRestaurante.Infrastructure` y `MiRestaurante.Api`, genera el informe bajo `TestResults/report/`, y que git no muestra los artefactos generados. No tratar un umbral incumplido como fallo de integración en esta feature.

**Checkpoint**: las herramientas y los informes se reproducen con los comandos documentados, sin instalaciones globales ni artefactos pendientes en git.

## Fase final: Pulido y validación transversal

**Propósito**: ejecutar las puertas de calidad y comprobar que la documentación coincide con la implementación.

- [x] T021 Ejecutar `dotnet format --verify-no-changes MiRestaurante.slnx`, `dotnet build MiRestaurante.slnx` y `dotnet test --solution MiRestaurante.slnx`; comprobar también los criterios de aceptación y la ausencia de referencias a las plantillas en los archivos de la solución.

## Dependencias y orden de ejecución

### Dependencias entre fases

- **Preparación y fundamentos**: no tienen tareas ni bloquean historias; la solución ya está inicializada.
- **US1 (P1) y US2 (P1)**: pueden iniciarse sin dependencia funcional entre sí. Modifican los mismos `.csproj`, así que sus ediciones deben hacerse en orden o coordinarse para evitar conflictos.
- **US3 (P2)**: depende de T008 (US2). Al borrar los `UnitTest1.cs`, cuatro proyectos quedan sin pruebas y la suite termina con código 8 si la tolerancia de `Directory.Build.props` no existe todavía.
- **US4 (P2)**: depende de US1 para la nueva versión de AwesomeAssertions y NetArchTest, y de US2 para compilar con la configuración común.
- **US5 (P3)**: depende de US1 para `coverlet.MTP` en los cinco proyectos y de US2 para tolerar los proyectos que aún no contienen pruebas.
- **Pulido**: depende de que se completen las historias seleccionadas.

### Oportunidades de paralelismo

- En US1, T001–T005 son independientes porque actualizan archivos `.csproj` distintos.
- En US3, T011 y T012 son independientes porque eliminan archivos de plantilla y editan el archivo HTTP, respectivamente.
- En US5, T017 y T018 son independientes porque editan el manifiesto local y la guía de comandos.
- T011 y T012 pueden prepararse en paralelo con US1, pero T013 requiere T008 completada. US4 y US5 deben esperar sus dependencias indicadas.

## Ejemplos de ejecución en paralelo

### Historia de usuario 1

```text
T001: actualizar tests/MiRestaurante.Domain.UnitTests/MiRestaurante.Domain.UnitTests.csproj
T002: actualizar tests/MiRestaurante.Application.UnitTests/MiRestaurante.Application.UnitTests.csproj
T003: actualizar tests/MiRestaurante.Infrastructure.IntegrationTests/MiRestaurante.Infrastructure.IntegrationTests.csproj
T004: actualizar tests/MiRestaurante.Api.FunctionalTests/MiRestaurante.Api.FunctionalTests.csproj
T005: actualizar tests/MiRestaurante.Architecture.Tests/MiRestaurante.Architecture.Tests.csproj
```

### Historia de usuario 3

```text
T011: eliminar archivos de plantilla de src/ y tests/
T012: limpiar src/MiRestaurante.Api/MiRestaurante.Api.http
```

### Historia de usuario 5

```text
T017: crear .config/dotnet-tools.json
T018: documentar la puerta de cobertura en CLAUDE.md
```

## Estrategia de implementación

### MVP

Completar primero US1 y US2 (ambas P1), que dejan la suite de pruebas utilizable y la compilación estricta. Validar de forma independiente ambas historias antes de continuar. US3 es el siguiente incremento pequeño y verificable; US4 y US5 completan las reglas de arquitectura y la puerta reproducible.

### Entrega incremental

1. Migrar los proyectos de pruebas y validar las reglas existentes (US1).
2. Activar la configuración común y comprobar compilación estricta (US2).
3. Eliminar las plantillas y validar la API (US3).
4. Añadir y validar la lista blanca de Application con TDD (US4).
5. Añadir las herramientas y validar la cobertura fusionada (US5).
6. Ejecutar el pulido transversal y las puertas de calidad.
