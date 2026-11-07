# Plan de implementación: Base de calidad y limpieza del esqueleto

**Rama**: `speckit-test` (feature `001-quality-baseline`) | **Fecha**: 2026-11-04 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la feature en `specs/001-quality-baseline/spec.md`

## Resumen

Dejar el esqueleto de la solución conforme a la constitución antes de escribir código de negocio:

1. Los cinco proyectos de pruebas usan solo xUnit v3 sobre Microsoft.Testing.Platform (MTP), AwesomeAssertions y, en Application, Moq.
2. Un `Directory.Build.props` en la raíz fija `net10.0`, nulabilidad, usings implícitos y `TreatWarningsAsErrors`, y tolera los proyectos sin pruebas.
3. Un manifiesto de herramientas locales trae `dotnet-ef` y ReportGenerator.
4. La puerta de cobertura (coverlet.MTP por proyecto, fusión con ReportGenerator, umbral 80 %) se documenta como comandos en `CLAUDE.md`, sin scripts.
5. Se borran los archivos de plantilla.
6. Una prueba de arquitectura nueva limita lo que puede referenciar el ensamblado de Application (lista blanca).

El enfoque se validó con una copia desechable del repositorio (ver [research.md](research.md)): compila con 0 advertencias, la suite termina con éxito aunque 4 proyectos no tengan pruebas y la fusión de cobertura funciona.

## Technical Context

**Language/Version**: C# sobre .NET 10 (SDK 10.0.112 en `global.json`, `rollForward: latestFeature`)

**Primary Dependencies**: `xunit.v3` 4.0.2, `AwesomeAssertions` 9.6.0, `Moq` 4.21.0 (solo Application.UnitTests), `NetArchTest.eNhancedEdition` 1.4.5, `coverlet.MTP` 10.1.0, `Microsoft.AspNetCore.OpenApi` 10.0.14, `Microsoft.AspNetCore.Mvc.Testing` 10.0.14; herramientas locales `dotnet-ef` 10.0.14 y `dotnet-reportgenerator-globaltool` 5.5.12

**Storage**: N/A (esta feature no toca persistencia)

**Testing**: xUnit v3 sobre MTP (`dotnet test --solution`), coverlet.MTP para cobertura, ReportGenerator para fusionarla

**Target Platform**: equipos de los desarrolladores (herramienta de desarrollo, no se despliega)

**Project Type**: web-service (API REST en cuatro capas); esta feature solo cambia configuración de compilación, pruebas y limpieza

**Performance Goals**: N/A

**Constraints**: cero advertencias; ninguna regla desactivada ni advertencia suprimida; sin scripts, Makefiles ni objetivos MSBuild nuevos para la puerta de cobertura; sin gestión centralizada de paquetes

**Scale/Scope**: 9 proyectos, 9 archivos de plantilla a borrar, 5 proyectos de pruebas a migrar, 1 prueba de arquitectura nueva

No quedan marcadores `NEEDS CLARIFICATION`.

## Constitution Check

_Puerta: debe pasar antes de la Fase 0. Se reevalúa tras el diseño de la Fase 1._

| Principio                        | Evaluación                                                                                                                                                                                                                                                                                                                                                      | Resultado                                                  |
| -------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------- |
| I. Simplicidad                   | Cero proyectos, capas, abstracciones ni scripts nuevos. Los únicos archivos nuevos son `Directory.Build.props`, `.config/dotnet-tools.json` y una clase de prueba. No se introduce gestión centralizada de paquetes. `NetArchTest.eNhancedEdition` está «en riesgo» según la constitución, pero ya está aceptado en ella como sustituto de `NetArchTest.Rules`. | Pasa                                                       |
| II. Arquitectura en cuatro capas | La nueva prueba de lista blanca automatiza la regla de dependencias de Application que hoy no tiene prueba; sus ensamblados permitidos (Domain, núcleo de EF Core y `Microsoft.Extensions.Logging.Abstractions`) coinciden con el principio II de la constitución v1.0.1. Las pruebas existentes se conservan.                                                  | Pasa                                                       |
| III. Modelo de dominio simple    | No aplica: no hay entidades ni fechas.                                                                                                                                                                                                                                                                                                                          | N/A                                                        |
| IV. Capa de aplicación           | No aplica: no hay servicios.                                                                                                                                                                                                                                                                                                                                    | N/A                                                        |
| V. Acceso a datos                | No aplica. `dotnet-ef` solo se añade al manifiesto de herramientas.                                                                                                                                                                                                                                                                                             | N/A                                                        |
| VI. API HTTP                     | Se borra el controlador de ejemplo (viola el prefijo `/api/v1/`). Scalar, ProblemDetails, CORS y `/health` quedan fuera de alcance por decisión de clarificación.                                                                                                                                                                                               | Pasa                                                       |
| VII. TDD (no negociable)         | La prueba de arquitectura sigue Red → Green: se introduce temporalmente en Application un tipo de `Microsoft.EntityFrameworkCore.Relational` (y otro de Npgsql), se ve fallar la prueba y se revierte. La cobertura y sus herramientas se montan conforme al principio.                                                                                         | Pasa, con **excepción aceptada** (ver Complexity Tracking) |
| VIII. Convenciones de código     | `TreatWarningsAsErrors` se activa en toda la solución. La nueva prueba sigue `{Metodo}_Should_{Resultado}_When_{Escenario}`, una sola aserción y sin lógica condicional.                                                                                                                                                                                        | Pasa                                                       |
| IX. Entorno local                | No aplica (`compose.yaml` fuera de alcance).                                                                                                                                                                                                                                                                                                                    | N/A                                                        |

Puertas de calidad de la sección «Flujo de desarrollo»: la 3 (pruebas) y la 4 (cobertura) se montan en esta feature. La puerta 3 de la constitución usa `--coverlet --coverlet-output-format cobertura` sin `--results-directory`; el plan añade `--results-directory TestResults` para tener una ubicación única (FR-014).

**Sintaxis del umbral**: ReportGenerator 5.5.12 solo acepta `minimumCoverageThresholds:lineCoverage=80` sin guion inicial (con guion responde «Unknown command line parameter» y no aplica el umbral). La constitución v1.0.1 ya recoge esta forma, y el plan y `CLAUDE.md` la usan.

**Reevaluación tras la Fase 1**: sin cambios. El diseño no añade piezas respecto a lo evaluado.

## Estructura del proyecto

### Documentación (esta feature)

```text
specs/001-quality-baseline/
├── plan.md          # Este archivo
├── research.md      # Fase 0
├── quickstart.md    # Fase 1: guía de validación
├── checklists/
│   └── requirements.md
└── tasks.md         # Fase 2 (lo genera /speckit-tasks, no este comando)
```

No se generan `data-model.md` (la feature no tiene entidades ni estados) ni `contracts/` (no expone interfaces nuevas: se elimina el único endpoint existente y los comandos de desarrollo se documentan en `quickstart.md` y en `CLAUDE.md`).

### Código fuente (raíz del repositorio)

```text
.config/
└── dotnet-tools.json                 # NUEVO: dotnet-ef 10.0.14, reportgenerator 5.5.12
Directory.Build.props                 # NUEVO: TFM, Nullable, ImplicitUsings, TreatWarningsAsErrors,
                                      #        TestingPlatformCommandLineArguments=--ignore-exit-code 8
CLAUDE.md                             # MODIFICADO: comandos de herramientas y puerta de cobertura

src/
├── MiRestaurante.Api/
│   ├── MiRestaurante.Api.csproj      # MODIFICADO: sin TFM/Nullable/ImplicitUsings; OpenApi 10.0.14
│   ├── MiRestaurante.Api.http        # MODIFICADO: solo la variable del host
│   ├── WeatherForecast.cs            # BORRADO
│   └── Controllers/
│       └── WeatherForecastController.cs   # BORRADO (la carpeta queda vacía y se elimina)
├── MiRestaurante.Application/Class1.cs    # BORRADO (+ .csproj sin TFM/Nullable/ImplicitUsings)
├── MiRestaurante.Domain/Class1.cs         # BORRADO (+ .csproj ídem)
└── MiRestaurante.Infrastructure/Class1.cs # BORRADO (+ .csproj ídem)

tests/
├── MiRestaurante.Api.FunctionalTests/
│   ├── *.csproj                      # MODIFICADO: sin Test.Sdk/runner; Mvc.Testing 10.0.14; + AwesomeAssertions
│   └── UnitTest1.cs                  # BORRADO
├── MiRestaurante.Application.UnitTests/
│   ├── *.csproj                      # MODIFICADO: sin Test.Sdk/runner; + AwesomeAssertions, + Moq 4.21.0
│   └── UnitTest1.cs                  # BORRADO
├── MiRestaurante.Domain.UnitTests/
│   ├── *.csproj                      # MODIFICADO: sin Test.Sdk/runner; + AwesomeAssertions
│   └── UnitTest1.cs                  # BORRADO
├── MiRestaurante.Infrastructure.IntegrationTests/
│   ├── *.csproj                      # MODIFICADO: sin Test.Sdk/runner; + AwesomeAssertions
│   └── UnitTest1.cs                  # BORRADO
└── MiRestaurante.Architecture.Tests/
    ├── *.csproj                      # MODIFICADO: sin Test.Sdk/runner; NetArchTest.eNhancedEdition 1.4.5
    ├── Layers.cs                     # MODIFICADO: recibe IsFrameworkAssembly (compartido por dos clases)
    ├── LayerDependencyTests.cs       # MODIFICADO: migración de API (HaveDependencyOnAny, FailingTypes)
    └── ApplicationDependencyTests.cs # NUEVO: prueba de lista blanca de Application
```

**Decisión de estructura**: se conserva la estructura de cuatro capas y cinco proyectos de pruebas existente. No se crean proyectos. La prueba nueva va en su propia clase (`ApplicationDependencyTests`) porque `LayerDependencyTests` ya agrupa reglas entre capas, y el método `IsFrameworkAssembly` pasa a `Layers` para no duplicarlo.

## Diseño técnico

### Configuración común (`Directory.Build.props`)

Cinco propiedades: `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true` y `TestingPlatformCommandLineArguments=--ignore-exit-code 8` (FR-005: el código de salida 8 de MTP indica «cero pruebas»). Los ajustes propios de las pruebas (`OutputType`, `IsPackable`, `Using Include="Xunit"`, paquetes) se quedan en cada `.csproj` de pruebas. Detalle y verificación en [research.md](research.md), decisiones D1 y D2.

### Paquetes por proyecto de pruebas

| Proyecto                        | xunit.v3 | coverlet.MTP | AwesomeAssertions | Moq    | Otros                                                               |
| ------------------------------- | -------- | ------------ | ----------------- | ------ | ------------------------------------------------------------------- |
| Domain.UnitTests                | 4.0.2    | 10.1.0       | 9.6.0             | no     |                                                                     |
| Application.UnitTests           | 4.0.2    | 10.1.0       | 9.6.0             | 4.21.0 |                                                                     |
| Infrastructure.IntegrationTests | 4.0.2    | 10.1.0       | 9.6.0             | no     |                                                                     |
| Api.FunctionalTests             | 4.0.2    | 10.1.0       | 9.6.0             | no     | `Microsoft.AspNetCore.Mvc.Testing` 10.0.14                          |
| Architecture.Tests              | 4.0.2    | 10.1.0       | 9.6.0 (ya está)   | no     | `NetArchTest.eNhancedEdition` 1.4.5 en lugar de `NetArchTest.Rules` |

### Migración de la librería de arquitectura

`NetArchTest.eNhancedEdition` conserva el espacio de nombres `NetArchTest.Rules`, pero cambia dos miembros que usan las pruebas actuales (el compilador los rechaza): `HaveDependencyOn` pasa a `HaveDependencyOnAny` y `TestResult.FailingTypeNames` desaparece (se usa `FailingTypes`). Se cambian solo esas llamadas; los nombres y la intención de las pruebas no cambian (FR-002, FR-026). La suite migrada pasa en la copia de verificación.

### Prueba de lista blanca de Application (FR-023 a FR-025)

- Método: `Application_Should_ReferenceOnlyAllowedAssemblies_When_Compiled`, en `ApplicationDependencyTests`.
- Obtiene `Layers.Load(Layers.Application).GetReferencedAssemblies()`, descarta los ensamblados del framework (`System*`, `netstandard`, `mscorlib`) y los permitidos por nombre exacto: `MiRestaurante.Domain`, `Microsoft.EntityFrameworkCore` y `Microsoft.Extensions.Logging.Abstractions`.
- Una sola aserción: `BeEmpty` sobre los nombres restantes, con mensaje que enumera los no permitidos (AwesomeAssertions incluye los elementos en el fallo).
- Sin condicionales en el cuerpo: el filtrado es una consulta LINQ, igual que en la prueba existente de Domain.
- Con Application vacío el compilador no emite referencias, así que la prueba pasa. Su valor se demuestra en la fase Red con un tipo temporal (decisión D6 de research).

### Puerta de cobertura (FR-011 a FR-017)

Sin scripts: solo comandos documentados en `CLAUDE.md`.

1. Restaurar herramientas: `dotnet tool restore`.
2. Limpiar informes previos: borrar `TestResults/`.
3. Ejecutar con cobertura: `dotnet test --solution MiRestaurante.slnx --results-directory TestResults --coverlet --coverlet-output-format cobertura`.
4. Fusionar y aplicar el umbral: `dotnet reportgenerator` con los informes `TestResults/coverage.cobertura.*.xml`, destino `TestResults/report`, `-assemblyfilters` con los cuatro ensamblados de producción y `minimumCoverageThresholds:lineCoverage=80`.

`TestResults/` ya está ignorado por `.gitignore` (patrón `[Tt]est[Rr]esult*/`), así que FR-015 se cumple sin tocar ese archivo. El resultado del umbral es informativo en esta feature: ReportGenerator termina con código 1 si no se alcanza, y esa salida no bloquea la integración (excepción aceptada).

## Seguimiento de complejidad

| Violación                                                                                                                                                       | Por qué se necesita                                                                                                                                                                                                     | Alternativa más simple descartada porque                                                                                                                                                                                                                                 |
| --------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Principio VII y puerta de calidad 4: la cobertura de líneas ≥ 80 % no bloquea en esta feature                                                                   | Sin código de negocio, lo único medible es la raíz de composición de la API (`Program.cs`), que ninguna prueba ejecuta. El responsable del proyecto confirmó la excepción de forma explícita durante la especificación. | Añadir una prueba de humo, o excluir `Program.cs` de la cobertura, solo para llegar al 80 %: el responsable del proyecto lo rechazó. Vigencia: termina con esta feature; desde la primera feature con código de negocio la puerta 4 vuelve a ser bloqueante.             |
| `TestingPlatformCommandLineArguments=--ignore-exit-code 8` en `Directory.Build.props` (excepción temporal al principio VII, que no prevé proyectos sin pruebas) | Cuatro de los cinco proyectos no tienen pruebas aún y MTP termina con código 8 en ese caso.                                                                                                                             | Mantener una prueba vacía por proyecto: contradice el borrado de las plantillas. Configurarlo por proyecto o por variable de entorno: repite la configuración o no queda versionado. Se retira cuando los cinco proyectos tengan pruebas reales (fuera de alcance aquí). |

No se añaden otros paquetes ni proyectos que requieran justificación.
