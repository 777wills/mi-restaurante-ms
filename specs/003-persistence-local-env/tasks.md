# Tasks: Persistencia y entorno local

**Entrada**: documentos de diseño en `/specs/003-persistence-local-env/`

**Prerequisitos**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/persistence-local-env.md`, `quickstart.md`

**Pruebas**: incluidas. El principio VII (TDD, no negociable) y FR-026 las exigen: cada prueba se escribe antes de su implementación y se ejecuta en Red. Nomenclatura `{Metodo}_Should_{Resultado}_When_{Escenario}`, una sola aserción lógica y ninguna lógica condicional (`if`, `switch`, bucles) en las pruebas. Sin InMemory ni SQLite: PostgreSQL real con Testcontainers y migraciones reales (FR-018, FR-019). **Docker es obligatorio** para ejecutar las pruebas de integración y funcionales.

**Organización**: tareas agrupadas por historia de usuario. Todo el código, los identificadores y las rutas van en inglés; los títulos, los mensajes al usuario y los artefactos, en español.

## Formato: `[ID] [P?] [Story] Descripción`

- **[P]**: se puede ejecutar en paralelo (archivos distintos, sin dependencia de tareas incompletas).
- **[Story]**: historia de usuario a la que pertenece (US1 a US5).
- Todas las rutas son relativas a la raíz del repositorio.

## Fase 1: Preparación

**Propósito**: línea base y paquetes (versiones de la tabla del stack de la constitución).

- [x] T001 Confirmar la línea base: `dotnet tool restore`, `dotnet tool list` (debe listar `dotnet-ef` 10.0.14 en `.config/dotnet-tools.json`, FR-012; no se modifica el manifiesto), `docker info` (Docker disponible), `dotnet build MiRestaurante.slnx` (0 advertencias) y `dotnet test --solution MiRestaurante.slnx` (código de salida 0).
- [x] T002 [P] Añadir `Microsoft.EntityFrameworkCore` 10.0.14 a `src/MiRestaurante.Application/MiRestaurante.Application.csproj` (FR-009; aún sin uso en código, lo exige la spec).
- [x] T003 [P] Añadir `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 y `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.14 a `src/MiRestaurante.Infrastructure/MiRestaurante.Infrastructure.csproj` (FR-009, FR-016).
- [x] T004 [P] Añadir `Microsoft.EntityFrameworkCore.Design` 10.0.14 con `PrivateAssets=all` a `src/MiRestaurante.Api/MiRestaurante.Api.csproj` (proyecto de arranque de las migraciones, FR-009).
- [x] T005 [P] Añadir `Testcontainers.PostgreSql` 4.16.0 a `tests/MiRestaurante.Infrastructure.IntegrationTests/MiRestaurante.Infrastructure.IntegrationTests.csproj` (FR-018). No referenciar Npgsql de forma explícita: llega de forma transitiva.
- [x] T006 [P] Añadir `Testcontainers.PostgreSql` 4.16.0 a `tests/MiRestaurante.Api.FunctionalTests/MiRestaurante.Api.FunctionalTests.csproj` (FR-018).
- [x] T007 Ejecutar `dotnet restore MiRestaurante.slnx` y `dotnet build MiRestaurante.slnx` (0 advertencias, sin avisos de vulnerabilidades) y `dotnet test --project tests/MiRestaurante.Architecture.Tests --filter-class "*ApplicationDependencyTests"` para confirmar que la lista blanca de Application sigue pasando sin cambios (FR-010).

## Fase 2: Fundamentos

**Propósito**: infraestructura de pruebas contra PostgreSQL real y núcleo de persistencia (interfaz, contexto, registro y migración inicial) que necesitan todas las historias. Cubre los escenarios 1 a 3 de la historia 4 (base efímera, migraciones reales, modelo sin cambios pendientes y resolución de `IApplicationDbContext`).

**⚠️ CRÍTICO**: ninguna historia puede empezar hasta completar esta fase.

- [x] T008 [P] Crear `tests/MiRestaurante.Infrastructure.IntegrationTests/PostgreSqlFixture.cs` (D7, FR-018): `PostgreSqlFixture : IAsyncLifetime` con `new PostgreSqlBuilder("postgres:18")` (misma versión mayor que `compose.yaml`; nunca `latest`), que arranca el contenedor en `InitializeAsync` y lo libera en `DisposeAsync`. Expone `CreateDatabaseAsync()`, que ejecuta `CREATE DATABASE test_<guid>` con `ExecScriptAsync` y devuelve la cadena de conexión de esa base (`NpgsqlConnectionStringBuilder` sobre la del contenedor con `Database` cambiado). Se registra con `[assembly: AssemblyFixture(typeof(PostgreSqlFixture))]` (un contenedor por proyecto de pruebas). Namespace de ámbito de archivo.
- [x] T009 [P] Crear `tests/MiRestaurante.Api.FunctionalTests/PostgreSqlFixture.cs`: copia de T008 con el namespace `MiRestaurante.Api.FunctionalTests` (duplicación aceptada en el plan: ≈20 líneas frente a un proyecto compartido).
- [x] T010 Editar `tests/MiRestaurante.Api.FunctionalTests/ApiFactory.cs` (depende de T009; FR-020, FR-024): `ApiFactory(string environment, PostgreSqlFixture postgres)` implementa además `IAsyncLifetime`; `InitializeAsync` crea su base con `postgres.CreateDatabaseAsync()` y la guarda; `ConfigureWebHost` fija `ConnectionStrings:DefaultConnection` con `builder.UseSetting(...)` (una base por fábrica). `DevelopmentApiFactory(PostgreSqlFixture postgres)` y `ProductionApiFactory(PostgreSqlFixture postgres)` pasan el fixture a la base. No se edita ninguna aserción de las pruebas existentes: solo cambian los constructores de las fábricas.
- [x] T011 Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests` y confirmar que las pruebas funcionales existentes siguen en verde con el nuevo andamiaje (la API aún no usa la base de datos).
- [x] T012 Escribir `tests/MiRestaurante.Infrastructure.IntegrationTests/ApplicationDbContextTests.cs` (Red, FR-008, FR-019, FR-021; D8): clase que recibe `PostgreSqlFixture` por constructor y, por prueba, crea una base nueva y un `ServiceCollection` con un `IConfiguration` en memoria (`ConnectionStrings:DefaultConnection`), `AddLogging()` y `AddInfrastructure()`. Cuatro pruebas, una aserción cada una: `MigrateAsync_Should_RegisterAllMigrations_When_DatabaseIsNew` (tras `MigrateAsync()`, `GetAppliedMigrationsAsync()` es igual a `GetMigrations()`); `HasPendingModelChanges_Should_BeFalse_When_DatabaseIsMigrated`; `Resolve_Should_ReturnApplicationDbContext_When_ApplicationDbContextInterfaceIsRequested` (`BeOfType<ApplicationDbContext>`); `Resolve_Should_ReturnSameInstance_When_ResolvedTwiceInSameScope` (la interfaz y el contexto concreto son la misma instancia). Ejecutar `dotnet test --project tests/MiRestaurante.Infrastructure.IntegrationTests` y confirmar Red por compilación (los tipos no existen).
- [x] T013 [P] Crear `src/MiRestaurante.Application/IApplicationDbContext.cs` (FR-006): interfaz pública con únicamente `Task<int> SaveChangesAsync(CancellationToken cancellationToken);`. Sin `DbSet<T>` ni referencias a proveedores. Namespace `MiRestaurante.Application`, de ámbito de archivo.
- [x] T014 Crear `src/MiRestaurante.Infrastructure/ApplicationDbContext.cs` (depende de T013; FR-007): `ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext`. Sin `DbSet` ni configuraciones de entidad.
- [x] T015 Crear `src/MiRestaurante.Infrastructure/DependencyInjection.cs` (depende de T014; FR-008, D1): clase estática con `AddInfrastructure(this IServiceCollection services)`: `AddDbContext<ApplicationDbContext>((sp, options) => options.UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")))` (la cadena se lee **al resolver**, no al registrar) y `AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>())`. Editar también `src/MiRestaurante.Api/Program.cs`: añadir `using MiRestaurante.Infrastructure;` y `builder.Services.AddInfrastructure();` junto al resto de servicios, para que `dotnet ef` pueda resolver el contexto desde el proyecto de arranque en T016 (sin esto EF no puede crear `ApplicationDbContext`, porque no hay `IDesignTimeDbContextFactory`). Ejecutar las pruebas de integración: pasan las de resolución del contexto y `HasPendingModelChanges` (modelo vacío); la de `InitialCreate` sigue en Red porque aún no hay migración.
- [x] T016 Generar la migración inicial (FR-011, D5): `dotnet ef migrations add InitialCreate --project src/MiRestaurante.Infrastructure --startup-project src/MiRestaurante.Api --output-dir Migrations`. Versionar `src/MiRestaurante.Infrastructure/Migrations/` tal cual la genera la herramienta, sin editarla: `Up` y `Down` vacíos. Ejecutar `dotnet test --project tests/MiRestaurante.Infrastructure.IntegrationTests` y confirmar Green de las cuatro pruebas.

**Punto de control**: base PostgreSQL efímera disponible en las dos suites de pruebas; contexto, registro y migración inicial funcionando.

## Fase 3: Historia de usuario 1 — Base de datos local con un solo comando (P1) 🎯 MVP

**Objetivo**: `docker compose up` levanta PostgreSQL 18 y la API, al arrancar en Development sin configuración adicional, se conecta y aplica sus migraciones antes de atender peticiones; si la base no está disponible, el arranque falla y el error queda registrado.

**Prueba independiente**: arrancar la API en Development contra una base vacía y comprobar el historial de migraciones; repetir con la base inaccesible y comprobar que el arranque lanza y registra un Error.

### Pruebas (Red)

- [x] T017 [US1] Crear `tests/MiRestaurante.Api.FunctionalTests/PersistenceTests.cs` con `DevelopmentApiFactory` por `IClassFixture` (FR-013, FR-015, FR-022): `Startup_Should_ApplyAllMigrations_When_EnvironmentIsDevelopment` (`GetAppliedMigrationsAsync()` obtenido de un ámbito de `factory.Services` es igual a `GetMigrations()`); `Startup_Should_Throw_When_DevelopmentDatabaseIsUnreachable` (`factory.WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:DefaultConnection", "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x")).CreateClient()` lanza; rechazo inmediato); `Startup_Should_LogError_When_DevelopmentDatabaseIsUnreachable` (`factory.Logs` contiene una entrada de nivel Error de categoría `Microsoft.EntityFrameworkCore.Database.Connection`). El registro se comprueba con el `CapturingLoggerProvider` existente.
- [x] T018 [US1] Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*PersistenceTests"` y confirmar Red (la API no migra ni lanza).

### Implementación

- [x] T019 [P] [US1] Crear `compose.yaml` en la raíz (FR-001 a FR-003, D6): un único servicio `postgres`, `image: postgres:18` (nunca `latest`), puerto `5432:5432`, `POSTGRES_DB=mirestaurante`, `POSTGRES_USER=mirestaurante`, `POSTGRES_PASSWORD=mirestaurante_dev` (solo desarrollo local) y volumen con nombre `postgres-data` montado en `/var/lib/postgresql` (la imagen 18 guarda los datos bajo esa ruta; no usar `/data`). Sin `healthcheck` ni otros servicios.
- [x] T020 [P] [US1] Editar `src/MiRestaurante.Api/appsettings.Development.json` (FR-004): añadir `ConnectionStrings:DefaultConnection` = `Host=localhost;Port=5432;Database=mirestaurante;Username=mirestaurante;Password=mirestaurante_dev` conservando la sección `Logging`. `appsettings.json` NO se toca (sin cadena de conexión, FR-004/FR-005).
- [x] T021 [US1] Editar `src/MiRestaurante.Infrastructure/DependencyInjection.cs` (FR-013, FR-015, D2, D4): añadir `MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)`: crea un ámbito, resuelve `ApplicationDbContext` y llama a `Database.MigrateAsync(cancellationToken)` (principio VIII: `CancellationToken` propagado en todo I/O). Sin `try`/`catch`.
- [x] T022 [US1] Editar `src/MiRestaurante.Api/Program.cs` (depende de T021; `AddInfrastructure()` y el `using` ya se añadieron en T015): tras `var app = builder.Build();` y antes de `app.UseExceptionHandler();`, añadir `if (app.Environment.IsDevelopment()) { await app.Services.MigrateDatabaseAsync(app.Lifetime.ApplicationStopping); }`. Sin `try`/`catch`.
- [x] T023 [US1] Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests` y confirmar Green de `PersistenceTests` y de las pruebas funcionales existentes.
- [x] T024 [US1] Validar a mano los escenarios 1 a 5 de la historia 1 (quickstart §2 y la segunda parte de §4): `docker compose up -d`, `docker compose ps` (un único contenedor `postgres:18` en el 5432), `dotnet run --project src/MiRestaurante.Api --launch-profile http`, consulta a `__EFMigrationsHistory` (una fila `…_InitialCreate`), `docker compose down` + `up -d` (la fila persiste; segundo arranque sin reaplicar) y arranque en Development con `docker compose stop postgres` (falla con `fail: Microsoft.EntityFrameworkCore.Database.Connection`). Volver a levantar la base con `docker compose start postgres`.

**Punto de control**: la historia 1 es funcional y verificable por sí sola (MVP).

## Fase 4: Historia de usuario 2 — Comprobación de salud de la API y su base de datos (P1)

**Objetivo**: `/health` en todos los entornos responde 200 `Healthy` con la base accesible y 503 `Unhealthy` si está inaccesible o falta la cadena, en texto plano y sin detalles internos.

**Prueba independiente**: pedir `/health` con la base accesible, con una base inaccesible y sin cadena de conexión.

**Dependencia**: usa `AddInfrastructure()` y `Program.cs` de la historia 1.

### Pruebas (Red)

- [x] T025 [US2] Editar `tests/MiRestaurante.Api.FunctionalTests/PersistenceTests.cs` (FR-016, FR-017, FR-022): añadir `ProductionApiFactory` por `IClassFixture` y las pruebas, una aserción cada una: `Health_Should_ReturnOk_When_DatabaseIsReachable` (Development), `Health_Should_ReturnHealthyBody_When_DatabaseIsReachable` (cuerpo exactamente `Healthy`), `Health_Should_ReturnTextPlain_When_DatabaseIsReachable`, `Health_Should_ReturnOk_When_EnvironmentIsProduction` (disponible en todos los entornos), `Health_Should_ReturnServiceUnavailable_When_ConnectionStringIsMissing` y `Health_Should_ReturnServiceUnavailable_When_DatabaseIsUnreachable` (ambas con `ProductionApiFactory.WithWebHostBuilder`: cadena vacía y `Host=127.0.0.1;Port=1;…`, para no fallar en el arranque de Development), y `Health_Should_ReturnOnlyUnhealthyBody_When_DatabaseIsUnreachable` (cuerpo exactamente `Unhealthy`, sin mensajes ni detalles internos) y `Health_Should_ReturnTextPlain_When_DatabaseIsUnreachable` (`Content-Type` `text/plain`, también con `ProductionApiFactory.WithWebHostBuilder` y `Host=127.0.0.1;Port=1;…`).
- [x] T026 [US2] Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*PersistenceTests"` y confirmar Red de las pruebas de `/health` (404: la ruta no existe).

### Implementación

- [x] T027 [US2] Editar `src/MiRestaurante.Infrastructure/DependencyInjection.cs` (FR-016, D1): en `AddInfrastructure` añadir `services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();` (única comprobación).
- [x] T028 [US2] Editar `src/MiRestaurante.Api/Program.cs` (depende de T027; D3): `app.MapHealthChecks("/health");` junto a `app.MapControllers();`, sin opciones (cuerpo de texto por defecto, ruta exacta fuera de `/api/v1/`).
- [x] T029 [US2] Ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests` y confirmar Green de todas.
- [x] T030 [US2] Validar a mano el quickstart §3 (`curl -si http://localhost:5110/health` con la base arriba y con `docker compose stop postgres`): `200 Healthy` y `503 Unhealthy`, ambas `text/plain`.

**Punto de control**: las historias 1 y 2 son funcionales (P1 completas).

## Fase 5: Historia de usuario 3 — Migraciones solo automáticas en Development (P2)

**Objetivo**: ningún entorno distinto de Development cambia su esquema al arrancar; el script idempotente se genera sin errores.

**Prueba independiente**: arrancar en Production contra una base vacía y comprobar que no hay historial de migraciones.

**Dependencia**: `Program.cs` de la historia 1. El escenario 1 (Development migra) ya lo cubre T017.

- [x] T031 [US3] Editar `tests/MiRestaurante.Api.FunctionalTests/PersistenceTests.cs` (FR-014, FR-022): añadir `Startup_Should_NotApplyMigrations_When_EnvironmentIsProduction` (con `ProductionApiFactory`, `GetAppliedMigrationsAsync()` obtenido de un ámbito de `factory.Services` está vacío; una aserción).
- [x] T032 [US3] Demostrar Red (SC-003): quitar temporalmente la condición `IsDevelopment()` de la llamada a `MigrateDatabaseAsync` en `src/MiRestaurante.Api/Program.cs`, ejecutar `dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*PersistenceTests"` y confirmar que falla solo la prueba de Production; restaurar la condición y repetir para confirmar Green. La prueba pasaría desde el primer momento (la condición ya existe), por eso la fase Red se demuestra así.
- [x] T033 [US3] Comprobar el script idempotente (US3-3, D5): `dotnet ef migrations script --idempotent --project src/MiRestaurante.Infrastructure --startup-project src/MiRestaurante.Api` termina con código de salida 0 y emite SQL (con la migración vacía, solo la creación de `__EFMigrationsHistory`).

**Punto de control**: la regla «migrar solo en Development» está protegida por una prueba.

## Fase 6: Historia de usuario 4 — Infraestructura de pruebas contra PostgreSQL real (P2)

**Objetivo**: las suites de integración y funcionales crean su propio PostgreSQL 18 efímero con las migraciones reales y no dejan contenedores. Los escenarios 1 a 3 los cubren los Fundamentos (T008 a T016); esta fase verifica el resto.

**Prueba independiente**: con Docker disponible y ningún contenedor levantado a mano, ejecutar la suite completa y comprobar que pasa y que no queda ningún contenedor.

**Dependencia**: Fundamentos y las historias 1 a 3 (para que las funcionales ejerciten migraciones y `/health`).

- [x] T034 [US4] Ejecutar `docker compose down` y luego `dotnet test --solution MiRestaurante.slnx` sin ningún contenedor levantado a mano (US4-1, US4-4): código de salida 0, con las pruebas funcionales existentes sin cambios en sus aserciones (FR-024) y todas las nuevas en verde.
- [x] T035 [US4] Ejecutar `docker ps --filter ancestor=postgres:18` al terminar la suite y confirmar que no lista ningún contenedor (US4-5, SC-004). Si queda alguno, corregir la liberación en `PostgreSqlFixture.DisposeAsync` de ambos proyectos (T008, T009).

**Punto de control**: las suites usan PostgreSQL real, efímero y limpio.

## Fase 7: Historia de usuario 5 — Los controladores no acceden directamente a los datos (P3)

**Objetivo**: la suite de arquitectura falla si un tipo que hereda de `ControllerBase`, en cualquier namespace, depende de `IApplicationDbContext`.

**Prueba independiente**: añadir temporalmente un controlador que dependa de `IApplicationDbContext` y comprobar que la prueba falla nombrándolo; al quitarlo, pasa (también con cero controladores).

**Dependencia**: T013 (la interfaz). No usa Docker.

- [x] T036 [US5] Crear `tests/MiRestaurante.Architecture.Tests/ControllerDataAccessTests.cs` (FR-025, D9): `Controllers_Should_NotDependOnApplicationDbContext_When_HandlingRequests` con `Types.InAssembly(Layers.Load(Layers.Api)).That().Inherit(typeof(ControllerBase)).ShouldNot().HaveDependencyOnAny($"{Layers.Application}.IApplicationDbContext").GetResult()` y una sola aserción `result.FailingTypes.Should().BeNullOrEmpty("controllers must use application services instead of IApplicationDbContext")`, con el mismo estilo que `LayerDependencyTests`. No modificar `LayerDependencyTests.cs` ni `ControllerConventionTests.cs`.
- [x] T037 [US5] Demostrar Red (SC-005): crear temporalmente en `src/MiRestaurante.Api/Tmp/` una clase base abstracta que herede de `ControllerBase` y un controlador en el namespace `MiRestaurante.Api.Tmp` que herede de ella y reciba `IApplicationDbContext` por constructor; ejecutar `dotnet test --project tests/MiRestaurante.Architecture.Tests --filter-class "*ControllerDataAccessTests"` y confirmar que falla y que el mensaje nombra el controlador (herencia indirecta y namespace cualquiera).
- [x] T038 [US5] Borrar `src/MiRestaurante.Api/Tmp/` y ejecutar `dotnet test --project tests/MiRestaurante.Architecture.Tests` para confirmar Green (con cero controladores el conjunto es vacío y la prueba pasa); comprobar con `git status` que no queda rastro.

**Punto de control**: la regla de acceso a datos está activa y probada en Red y Green.

## Fase final: Pulido y validación transversal

**Propósito**: puertas de calidad y revisión estática de la feature.

- [x] T039 Ejecutar `dotnet format MiRestaurante.slnx` y luego `dotnet format --verify-no-changes MiRestaurante.slnx`; debe terminar sin cambios pendientes, incluidos los archivos generados de `src/MiRestaurante.Infrastructure/Migrations/` (SC-006).
- [x] T040 Ejecutar `dotnet build MiRestaurante.slnx` (0 advertencias, 0 errores) y `dotnet test --solution MiRestaurante.slnx` (código de salida 0) (SC-006).
- [x] T041 Ejecutar la puerta de cobertura de `CLAUDE.md` (`dotnet tool restore`, `rm -rf TestResults`, prueba con `--coverlet --coverlet-output-format cobertura`, ReportGenerator con `minimumCoverageThresholds:lineCoverage=80`) e informar el resultado de `TestResults/report/Summary.txt`. En esta feature es **obligatoria y sin exclusiones** (FR-027): ReportGenerator DEBE terminar con código 0 (el prototipo midió 98,4 %); si no, añadir pruebas antes de integrar. Comprobar que `git status` no muestra `TestResults/`.
- [x] T042 Revisión estática (FR-005, FR-015, FR-023): `grep -rnw "try\|catch" src/MiRestaurante.Infrastructure/DependencyInjection.cs src/MiRestaurante.Api/Program.cs` sin coincidencias; `src/MiRestaurante.Api/appsettings.json` sin `ConnectionStrings`; `compose.yaml` con un único servicio y sin `healthcheck`; ninguna referencia a `Respawn`, `EnsureCreated`, `UseInMemoryDatabase` ni SQLite en el repositorio.
- [x] T043 Ejecutar los escenarios 1 a 8 de `specs/003-persistence-local-env/quickstart.md` que no se hayan cubierto ya en T024, T030, T033, T035 y T038 (§1 compilación y formato, §4 primera parte con `ASPNETCORE_ENVIRONMENT=Production` sobre una base recreada con `docker compose down -v && docker compose up -d`) y confirmar los resultados esperados. Al terminar, `docker compose down` y comprobar con `git status` que no quedan archivos temporales.

## Dependencias y orden de ejecución

### Dependencias entre fases

- **Preparación (Fase 1)**: sin dependencias.
- **Fundamentos (Fase 2)**: depende de la Preparación y **bloquea US1 a US4**. US5 solo necesita T013 (la interfaz).
- **US1 (P1)**: depende de los Fundamentos. Es el MVP.
- **US2 (P1)**: depende de US1 (`AddInfrastructure()` y `Program.cs`).
- **US3 (P2)**: depende de US1 (`MigrateDatabaseAsync` y la condición de entorno).
- **US4 (P2)**: la infraestructura la entregan los Fundamentos; la verificación final depende de US1 a US3.
- **US5 (P3)**: independiente de US1 a US4; puede hacerse tras T013.
- **Pulido**: depende de las historias completadas.

### Dentro de cada historia

- La prueba se escribe y se ejecuta en Red antes de implementar.
- Las tareas que editan `src/MiRestaurante.Api/Program.cs` (T015, T022, T028, y la modificación temporal de T032), `src/MiRestaurante.Infrastructure/DependencyInjection.cs` (T015, T021, T027) y `tests/MiRestaurante.Api.FunctionalTests/PersistenceTests.cs` (T017, T025, T031) NO son paralelizables entre sí: aplicar en orden de fase.
- Docker en marcha es requisito de T011, T012 (en Green), T016, T023, T029, T034 y de las ejecuciones de las pruebas funcionales y de integración.

### Oportunidades de paralelismo

- Preparación: T002 a T006 (csproj distintos).
- Fundamentos: T008 y T009 (archivos distintos); T013 en paralelo con T008 a T012.
- US1: T019 y T020 (archivos distintos).
- US5 completa en paralelo con US1 a US4 (proyecto de pruebas distinto, sin Docker, sin tocar `Program.cs`).

## Ejemplos de ejecución en paralelo

```text
# Preparación:
Tarea: "Microsoft.EntityFrameworkCore en Application.csproj"                         (T002)
Tarea: "Npgsql + HealthChecks.EntityFrameworkCore en Infrastructure.csproj"          (T003)
Tarea: "Microsoft.EntityFrameworkCore.Design en Api.csproj"                          (T004)
Tarea: "Testcontainers.PostgreSql en IntegrationTests.csproj"                        (T005)
Tarea: "Testcontainers.PostgreSql en FunctionalTests.csproj"                         (T006)

# Fundamentos:
Tarea: "PostgreSqlFixture.cs en tests/MiRestaurante.Infrastructure.IntegrationTests" (T008)
Tarea: "PostgreSqlFixture.cs en tests/MiRestaurante.Api.FunctionalTests"             (T009)
Tarea: "IApplicationDbContext.cs en src/MiRestaurante.Application"                   (T013)

# US1, implementación:
Tarea: "compose.yaml en la raíz"                                                     (T019)
Tarea: "ConnectionStrings en appsettings.Development.json"                           (T020)
```

## Estrategia de implementación

### MVP primero (solo historia 1)

1. Fase 1: Preparación.
2. Fase 2: Fundamentos (PostgreSQL efímero en pruebas, contexto y migración inicial).
3. Fase 3: US1.
4. **Parar y validar**: `docker compose up` + API en Development migran la base y la suite de `PersistenceTests` pasa.

### Entrega incremental

1. Preparación y Fundamentos.
2. US1 (MVP), luego US2 (`/health`) para completar las historias P1.
3. US3 (migraciones solo en Development) y la verificación de US4 (suite limpia sin contenedores).
4. US5 (regla de arquitectura) en cualquier punto tras T013.
5. Pulido y puertas de calidad; la cobertura ≥ 80 % es **obligatoria** en esta feature (FR-027). Respawn queda pospuesto (FR-023).

## Fase 8: Convergencia

- [x] T044 Sustituir `response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain")` en las pruebas `Health_Should_ReturnTextPlain_When_DatabaseIsReachable` y `Health_Should_ReturnTextPlain_When_DatabaseIsUnreachable` de `tests/MiRestaurante.Api.FunctionalTests/PersistenceTests.cs` por una aserción sin acceso condicional (`ContentType`/`MediaType` no nulo y igual a `text/plain`), de modo que una respuesta sin `Content-Type` falle en lugar de pasar en silencio per FR-026 (partial)
- [x] T045 Corregir `HasPendingModelChanges_Should_BeFalse_When_DatabaseIsMigrated` en `tests/MiRestaurante.Infrastructure.IntegrationTests/ApplicationDbContextTests.cs`: migrar la base con `MigrateAsync` antes de comprobar `HasPendingModelChanges()` (o renombrar la prueba para reflejar que no se migra) para que cumpla el escenario «base recién migrada» per US4/AC2 (partial)
