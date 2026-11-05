# Plan de implementación: Persistencia y entorno local

**Rama**: `speckit-test` (feature `003-persistence-local-env`) | **Fecha**: 2026-11-04 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la feature en `specs/003-persistence-local-env/spec.md`

## Resumen

Dejar lista la tubería de persistencia antes de la primera feature de negocio, sin ninguna entidad:

1. `compose.yaml` levanta solo `postgres:18`; la cadena de conexión `DefaultConnection` vive en `appsettings.Development.json`.
2. Application declara `IApplicationDbContext` (solo `SaveChangesAsync`); Infrastructure contiene `ApplicationDbContext` (Npgsql), la migración inicial vacía `InitialCreate` y una única clase estática `DependencyInjection` con dos métodos: `AddInfrastructure()` (contexto, alias de la interfaz y `AddDbContextCheck`) y `MigrateDatabaseAsync(CancellationToken)`.
3. `Program.cs` llama a `AddInfrastructure()`, aplica las migraciones solo en Development antes de atender peticiones y mapea `/health` con el cuerpo de texto por defecto.
4. Las pruebas de integración y funcionales arrancan un contenedor `postgres:18` con Testcontainers (un contenedor por proyecto de pruebas, una base de datos nueva por fábrica o por prueba) y aplican las migraciones reales. Respawn queda pospuesto (FR-023).
5. Una prueba de arquitectura impide que cualquier tipo que herede de `ControllerBase` dependa de `IApplicationDbContext`.

El diseño se validó con un prototipo desechable que ejecutó la suite completa en verde con cobertura del 98,4 % sin exclusiones (ver [research.md](research.md)).

## Contexto técnico

**Lenguaje/Versión**: C# sobre .NET 10 (SDK 10.0.112 de `global.json` con `latestFeature`)

**Dependencias principales** (versiones de la tabla del stack de la constitución):

| Proyecto                        | Paquete nuevo                                                       | Versión |
| ------------------------------- | ------------------------------------------------------------------- | ------- |
| Application                     | `Microsoft.EntityFrameworkCore`                                     | 10.0.14 |
| Infrastructure                  | `Npgsql.EntityFrameworkCore.PostgreSQL`                             | 10.0.3  |
| Infrastructure                  | `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | 10.0.14 |
| Api                             | `Microsoft.EntityFrameworkCore.Design` (`PrivateAssets=all`)        | 10.0.14 |
| Infrastructure.IntegrationTests | `Testcontainers.PostgreSql`                                         | 4.16.0  |
| Api.FunctionalTests             | `Testcontainers.PostgreSql`                                         | 4.16.0  |

`dotnet-ef` 10.0.14 ya está en `.config/dotnet-tools.json` (FR-012 ya cumplido; solo se verifica). Respawn no se añade.

**Almacenamiento**: PostgreSQL 18 (imagen oficial `postgres:18`), con EF Core y migraciones

**Pruebas**: xUnit v3 sobre Microsoft.Testing.Platform, AwesomeAssertions, `Microsoft.AspNetCore.Mvc.Testing`, NetArchTest.eNhancedEdition (ya referenciados) y Testcontainers.PostgreSql; Docker obligatorio

**Plataforma de destino**: API web REST ejecutada en el equipo del desarrollador (`dotnet run`) con PostgreSQL en Docker Compose

**Tipo de proyecto**: web-service (API REST en cuatro capas)

**Objetivos de rendimiento**: N/A

**Restricciones**: cero advertencias (`TreatWarningsAsErrors`); `dotnet format` limpio (incluido el código generado de la migración); cobertura ≥ 80 % obligatoria sin exclusiones (FR-027); sin `try`/`catch` en el arranque (FR-015); `/health` sin detalles internos; sin `EnsureCreated`, InMemory ni SQLite; el código en inglés, los artefactos en español

**Escala/Alcance**: 1 `compose.yaml`, 1 interfaz, 1 contexto, 1 clase de registro y 3 archivos de migración (generados) en `src`; cambios en `Program.cs`, `appsettings.Development.json` y 4 `.csproj`; 1 fixture duplicado en 2 proyectos de pruebas, 1 clase de pruebas de integración, 1 de pruebas funcionales y 1 de arquitectura

No quedan marcadores `NEEDS CLARIFICATION`.

## Constitution Check

_Puerta: debe pasar antes de la Fase 0. Se reevalúa tras el diseño de la Fase 1._

| Principio                        | Evaluación                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | Resultado           |
| -------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------- |
| I. Simplicidad                   | Cero proyectos y cero patrones nuevos. Los 4 paquetes de producción y el de pruebas están exigidos por la constitución o por la spec, activos y sin vulnerabilidades en la restauración. `Microsoft.EntityFrameworkCore` en Application no se usa aún en código (la interfaz solo expone `SaveChangesAsync`): lo exige FR-009 y se usará en cuanto la primera feature añada `DbSet<T>`. Una sola clase estática de registro; sin repositorios, sin fábricas, sin `IDesignTimeDbContextFactory` (innecesaria: se probó que `dotnet ef` funciona sin cadena). El fixture de Testcontainers se duplica en dos proyectos (≈20 líneas) en vez de crear un proyecto compartido de utilidades. | Pasa                |
| II. Arquitectura en cuatro capas | `IApplicationDbContext` en Application sin referencias nuevas salvo el paquete permitido; contexto, migraciones y registro en Infrastructure; Api solo llama a `AddInfrastructure()` y `MigrateDatabaseAsync()` y no conoce el contexto concreto. La prueba de lista blanca de Application sigue pasando sin cambios. Nueva regla verificada en `Architecture.Tests` (FR-025).                                                                                                                                                                                                                                                                                                          | Pasa                |
| III. Modelo de dominio           | Sin entidades, fechas ni dinero.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        | N/A                 |
| IV. Capa de aplicación           | Sin servicios todavía.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  | N/A                 |
| V. Acceso a datos                | EF Core + Npgsql; `IApplicationDbContext` implementada por el `DbContext` concreto; sin repositorios ni Unit of Work; esquema solo con migraciones versionadas en Infrastructure; migraciones automáticas solo en Development; script idempotente comprobado. `HasData`, `IEntityTypeConfiguration<T>` y SQL explícito: sin objeto todavía.                                                                                                                                                                                                                                                                                                                                             | Pasa                |
| VI. API HTTP                     | `/health` con `AddDbContextCheck`, fuera de `/api/v1/` y disponible en todos los entornos. Sin cambios en el resto de convenciones.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     | Pasa                |
| VII. TDD (no negociable)         | Cada tarea de implementación va precedida de su prueba en Red. Testcontainers `postgres:18` (misma versión mayor que `compose.yaml`), esquema creado con las migraciones reales, sin InMemory ni SQLite, nomenclatura y una aserción lógica por prueba. Puerta 4 de cobertura bloqueante (medido: 98,4 %). **Desviación**: Respawn «DEBE limpiar todas las tablas» pero se pospone por falta de tablas (ver Seguimiento de complejidad).                                                                                                                                                                                                                                                | Pasa con desviación |
| VIII. Convenciones de código     | Namespaces de ámbito de archivo, `Nullable`, `TreatWarningsAsErrors`; `dotnet format --verify-no-changes` pasó sobre el código y la migración generados. Identificadores en inglés, artefactos en español.                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | Pasa                |
| IX. Entorno local                | `compose.yaml` solo con PostgreSQL y etiqueta `18`; cadena en `appsettings.Development.json` con credenciales locales.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  | Pasa                |

**Reevaluación tras la Fase 1**: el diseño no añade proyectos, abstracciones ni paquetes fuera de la lista. Las decisiones con efecto transversal (lectura perezosa de la cadena de conexión, un contenedor por proyecto de pruebas con una base de datos por fábrica) son configuración y pruebas, no capas. Resultado: **Pasa**, con la desviación de Respawn registrada.

## Estructura del proyecto

### Documentación (esta feature)

```text
specs/003-persistence-local-env/
├── plan.md              # Este archivo
├── research.md          # Fase 0
├── data-model.md        # Fase 1
├── quickstart.md        # Fase 1
├── contracts/
│   └── persistence-local-env.md  # Fase 1: /health y configuración
├── checklists/
└── tasks.md             # Fase 2 (/speckit-tasks; no lo crea /speckit-plan)
```

### Código fuente (raíz del repositorio)

```text
compose.yaml                                          # NUEVO: solo postgres:18

src/
├── MiRestaurante.Application/
│   ├── MiRestaurante.Application.csproj              # MODIFICADO: + Microsoft.EntityFrameworkCore
│   └── IApplicationDbContext.cs                      # NUEVO: SaveChangesAsync(CancellationToken)
├── MiRestaurante.Infrastructure/
│   ├── MiRestaurante.Infrastructure.csproj           # MODIFICADO: + Npgsql EF Core + HealthChecks EF Core
│   ├── ApplicationDbContext.cs                       # NUEVO: DbContext que implementa IApplicationDbContext
│   ├── DependencyInjection.cs                        # NUEVO: AddInfrastructure() y MigrateDatabaseAsync()
│   └── Migrations/                                   # NUEVO (generado por dotnet ef)
│       ├── <ts>_InitialCreate.cs
│       ├── <ts>_InitialCreate.Designer.cs
│       └── ApplicationDbContextModelSnapshot.cs
└── MiRestaurante.Api/
    ├── MiRestaurante.Api.csproj                      # MODIFICADO: + Microsoft.EntityFrameworkCore.Design
    ├── Program.cs                                    # MODIFICADO: registro, migración en Development, /health
    └── appsettings.Development.json                  # MODIFICADO: ConnectionStrings:DefaultConnection

tests/
├── MiRestaurante.Architecture.Tests/
│   └── ControllerDataAccessTests.cs                  # NUEVO (FR-025)
├── MiRestaurante.Infrastructure.IntegrationTests/
│   ├── MiRestaurante.Infrastructure.IntegrationTests.csproj  # MODIFICADO: + Testcontainers.PostgreSql
│   ├── PostgreSqlFixture.cs                          # NUEVO: contenedor por proyecto (AssemblyFixture)
│   └── ApplicationDbContextTests.cs                  # NUEVO (FR-019, FR-021, FR-008)
└── MiRestaurante.Api.FunctionalTests/
    ├── MiRestaurante.Api.FunctionalTests.csproj      # MODIFICADO: + Testcontainers.PostgreSql
    ├── PostgreSqlFixture.cs                          # NUEVO: igual que el de integración
    ├── ApiFactory.cs                                 # MODIFICADO: recibe el fixture, crea su base de datos
    └── PersistenceTests.cs                           # NUEVO (FR-013, FR-014, FR-015, FR-017)
```

**Decisión de estructura**: se conserva la estructura de cuatro capas y cinco proyectos de pruebas. La carpeta `Migrations/` la genera `dotnet ef`. Las pruebas existentes de las features 001 y 002 no se editan: solo cambian los constructores de las fábricas (FR-024).

## Diseño técnico

### Entorno local

- `compose.yaml`: servicio `postgres`, imagen `postgres:18`, puerto `5432:5432`, volumen con nombre montado en `/var/lib/postgresql` (la imagen 18 guarda los datos bajo esa ruta; no se monta en `/data`), variables `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` solo de desarrollo (D6). Sin `healthcheck`.
- `appsettings.Development.json`: `ConnectionStrings:DefaultConnection` apuntando a `localhost:5432` con esas credenciales. `appsettings.json` no cambia.

### Application e Infrastructure

- `IApplicationDbContext` con `Task<int> SaveChangesAsync(CancellationToken cancellationToken)`.
- `ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext`. Sin `DbSet`, sin configuraciones de entidad.
- `DependencyInjection.AddInfrastructure(this IServiceCollection)`: `AddDbContext<ApplicationDbContext>` con `UseNpgsql` leyendo `GetConnectionString("DefaultConnection")` del `IConfiguration` del proveedor **al resolver** (D1); `IApplicationDbContext` resuelve la misma instancia del ámbito; `AddHealthChecks().AddDbContextCheck<ApplicationDbContext>()`.
- `DependencyInjection.MigrateDatabaseAsync(this IServiceProvider, CancellationToken)`: crea un ámbito, resuelve `ApplicationDbContext` y llama a `Database.MigrateAsync(cancellationToken)`. Sin `try`/`catch` (D4).
- Migración: `dotnet ef migrations add InitialCreate --project src/MiRestaurante.Infrastructure --startup-project src/MiRestaurante.Api --output-dir Migrations`. Se versiona tal cual la genera la herramienta (vacía).

### Api: `Program.cs`

`builder.Services.AddInfrastructure()` junto al resto de servicios; tras `builder.Build()`, `if (app.Environment.IsDevelopment()) await app.Services.MigrateDatabaseAsync(app.Lifetime.ApplicationStopping);` antes de configurar el pipeline; `app.MapHealthChecks("/health")` junto a `MapControllers()`. Sin más cambios.

### Pruebas

| Proyecto                        | Clase                                | Cubre                                                                                                                                                                                                   | FR                      |
| ------------------------------- | ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------- |
| Architecture.Tests              | `ControllerDataAccessTests`          | `Inherit(ControllerBase)` + `HaveDependencyOnAny("MiRestaurante.Application.IApplicationDbContext")`; pasa con cero controladores                                                                       | FR-025                  |
| Infrastructure.IntegrationTests | `ApplicationDbContextTests`          | Todas las migraciones del proyecto registradas en una base nueva; sin cambios de modelo pendientes; resolución de la interfaz → contexto concreto; misma instancia en el ámbito                                                | FR-008, FR-019, FR-021  |
| Api.FunctionalTests             | `PersistenceTests`                   | Migra en Development; no migra en Production; `/health` 200 `Healthy`; `/health` 503 `Unhealthy` sin cadena y con base inaccesible; arranque en Development con base inaccesible lanza y registra Error | FR-013 a FR-017, FR-022 |
| Api.FunctionalTests             | (las pruebas funcionales existentes) | Siguen pasando sin cambiar sus aserciones                                                                                                                                                               | FR-024                  |

Verificados por comando y no por prueba automática (quickstart): script idempotente sin errores (US3-3), persistencia del volumen tras `docker compose down`/`up` (US1-4), ausencia de contenedores de prueba al terminar (SC-004), fase Red de la regla de arquitectura con un controlador temporal (SC-005).

**Infraestructura de pruebas** (D7): `PostgreSqlFixture` (`IAsyncLifetime`, `postgres:18`) registrado con `[assembly: AssemblyFixture]`; xUnit v3 lo inyecta en el constructor de las fábricas. `ApiFactory` implementa `IAsyncLifetime`, crea su base con `CREATE DATABASE` (`ExecScriptAsync`) y fija `ConnectionStrings:DefaultConnection` con `UseSetting`; los casos especiales usan `WithWebHostBuilder`. Las pruebas de integración crean una base nueva por prueba.

**Orden TDD**: en `tasks.md` cada prueba precede a su implementación y se ejecuta en Red. Las pruebas de integración fallan primero por no compilar o por falta de migración; las funcionales por falta de `/health` y de migraciones; la de arquitectura, con un controlador temporal que se revierte.

### Puerta de cobertura

Mismos comandos que en la feature 001 (ver [quickstart.md](quickstart.md#8-puerta-de-cobertura)). Obligatoria, sin exclusiones. `--ignore-exit-code 8` se mantiene: Domain.UnitTests sigue sin pruebas.

## Seguimiento de complejidad

| Violación                                                                                                            | Por qué se necesita                                                                                     | Alternativa más simple descartada y por qué                                                                                                                                                                                                                                                                                                                          |
| -------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Respawn no se integra (principio VII: «DEBE limpiar todas las tablas, salvo `__EFMigrationsHistory`, entre pruebas») | Sin tablas, `Respawner.CreateAsync` lanza «No tables found» (decisión de la spec, FR-023)               | Integrarlo con una tabla de relleno: añade una tabla y una migración sin valor. Se añade en la primera feature que cree tablas. La regla «limpiar todas las tablas» se cumple por vacuidad: tras esta feature la única tabla es `__EFMigrationsHistory`, que está excluida. El plan de esa feature DEBE incluir el paquete Respawn (versión 7 de la tabla del stack) |
| `Microsoft.EntityFrameworkCore` en Application sin uso en código                                                     | Lo exige FR-009 y el principio II; `IApplicationDbContext` lo necesitará con el primer `DbSet<T>`       | No referenciarlo hasta entonces: contradice la spec; coste de mantenerlo: cero                                                                                                                                                                                                                                                                                       |
| `PostgreSqlFixture` duplicado en dos proyectos de pruebas                                                            | Cada proyecto referencia solo su capa (principio VII) y un proyecto compartido sería un quinto concepto | Proyecto de utilidades de pruebas: más piezas que 20 líneas duplicadas                                                                                                                                                                                                                                                                                               |

No se añaden otros paquetes ni proyectos que requieran justificación.
