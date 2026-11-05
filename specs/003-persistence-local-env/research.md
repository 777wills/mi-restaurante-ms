# Investigación: Persistencia y entorno local

Decisiones de la Fase 0. Todas las incógnitas del contexto técnico se resolvieron con un prototipo desechable (copia del repositorio en `/tmp`, fuera del árbol del proyecto) que implementó la feature completa y ejecutó la suite con cobertura: **145 pruebas en verde** (135 anteriores + las nuevas pruebas funcionales y de integración; la prueba de arquitectura se comprobó aparte, en Red) y **98,4 % de cobertura de líneas** sobre los cuatro ensamblados, sin exclusiones.

## D1. Dónde y cómo se registra el contexto

- **Decisión**: una clase estática `DependencyInjection` en Infrastructure con `AddInfrastructure(this IServiceCollection)`. El contexto se registra con `AddDbContext<ApplicationDbContext>((sp, options) => options.UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")))`; `IApplicationDbContext` se registra como `AddScoped` que resuelve `ApplicationDbContext`; el mismo método registra `AddHealthChecks().AddDbContextCheck<ApplicationDbContext>()`.
- **Justificación**: Api solo referencia Infrastructure «para registrar dependencias» (principio II) y no necesita conocer el contexto concreto. Leer la cadena **al resolver** (no al registrar) hace que cualquier configuración añadida después —`UseSetting` de `WebApplicationFactory`, variables de entorno— se vea siempre, y permite probar el registro con un `ServiceCollection` y un `IConfiguration` en memoria.
- **Alternativas**: registrar en `Program.cs` (obliga a Api a conocer `ApplicationDbContext` y Npgsql); leer la cadena al registrar (frágil con las fábricas de pruebas).
- **Verificado**: `IApplicationDbContext` y `ApplicationDbContext` resuelven la misma instancia en el ámbito; `ConfigurationBuilder` está disponible en el proyecto de integración sin paquetes extra.

## D2. Migraciones al arrancar en Development

- **Decisión**: `MigrateDatabaseAsync(this IServiceProvider, CancellationToken)` en la misma clase (ámbito → `ApplicationDbContext` → `Database.MigrateAsync(cancellationToken)`; el token es `ApplicationStopping` por el principio VIII). `Program.cs` lo espera con `await` tras `builder.Build()` y antes del pipeline, solo si `IsDevelopment()`.
- **Justificación**: la migración ocurre antes de atender peticiones (FR-013). Se comprobó con `WebApplicationFactory` que el código entre `Build()` y `Run()` termina antes de que la fábrica entregue el cliente (no hay carrera): las pruebas de historial de migraciones pasan.
- **Alternativas**: `IHostedService` (el orden respecto al servidor no está garantizado); método de extensión sobre `WebApplication` (arrastra tipos de ASP.NET a Infrastructure).
- **Verificado en vivo**: primer arranque contra una base vacía aplica `InitialCreate` y deja una fila en `__EFMigrationsHistory`; el segundo no vuelve a aplicarla.

## D3. Endpoint `/health`

- **Decisión**: `app.MapHealthChecks("/health")` con las opciones por defecto; el paquete `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` va en Infrastructure porque ahí se registra la comprobación.
- **Verificado**: con la base accesible responde `200` `text/plain` `Healthy`; con la base detenida, `503` `text/plain` `Unhealthy`; con la cadena vacía, `503` (las pruebas funcionales del prototipo cubren ambos casos). El cuerpo no contiene detalles. Cada comprobación ejecuta `SELECT 1` y EF lo registra en Information.
- **Alternativas**: personalizar el cuerpo (fuera de alcance por la spec).

## D4. Fallo de migración en Development

- **Decisión**: sin `try`/`catch`; el arranque falla y se deja que el entorno lo registre.
- **Verificado**: con la base detenida, EF registra `fail: Microsoft.EntityFrameworkCore.Database.Connection[20004]` (nivel Error) y la excepción `NpgsqlException` queda sin capturar (el proceso termina con código 134 y la traza en la salida). Sin cadena de conexión el resultado es análogo (`InvalidOperationException: The ConnectionString property has not been initialized`). No hace falta código extra para cumplir FR-015.
- **Nota**: en el primer arranque sobre una base vacía EF registra también un `fail: …Command[20102]` por el `SELECT` a `__EFMigrationsHistory` (la tabla aún no existe). Es normal y no es un fallo.

## D5. Migración inicial vacía

- **Decisión**: `dotnet ef migrations add InitialCreate --project src/MiRestaurante.Infrastructure --startup-project src/MiRestaurante.Api --output-dir Migrations`. Se versiona el resultado sin editarlo.
- **Verificado**: `dotnet ef` funciona sin cadena de conexión en `appsettings.json` (`UseNpgsql` acepta `null` al construir el contexto en tiempo de diseño), por lo que no hace falta `IDesignTimeDbContextFactory`. `Up` y `Down` salen vacíos; el modelo no tiene cambios pendientes. `dotnet format --verify-no-changes` y la compilación con `TreatWarningsAsErrors` pasan con los archivos generados.
- **Script idempotente** (`dotnet ef migrations script --idempotent …`): termina con código 0. Al ser vacía, solo contiene la creación de `__EFMigrationsHistory`; EF omite las migraciones sin operaciones en el script. Basta para US3-3.

## D6. `compose.yaml`

- **Decisión**: servicio `postgres`, `image: postgres:18`, `5432:5432`, variables `POSTGRES_DB=mirestaurante`, `POSTGRES_USER=mirestaurante`, `POSTGRES_PASSWORD=mirestaurante_dev`, volumen con nombre `postgres-data` montado en **`/var/lib/postgresql`**. Sin `healthcheck`.
- **Justificación**: desde la imagen 18, `PGDATA` por defecto es `/var/lib/postgresql/18/docker`, así que el volumen se monta un nivel por encima de `/data`.
- **Verificado**: el contenedor arranca, acepta conexiones y la API migra. La conservación de datos tras `down`/`up` se valida en el [quickstart](quickstart.md#2-persistencia-del-volumen-us1-escenario-4).

## D7. Base de datos en las pruebas

- **Decisión**: un `PostgreSqlFixture` por proyecto de pruebas (`IAsyncLifetime`, `new PostgreSqlBuilder("postgres:18")`), registrado con `[assembly: AssemblyFixture(typeof(PostgreSqlFixture))]`. Expone `CreateDatabaseAsync()`, que ejecuta `CREATE DATABASE test_<guid>` con `ExecScriptAsync` y devuelve su cadena de conexión.
- **Verificado**: xUnit v3 inyecta el fixture de ensamblado en el constructor de las fábricas de clase; `ApiFactory` implementa `IAsyncLifetime`, crea su base y fija `ConnectionStrings:DefaultConnection` con `UseSetting`. Las pruebas funcionales existentes pasan sin editar sus aserciones (solo cambian los constructores de las fábricas). `WithWebHostBuilder` sobrescribe la cadena para los casos «sin cadena» y «base inaccesible» (`Host=127.0.0.1;Port=1`, rechazo inmediato). En Development con la base inaccesible, `CreateClient()` lanza.
- **Por qué una base por fábrica**: la prueba de «Production no migra» necesita una base realmente vacía; compartir una base la contaminaría. Un único contenedor evita arrancar siete.
- **Limpieza (SC-004)**: `DisposeAsync` del fixture más el contenedor de limpieza de Testcontainers; se comprueba en el quickstart.
- **Alternativas**: una base compartida (contaminación entre pruebas); un contenedor por fábrica (lento); `IClassFixture` por clase (más contenedores).

## D8. Pruebas de integración

- **Decisión**: `ApplicationDbContextTests` construye un `ServiceCollection` con `IConfiguration` en memoria, `AddLogging()` y `AddInfrastructure()`; cada prueba recibe una base nueva. Compara `GetAppliedMigrationsAsync()` con `GetMigrations()` tras `MigrateAsync()` y usa `Database.HasPendingModelChanges()` para FR-021.
- **Verificado**: las 4 pruebas pasan; `HasPendingModelChanges()` existe en EF Core 10.

## D9. Regla de arquitectura

- **Decisión**: NetArchTest.eNhancedEdition: `Types.InAssembly(Layers.Load(Layers.Api)).That().Inherit(typeof(ControllerBase)).ShouldNot().HaveDependencyOnAny("MiRestaurante.Application.IApplicationDbContext")`, con el mismo estilo de aserción que `LayerDependencyTests` (`FailingTypes.Should().BeNullOrEmpty(...)`: `FailingTypes` es `null` cuando no hay incumplimientos).
- **Verificado (fase Red)**: un controlador temporal en el namespace `MiRestaurante.Api.Tmp` que hereda de una clase base abstracta que a su vez hereda de `ControllerBase` y recibe `IApplicationDbContext` por constructor hace fallar la prueba con la explicación «Has dependency on: MiRestaurante.Application.IApplicationDbContext» y su nombre completo. Cubre herencia indirecta y cualquier namespace (FR-025). Con cero controladores el conjunto es vacío y la prueba pasa.
- **Alternativa**: reflexión como en `ControllerConventionTests` (más código para detectar dependencias en constructores, campos y métodos).

## D10. Cobertura

- **Medido**: 98,4 % (125 de 127 líneas): Api 100 %, Application 100 %, Infrastructure 93,1 %. Lo único sin cubrir es `Down()` de `InitialCreate` (sin operaciones). No hace falta excluir nada ni probar la reversión.
- **Nota**: `Application` no tiene líneas ejecutables nuevas (la interfaz no genera código).

## D11. Paquetes

- Todas las versiones de la tabla del stack restauran y compilan sin advertencias. `Microsoft.EntityFrameworkCore.Design` queda con `PrivateAssets=all` (el `dotnet add package` lo añade así). Npgsql llega a los proyectos de pruebas de forma transitiva (referencias de proyecto), por lo que no se referencia de forma explícita; `ConfigurationBuilder` también.
- `dotnet-ef` 10.0.14 ya está en el manifiesto de herramientas locales (FR-012): no hay que modificarlo.
- `--ignore-exit-code 8` se mantiene en `Directory.Build.props` porque `Domain.UnitTests` sigue sin pruebas.
