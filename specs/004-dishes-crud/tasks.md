---

description: "Lista de tareas de la feature 004-dishes-crud"
---

# Tasks: CRUD de platos

**Entrada**: documentos de diseño en `/specs/004-dishes-crud/`

**Prerequisitos**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/dishes-api.md`, `quickstart.md`

**Pruebas**: incluidas. El principio VII (TDD, no negociable) y FR-029 las exigen: cada prueba se escribe antes de su implementación y se ejecuta en Red. Nomenclatura `{Metodo}_Should_{Resultado}_When_{Escenario}`, una sola aserción lógica y ninguna lógica condicional (`if`, `switch`, bucles, ternarios) en las pruebas; los escenarios parametrizados usan `[Theory]` con `[InlineData]`. Sin InMemory ni SQLite: PostgreSQL real con Testcontainers (`postgres:18`) y migraciones reales. Moq solo para `TimeProvider`; NO se simulan `DbSet`, el `DbContext` ni `IApplicationDbContext`. Las horas de las pruebas son fijas y sin fracción de microsegundo (research D9). Las pruebas funcionales de error reutilizan el patrón `ReadProblemAsync` de `ExceptionHandlingTests`: una aserción por prueba (estado, tipo de contenido, `errors` o `detail`). **Docker es obligatorio** para las pruebas de integración y funcionales.

**Organización**: tareas agrupadas por historia de usuario. El código, los identificadores, las rutas y las propiedades JSON van en inglés; los mensajes al usuario y los artefactos, en español. Ninguna prueba existente de las features 001 a 003 se edita (FR-027, FR-031).

## Formato: `[ID] [P?] [Story] Descripción`

- **[P]**: se puede ejecutar en paralelo (archivos distintos, sin dependencia de tareas incompletas).
- **[Story]**: historia de usuario a la que pertenece (US1 a US5).
- Todas las rutas son relativas a la raíz del repositorio.

## Fase 1: Preparación

**Propósito**: línea base y paquetes de pruebas (versiones de la tabla del stack de la constitución; research D15). No se añade ningún paquete de producción.

- [X] T001 Confirmar la línea base: `dotnet tool restore`, `docker info` (Docker disponible), `dotnet build MiRestaurante.slnx` (0 advertencias) y `dotnet test --solution MiRestaurante.slnx` (código de salida 0).
- [X] T002 [P] Añadir `Respawn` 7.0.0 y `Moq` 4.21.0 a `tests/MiRestaurante.Infrastructure.IntegrationTests/MiRestaurante.Infrastructure.IntegrationTests.csproj` (FR-028, research D10 y D11). No referenciar `Npgsql` de forma explícita: llega de forma transitiva. NO usar Moq 4.20.0 ni 4.20.1.
- [X] T003 [P] Añadir `Respawn` 7.0.0 a `tests/MiRestaurante.Api.FunctionalTests/MiRestaurante.Api.FunctionalTests.csproj` (FR-028).
- [X] T004 Ejecutar `dotnet restore MiRestaurante.slnx`, `dotnet build MiRestaurante.slnx` (0 advertencias) y `dotnet list MiRestaurante.slnx package --vulnerable --include-transitive` y `--deprecated`: no deben informar de nada.

## Fase 2: Fundamentos

**Propósito**: entidad, contexto, configuración del modelo, migración `AddDishes` (con colación, índice único filtrado y semilla) e infraestructura de Respawn que necesitan todas las historias. La migración se genera una sola vez, después de ver fallar la prueba de la semilla (plan, «Orden TDD»); por eso la semilla (FR-024, FR-026; US5 escenario 1) se implementa aquí.

**⚠️ CRÍTICO**: ninguna historia puede empezar hasta completar esta fase.

- [X] T005 Escribir `tests/MiRestaurante.Infrastructure.IntegrationTests/DishSeedTests.cs` (Red; FR-024, FR-026, research D5): clase que recibe `PostgreSqlFixture` por constructor y, **por prueba**, crea una base nueva con `database.CreateDatabaseAsync()`, construye un `ApplicationDbContext` con `new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options` y llama a `Database.MigrateAsync(TestContext.Current.CancellationToken)` (sin Respawn: base recién migrada). Prueba `MigrateAsync_Should_CreateSeedDishes_When_DatabaseIsNew`: `db.Dishes.OrderBy(d => d.Id).ToListAsync()` debe ser `BeEquivalentTo(expected, o => o.WithStrictOrdering())` con estos 5 platos, todos con `CreatedAt` = `UpdatedAt` = `new DateTimeOffset(2026, 11, 4, 0, 0, 0, TimeSpan.Zero)` y `DeletedAt` = `null`:
  - Id 1, «Bandeja paisa», «Fríjoles, arroz, chicharrón, carne molida, chorizo, huevo frito, tajada de maduro, aguacate y arepa.», `38000.00m`, disponible, `https://upload.wikimedia.org/wikipedia/commons/1/12/Bandepaisabog.JPG`
  - Id 2, «Ajiaco santafereño», «Sopa de tres papas con pollo, mazorca y guascas, servida con crema, alcaparras y aguacate.», `32000.00m`, disponible, `https://upload.wikimedia.org/wikipedia/commons/d/d7/Ajiaco.jpg`
  - Id 3, «Sancocho de hueso», «Caldo de hueso de res con yuca, plátano, papa y mazorca, acompañado de arroz y aguacate.», `28000.00m`, disponible, `https://upload.wikimedia.org/wikipedia/commons/4/42/Sancocho-hueso.JPG`
  - Id 4, «Lechona», «Cerdo relleno de arroz y arveja, horneado durante horas, servido con arepa e insulso.», `30000.00m`, **no disponible**, `https://upload.wikimedia.org/wikipedia/commons/9/92/Plato_Lechona.jpg`
  - Id 5, «Arepa de huevo», «Arepa de maíz frita rellena de huevo, típica de la costa Caribe.», `8000.00m`, disponible, `https://upload.wikimedia.org/wikipedia/commons/2/21/Arepa_de_huevo_por_dentro.jpg`

  Ejecutar `dotnet test --project tests/MiRestaurante.Infrastructure.IntegrationTests` y confirmar Red por compilación (`Dish` y `Dishes` no existen).
- [X] T006 Crear `src/MiRestaurante.Domain/Dish.cs` (FR-001, principio III, data-model): clase pública anémica `Dish`, sin atributos, namespace `MiRestaurante.Domain` de ámbito de archivo, con `int Id`, `string Name` (inicializado a `string.Empty`), `string? Description`, `decimal Price`, `string? ImageUrl`, `bool IsAvailable`, `DateTimeOffset CreatedAt`, `DateTimeOffset UpdatedAt` y `DateTimeOffset? DeletedAt`, todas `{ get; set; }`.
- [X] T007 [P] Editar `src/MiRestaurante.Application/IApplicationDbContext.cs` (depende de T006; principio V): añadir `DbSet<Dish> Dishes { get; }` junto al `SaveChangesAsync(CancellationToken)` existente.
- [X] T008 [P] Crear `src/MiRestaurante.Infrastructure/Configurations/DishConfiguration.cs` (depende de T006; research D1, D2, D5; data-model): `internal sealed class DishConfiguration : IEntityTypeConfiguration<Dish>` con `Name` `IsRequired().HasMaxLength(100).UseCollation("case_insensitive")`; `Description` `HasMaxLength(500)`; `ImageUrl` `HasMaxLength(2048)`; `Price` `HasPrecision(10, 2)`; `HasIndex(d => d.Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL")`; y `HasData` con los 5 platos de T005 (valores literales de la tabla «Datos semilla» de la spec). No configurar la identidad (`HasIdentityOptions` descartado en D5) ni `HasQueryFilter` (D4).
- [X] T009 Editar `src/MiRestaurante.Infrastructure/ApplicationDbContext.cs` (depende de T007 y T008; research D1, D2): añadir `public DbSet<Dish> Dishes => Set<Dish>();` y sobrescribir `OnModelCreating` con `modelBuilder.HasCollation("case_insensitive", locale: "es-u-ks-level2", provider: "icu", deterministic: false);` y `modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);`. Ejecutar `dotnet test --project tests/MiRestaurante.Infrastructure.IntegrationTests` y confirmar Red de `DishSeedTests` (la tabla no existe) y de `HasPendingModelChanges_Should_BeFalse_When_DatabaseIsMigrated` (falta la migración).
- [X] T010 Generar la migración (FR-024, FR-025, principio V): `dotnet ef migrations add AddDishes --project src/MiRestaurante.Infrastructure --startup-project src/MiRestaurante.Api --output-dir Migrations`. Versionar `src/MiRestaurante.Infrastructure/Migrations/<ts>_AddDishes.cs`, `<ts>_AddDishes.Designer.cs` y `ApplicationDbContextModelSnapshot.cs` tal como los genera la herramienta, sin editarlos. Revisar que `Up` contiene la colación `case_insensitive`, la tabla `Dishes`, los 5 `InsertData`, el índice único `IX_Dishes_Name` con filtro `"DeletedAt" IS NULL` y el `setval` de la secuencia (D5). Ejecutar `dotnet test --project tests/MiRestaurante.Infrastructure.IntegrationTests` y confirmar Green de `DishSeedTests` y de `ApplicationDbContextTests` (FR-031).
- [X] T011 [P] Crear `tests/MiRestaurante.Infrastructure.IntegrationTests/MigratedDatabaseFixture.cs` (depende de T010; FR-028, research D11): `public sealed class MigratedDatabaseFixture(PostgreSqlFixture database) : IAsyncLifetime` (fixture de clase que recibe el fixture de ensamblado). `InitializeAsync`: crea una base con `database.CreateDatabaseAsync()`, guarda `ConnectionString`, la migra con `CreateContext().Database.MigrateAsync()` y crea **una sola vez** el `Respawner` con `Respawner.CreateAsync(connection, new RespawnerOptions { DbAdapter = DbAdapter.Postgres, TablesToIgnore = ["__EFMigrationsHistory"] })` sobre una `NpgsqlConnection` abierta que conserva. Expone `string ConnectionString`, `ApplicationDbContext CreateContext(params IInterceptor[] interceptors)` (`UseNpgsql(ConnectionString).AddInterceptors(interceptors)`) y `Task ResetAsync()` (`respawner.ResetAsync(connection)`). `DisposeAsync` libera la conexión.
- [X] T012 [P] Editar `tests/MiRestaurante.Api.FunctionalTests/ApiFactory.cs` (depende de T010; FR-028, research D11): añadir **solo** el método público `ResetDatabaseAsync()`, que primero fuerza el arranque del host (`_ = Services;`, para que la migración de Development exista), abre una `NpgsqlConnection` sobre `ConnectionString`, crea el `Respawner` la primera vez (mismas opciones que T011: `DbAdapter.Postgres`, `TablesToIgnore = ["__EFMigrationsHistory"]`) y lo reutiliza en las llamadas siguientes, y ejecuta `ResetAsync`. Sobrescribir `DisposeAsync()` para liberar la conexión y llamar a `base.DisposeAsync()`. No cambiar los constructores ni `ConfigureWebHost`.
- [X] T013 Ejecutar `dotnet test --solution MiRestaurante.slnx` y confirmar que todas las pruebas existentes de las features 001 a 003 siguen en verde sin cambios (FR-027, FR-031).

**Punto de control**: tabla `Dishes` con colación, índice filtrado y semilla generados por la migración; fixtures con Respawn disponibles en las dos suites con base de datos.

## Fase 3: Historia de usuario 1 — Consultar el menú (P1) 🎯 MVP

**Objetivo**: `GET /api/v1/dishes` (con filtro opcional `available`) y `GET /api/v1/dishes/{id}` devuelven los platos activos según el contrato.

**Prueba independiente**: con platos disponibles, no disponibles y eliminados insertados directamente en la base, pedir el listado con y sin filtro y el detalle de cada plato, y comprobar qué platos aparecen, en qué orden y con qué códigos.

### Pruebas (Red)

- [X] T014 [P] [US1] Crear `tests/MiRestaurante.Application.UnitTests/Dishes/DishMappingsTests.cs` (FR-007): `ToDto_Should_CopyAllPublicFields_When_DishIsMapped`: un `Dish` con los 9 campos rellenos (incluido `DeletedAt`) mapeado con `ToDto()` es `BeEquivalentTo` un `DishDto` con `Id`, `Name`, `Description`, `Price`, `ImageUrl`, `IsAvailable`, `CreatedAt` y `UpdatedAt` iguales (sin `DeletedAt`).
- [X] T015 [P] [US1] Crear `tests/MiRestaurante.Infrastructure.IntegrationTests/DishServiceTests.cs` (FR-003 a FR-006; research D4, D2): `public class DishServiceTests(MigratedDatabaseFixture database) : IClassFixture<MigratedDatabaseFixture>, IAsyncLifetime`, con `InitializeAsync` => `database.ResetAsync()` (cada prueba crea sus datos), un ayudante privado `AddDishesAsync(params Dish[] dishes)` que inserta con `database.CreateContext()`, un ayudante `NewDish(string name, bool isAvailable = true, DateTimeOffset? deletedAt = null)` con horas fijas, y un ayudante `CreateService()` que devuelve `new DishService(database.CreateContext())` (los cambios de constructor de historias posteriores solo tocan este ayudante). Pruebas:
  - `ListAsync_Should_ReturnActiveDishesOrderedByNameIgnoringCase_When_NoFilterIsGiven`: nombres insertados `banana`, `Zeta`, `ñame`, `Apple`, `cherry`, `nube`, `árbol` → nombres devueltos `Apple, árbol, banana, cherry, nube, ñame, Zeta` en orden estricto.
  - `ListAsync_Should_ReturnOnlyDishesWithThatAvailability_When_FilterIsGiven` (`[Theory]` `true`/`false`).
  - `ListAsync_Should_ExcludeDeletedDishes_When_FilterIsAnyValue` (`[Theory]` con `null`, `true`, `false`; el plato eliminado es disponible).
  - `ListAsync_Should_ReturnEmptyList_When_NoActiveDishesExist` (solo un plato eliminado).
  - `GetByIdAsync_Should_ReturnDish_When_DishIsActive` (`BeEquivalentTo` el `DishDto` esperado).
  - `GetByIdAsync_Should_ThrowNotFoundException_When_DishDoesNotExist` (`WithMessage("No existe el plato 999.")`).
  - `GetByIdAsync_Should_ThrowNotFoundException_When_DishIsDeleted`.
- [X] T016 [P] [US1] Crear `tests/MiRestaurante.Api.FunctionalTests/ListDishesTests.cs` (FR-004, FR-005, FR-007; contrato `GET /api/v1/dishes`; research D7): `IClassFixture<DevelopmentApiFactory>` e `IAsyncLifetime` con `InitializeAsync` => `factory.ResetDatabaseAsync()`; ayudante privado que inserta `Dish` mediante un ámbito de `factory.Services` (`ApplicationDbContext`). Pruebas:
  - `List_Should_ReturnOkWithDishesOrderedByName_When_NoFilterIsGiven` (nombres en el orden de T015).
  - `List_Should_ReturnOnlyMatchingDishes_When_AvailableFilterIsGiven` (`[Theory]` `true`, `True`, `TRUE`, `false`, `FALSE` con la disponibilidad esperada).
  - `List_Should_ReturnAllActiveDishes_When_AvailableFilterIsEmpty` (`?available=`).
  - `List_Should_NotIncludeDeletedDishes_When_DishIsDeleted`.
  - `List_Should_ReturnEmptyArray_When_NoActiveDishesExist` (cuerpo `[]`).
  - `List_Should_ReturnBadRequest_When_AvailableFilterIsInvalid` (`[Theory]` `si`, `1`): estado 400.
  - `List_Should_UseProblemJsonContentType_When_AvailableFilterIsInvalid` (`[Theory]` `si`, `1`): tipo de contenido `application/problem+json`.
  - `List_Should_ReportSpanishMessageUnderAvailableKey_When_AvailableFilterIsInvalid` (`[Theory]` `si`, `1`): `errors.available` = `["El valor indicado no es válido."]`.
- [X] T017 [P] [US1] Crear `tests/MiRestaurante.Api.FunctionalTests/GetDishTests.cs` (FR-006, FR-007; contrato `GET /api/v1/dishes/{id}`; research D8, D9): mismo andamiaje que T016. Pruebas:
  - `GetById_Should_ReturnOkWithDish_When_DishIsActive` (cuerpo equivalente al plato insertado).
  - `GetById_Should_ReturnExactlyEightProperties_When_DishIsActive`: los nombres de propiedad del objeto JSON son exactamente `id`, `name`, `description`, `price`, `imageUrl`, `isAvailable`, `createdAt`, `updatedAt` (sin `deletedAt`).
  - `GetById_Should_ReturnNullOptionalFields_When_DishHasNoDescriptionOrImage` (`description` e `imageUrl` son `null` explícito).
  - `GetById_Should_ReturnNotFound_When_DishDoesNotExistOrIsDeleted` (`[Theory]` id inexistente y id eliminado): estado 404.
  - `GetById_Should_UseSpanishDetail_When_DishDoesNotExistOrIsDeleted` (`[Theory]` id inexistente y id eliminado): `detail` «No existe el plato {id}.».
  - `GetById_Should_ReturnNotFound_When_IdIsNotAPositiveInteger` (`[Theory]` `abc`, `99999999999`, `0`, `-1`): estado 404.
  - `GetById_Should_UseProblemJsonContentType_When_IdIsNotAPositiveInteger` (`[Theory]` `abc`, `99999999999`, `0`, `-1`): tipo de contenido `application/problem+json`.
- [X] T018 [US1] Ejecutar `dotnet test --project tests/MiRestaurante.Application.UnitTests`, `--project tests/MiRestaurante.Infrastructure.IntegrationTests` y `--project tests/MiRestaurante.Api.FunctionalTests` y confirmar Red (compilación en las dos primeras; 404 de ruta inexistente en las funcionales).

### Implementación

- [X] T019 [P] [US1] Crear `src/MiRestaurante.Application/Dishes/DishDto.cs` (FR-007, research D9): `public sealed record DishDto(int Id, string Name, string? Description, decimal Price, string? ImageUrl, bool IsAvailable, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);` sin `DeletedAt`.
- [X] T020 [US1] Crear `src/MiRestaurante.Application/Dishes/DishMappings.cs` (depende de T019; principio IV): `public static class DishMappings` con `public static DishDto ToDto(this Dish dish)`, mapeo manual. Ejecutar `dotnet test --project tests/MiRestaurante.Application.UnitTests --filter-class "*DishMappingsTests"` → Green.
- [X] T021 [US1] Crear `src/MiRestaurante.Application/Dishes/IDishService.cs` (principio IV): `Task<IReadOnlyList<DishDto>> ListAsync(bool? available, CancellationToken cancellationToken)` y `Task<DishDto> GetByIdAsync(int id, CancellationToken cancellationToken)`.
- [X] T022 [US1] Crear `src/MiRestaurante.Application/Dishes/DishService.cs` (depende de T020 y T021; FR-003 a FR-006; research D4): `public sealed class DishService(IApplicationDbContext db) : IDishService` con la propiedad privada `ActiveDishes => db.Dishes.Where(d => d.DeletedAt == null)` (filtro explícito, sin `HasQueryFilter`); `ListAsync` filtra por `IsAvailable` solo si `available` no es `null`, ordena con `OrderBy(d => d.Name).ThenBy(d => d.Id)` y proyecta con `ToDto()`; método privado `FindActiveAsync(int id, CancellationToken)` que lanza `new NotFoundException($"No existe el plato {id}.")`; `GetByIdAsync` lo usa. Todo asíncrono, propagando el `CancellationToken`. Ejecutar `dotnet test --project tests/MiRestaurante.Infrastructure.IntegrationTests --filter-class "*DishServiceTests"` → Green.
- [X] T023 [P] [US1] Editar `src/MiRestaurante.Api/ErrorHandling/RequestBodyMessages.cs` (FR-005, research D7): añadir `public const string InvalidValue = "El valor indicado no es válido.";` (no repite el valor recibido).
- [X] T024 [US1] Editar `src/MiRestaurante.Api/Program.cs` (depende de T022 y T023; research D7, D10): dentro de `AddControllers(options => …)` añadir `options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => RequestBodyMessages.InvalidValue);` junto al mensaje de cuerpo ausente; y registrar `builder.Services.AddScoped<IDishService, DishService>();` (con `using MiRestaurante.Application.Dishes;`). No tocar el resto.
- [X] T025 [US1] Crear `src/MiRestaurante.Api/Controllers/DishesController.cs` (depende de T024; principio VI; contrato): `[ApiController]`, `[Route("api/v1/dishes")]`, `public sealed class DishesController(IDishService dishService) : ControllerBase`, sin lógica ni `try`/`catch`. Acciones: `[HttpGet] List([FromQuery] bool? available, CancellationToken)` → `Ok(...)` con `[ProducesResponseType<IReadOnlyList<DishDto>>(200)]` y `[ProducesResponseType<ValidationProblemDetails>(400)]`; `[HttpGet("{id:int}")] GetById(int id, CancellationToken)` con `[ProducesResponseType<DishDto>(200)]` y `[ProducesResponseType<ProblemDetails>(404)]`. Solo depende de `IDishService` y `DishDto`.
- [X] T026 [US1] Ejecutar `dotnet test --solution MiRestaurante.slnx` y confirmar Green de T014 a T017 y de las pruebas de arquitectura y convenciones existentes sin cambios (FR-027).

**Punto de control**: el menú se consulta por la API (MVP). Con la base de Development, `GET /api/v1/dishes` ya devuelve los 5 platos semilla.

## Fase 4: Historia de usuario 2 — Dar de alta un plato (P1)

**Objetivo**: `POST /api/v1/dishes` valida la entrada (400 con mensajes en español), recorta los textos, fija las fechas con `TimeProvider`, garantiza la unicidad del nombre entre los activos (422, también ante peticiones simultáneas) y responde 201 con `Location`.

**Prueba independiente**: crear un plato válido y comprobar la respuesta, `Location` y el listado posterior; repetir con datos inválidos y con un nombre duplicado.

**Dependencia**: `DishService`, `IDishService` y `DishesController` de US1.

### Pruebas (Red)

- [X] T027 [P] [US2] Crear `tests/MiRestaurante.Application.UnitTests/Validation/MaxDecimalPlacesAttributeTests.cs` (FR-011, research D6): con `new MaxDecimalPlacesAttribute(2)` y los valores escritos como `string` en `[InlineData]` y convertidos con `decimal.Parse(value, CultureInfo.InvariantCulture)`: `IsValid_Should_ReturnTrue_When_ValueHasAtMostTwoSignificantDecimals` (`1000.55`, `1000.550`, `8000.000`, `0.01`, `99999999.99`); `IsValid_Should_ReturnFalse_When_ValueHasMoreThanTwoSignificantDecimals` (`1000.555`, `0.001`, `-1.555`); `IsValid_Should_ReturnTrue_When_ValueIsNotDecimal` (`null` y una cadena).
- [X] T028 [P] [US2] Crear `tests/MiRestaurante.Application.UnitTests/Validation/HttpUrlAttributeTests.cs` (FR-012, FR-014, research D6): `IsValid_Should_ReturnTrue_When_ValueIsAbsoluteHttpOrHttpsUrl` (`http://example.com/a.jpg`, `https://example.com/a.jpg`, `  https://example.com/a.jpg  `); `IsValid_Should_ReturnTrue_When_ValueIsNullOrWhiteSpace` (`null`, `""`, `"   "`); `IsValid_Should_ReturnFalse_When_ValueIsNotAbsoluteHttpOrHttpsUrl` (`ftp://example.com/a.jpg`, `javascript:alert(1)`, `file:///etc/passwd`, `data:image/png;base64,AAAA`, `/images/a.jpg`, `no es una url`).
- [X] T029 [P] [US2] Crear `tests/MiRestaurante.Application.UnitTests/Dishes/SaveDishRequestValidationTests.cs` (FR-008 a FR-013; contrato «Mensajes de validación»): ayudante `Validate(SaveDishRequest request)` con `Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true)` que devuelve los `ValidationResult`; ayudante `ValidRequest()` (nombre, descripción, `32000m`, URL `https`, `IsAvailable = true`) modificado con `with`. Cada prueba comprueba el mensaje exacto y el miembro (`Name`, `Description`, `Price`, `ImageUrl`, `IsAvailable`):
  - `Validate_Should_ReturnNoErrors_When_RequestIsValid`.
  - `Validate_Should_RequireName_When_NameIsMissingOrBlank` (`null`, `""`, `"   "`) → «El nombre es obligatorio.».
  - `Validate_Should_RejectName_When_ReceivedValueExceedsHundredCharacters` (101 caracteres; 100 caracteres con un espacio a cada lado, 102 en total) → «El nombre no puede superar los 100 caracteres.».
  - `Validate_Should_AcceptName_When_NameHasExactlyHundredCharacters`.
  - `Validate_Should_RejectDescription_When_ReceivedValueExceedsFiveHundredCharacters` (501) → «La descripción no puede superar los 500 caracteres.».
  - `Validate_Should_RequirePrice_When_PriceIsMissing` → «El precio es obligatorio.».
  - `Validate_Should_RejectPrice_When_PriceIsOutOfRange` (`0`, `-1`, `100000000`) → «El precio debe ser mayor que 0 y menor o igual que 99.999.999,99.».
  - `Validate_Should_AcceptPrice_When_PriceIsWithinLimits` (`0.01`, `99999999.99`, `1000.550`, `8000.000`).
  - `Validate_Should_RejectPrice_When_PriceHasMoreThanTwoSignificantDecimals` (`1000.555`) → «El precio admite como máximo 2 decimales.».
  - `Validate_Should_ReturnOneMessagePerRule_When_PriceBreaksRangeAndDecimals` (`-1.555` → los dos mensajes de `Price`).
  - `Validate_Should_RejectImageUrl_When_ReceivedValueExceedsMaximumLength` (2049 caracteres) → «La URL de la imagen no puede superar los 2048 caracteres.».
  - `Validate_Should_RejectImageUrl_When_UrlIsNotAbsoluteHttpOrHttps` (`ftp://x/y`, `javascript:alert(1)`, `file:///etc/passwd`, `data:text/plain,hola`, `/img.jpg`) → «La URL de la imagen debe ser una URL absoluta http o https.».
  - `Validate_Should_AcceptImageUrl_When_ValidUrlHasSurroundingSpaces`.
  - `Validate_Should_RequireAvailability_When_IsAvailableIsMissing` → «La disponibilidad es obligatoria.».
- [X] T030 [P] [US2] Crear `tests/MiRestaurante.Infrastructure.IntegrationTests/InsertDuplicateInterceptor.cs` (FR-017, research D12): `public sealed class InsertDuplicateInterceptor(MigratedDatabaseFixture database, string name) : SaveChangesInterceptor` que, en `SavingChangesAsync`, inserta y guarda con **otro** contexto (`database.CreateContext()`, sin interceptores) un `Dish` activo llamado `name` con horas fijas, y después devuelve `base.SavingChangesAsync(...)`. Provoca de forma determinista la carrera entre la comprobación del servicio y su guardado.
- [X] T031 [US2] Editar `tests/MiRestaurante.Infrastructure.IntegrationTests/DishServiceTests.cs` (depende de T030; FR-002, FR-014 a FR-018; research D3, D10): añadir un campo `Mock<TimeProvider>` con `Setup(t => t.GetUtcNow()).Returns(Now)` (`Now` = `new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.Zero)`), cambiar el ayudante `CreateService(params IInterceptor[] interceptors)` a `new DishService(database.CreateContext(interceptors), timeProvider.Object)` y un ayudante `Request(string name, ...)` que construye `SaveDishRequest`. Pruebas (los datos guardados se leen con un contexto nuevo):
  - `CreateAsync_Should_SetCreatedAndUpdatedAtToCurrentTime_When_RequestIsValid` (ambas = `Now`).
  - `CreateAsync_Should_PersistDishWithoutDeletedAt_When_RequestIsValid` (el registro guardado es equivalente al `DishDto` devuelto y `DeletedAt` es `null`; una aserción de equivalencia sobre el registro).
  - `CreateAsync_Should_TrimTextFields_When_ValuesHaveSurroundingSpaces` (nombre, descripción y URL recortados).
  - `CreateAsync_Should_StoreNull_When_OptionalTextIsEmptyOrWhiteSpace` (`[Theory]` `null`, `""`, `"   "` para descripción y URL).
  - `CreateAsync_Should_ThrowBusinessRuleException_When_ActiveDishHasSameNameIgnoringCase` (`[Theory]` `ajiaco`, `" AJIACO "` frente a «Ajiaco»): `WithMessage` «Ya existe un plato con el nombre «{nombre recortado}».».
  - `CreateAsync_Should_NotCreateDish_When_NameIsDuplicated` (tras la excepción, la tabla solo contiene el plato original).
  - `CreateAsync_Should_CreateDish_When_SameNameBelongsToDeletedDish`.
  - `CreateAsync_Should_CreateDish_When_NameDiffersOnlyInAccents` («Ajíaco» frente a «Ajiaco»).
  - `CreateAsync_Should_ThrowBusinessRuleException_When_DuplicateIsInsertedConcurrently` (servicio con `new InsertDuplicateInterceptor(database, "Ajiaco")` y petición «Ajiaco»; cubre la rama `catch`).
  - `CreateAsync_Should_RethrowDbUpdateException_When_FailureIsNotDuplicateName` (petición con nombre de 101 caracteres enviada directamente al servicio: la columna `varchar(100)` la rechaza).
- [X] T032 [P] [US2] Crear `tests/MiRestaurante.Api.FunctionalTests/CreateDishTests.cs` (FR-008 a FR-018; SC-003; contrato `POST`; research D9, D12): mismo andamiaje que T016 (reinicio con `ResetDatabaseAsync()`). Pruebas:
  - `Create_Should_ReturnCreated_When_RequestIsValid` (201).
  - `Create_Should_ReturnLocationOfNewDish_When_RequestIsValid` (`Location.AbsolutePath` = `/api/v1/dishes/{id del cuerpo}`).
  - `Create_Should_ReturnEqualTimestamps_When_RequestIsValid` (`createdAt` = `updatedAt`).
  - `Create_Should_ReturnTrimmedName_When_NameHasSurroundingSpaces`.
  - `Create_Should_ReturnNullDescription_When_DescriptionIsWhiteSpace` (se envió `" "`).
  - `Create_Should_ReturnCreated_When_PriceHasTrailingZeros` (`1000.550`).
  - `Create_Should_IgnoreUnknownFields_When_BodyHasExtraProperties` (estado 201 con una propiedad extra en el cuerpo).
  - `Create_Should_ReturnBadRequestWithSpanishMessage_When_FieldIsInvalid` (`[Theory]` con el JSON del cuerpo, la clave y el mensaje esperados: nombre en blanco, nombre de 101 caracteres, precio `0`, precio `1000.555`, `imageUrl` `ftp://x/y`, sin `isAvailable`, sin `price`): `errors[clave]` contiene el mensaje del contrato.
  - `Create_Should_NotCreateDish_When_RequestIsInvalid` (tras el 400, `GET /api/v1/dishes` devuelve `[]`).
  - `Create_Should_ReturnBadRequest_When_BodyIsMissing` (estado 400).
  - `Create_Should_UseProblemJsonContentType_When_BodyIsMissing` (`application/problem+json`).
  - `Create_Should_ReturnBadRequest_When_JsonIsMalformed` (`{"name":`; estado 400).
  - `Create_Should_UseProblemJsonContentType_When_JsonIsMalformed` (`application/problem+json`).
  - `Create_Should_ReturnUnprocessableEntity_When_NameIsInUse` (estado 422 tras crear «Ajiaco» y enviar `" AJIACO "`).
  - `Create_Should_UseSpanishDetail_When_NameIsInUse` (`detail` «Ya existe un plato con el nombre «AJIACO».»).
  - `Create_Should_AcceptOnlyOneRequest_When_TwentyRequestsUseSameNameConcurrently` (`Enumerable.Repeat` de 20 peticiones con el mismo nombre en distintas mayúsculas y `Task.WhenAll`; aserción única: códigos ordenados = `[201, 422 × 19]`).
- [X] T033 [US2] Ejecutar las tres suites afectadas (`Application.UnitTests`, `Infrastructure.IntegrationTests`, `Api.FunctionalTests`) y confirmar Red (compilación por falta de `SaveDishRequest`, atributos y `CreateAsync`; 405 en las funcionales).

### Implementación

- [X] T034 [P] [US2] Crear `src/MiRestaurante.Application/Validation/MaxDecimalPlacesAttribute.cs` (FR-011, research D6): `public sealed class MaxDecimalPlacesAttribute(int decimals) : ValidationAttribute`; `IsValid(object? value)` devuelve `true` si el valor no es `decimal`, o si `decimal.Round(value, decimals) == value` (la igualdad de `decimal` ignora la escala: `1000.550` es válido y `1000.555` no). Ejecutar `MaxDecimalPlacesAttributeTests` → Green.
- [X] T035 [P] [US2] Crear `src/MiRestaurante.Application/Validation/HttpUrlAttribute.cs` (FR-012, research D6): `public sealed class HttpUrlAttribute : ValidationAttribute`; válido si el valor es `null` o solo tiene espacios, o si, recortado, `Uri.TryCreate(..., UriKind.Absolute, out var uri)` tiene éxito con `uri.Scheme` `http` o `https`. Ejecutar `HttpUrlAttributeTests` → Green.
- [X] T036 [US2] Crear `src/MiRestaurante.Application/Dishes/SaveDishRequest.cs` (depende de T034 y T035; FR-008 a FR-013; research D6): `public sealed record SaveDishRequest` (no posicional; propiedades `{ get; init; }` anulables para que `[Required]` detecte la ausencia, sin valores por defecto), con estos atributos y `ErrorMessage` literales:
  - `string? Name`: `[Required(ErrorMessage = "El nombre es obligatorio.")]`, `[MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]`.
  - `string? Description`: `[MaxLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]`.
  - `decimal? Price`: `[Required(ErrorMessage = "El precio es obligatorio.")]`, `[Range(typeof(decimal), "0", "99999999.99", MinimumIsExclusive = true, ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El precio debe ser mayor que 0 y menor o igual que 99.999.999,99.")]`, `[MaxDecimalPlaces(2, ErrorMessage = "El precio admite como máximo 2 decimales.")]`.
  - `string? ImageUrl`: `[MaxLength(2048, ErrorMessage = "La URL de la imagen no puede superar los 2048 caracteres.")]`, `[HttpUrl(ErrorMessage = "La URL de la imagen debe ser una URL absoluta http o https.")]`.
  - `bool? IsAvailable`: `[Required(ErrorMessage = "La disponibilidad es obligatoria.")]`.

  Ejecutar `SaveDishRequestValidationTests` → Green.
- [X] T037 [US2] Editar `src/MiRestaurante.Application/Dishes/IDishService.cs`: añadir `Task<DishDto> CreateAsync(SaveDishRequest request, CancellationToken cancellationToken)`.
- [X] T038 [US2] Editar `src/MiRestaurante.Application/Dishes/DishService.cs` (depende de T036 y T037; FR-002, FR-014 a FR-018; research D3, D10): constructor primario `DishService(IApplicationDbContext db, TimeProvider timeProvider)`; una sola llamada a `timeProvider.GetUtcNow()` por operación. Métodos privados: `Apply(Dish dish, SaveDishRequest request, DateTimeOffset now)` (copia los cinco campos; `Name.Trim()`; descripción y URL vacías o solo con espacios → `null`, en otro caso recortadas; fija `UpdatedAt = now`); `NameInUseAsync(string name, int? excludedId, CancellationToken)` (`AnyAsync` sobre `ActiveDishes` con `d.Name == name` y distinto Id; la colación de la columna resuelve las mayúsculas); `SaveAsync(Dish dish, int? excludedId, CancellationToken)`: comprueba `NameInUseAsync` y lanza `new BusinessRuleException($"Ya existe un plato con el nombre «{dish.Name}».")`; después `SaveChangesAsync` dentro de `try`; ante `DbUpdateException`, repite la comprobación y lanza la `BusinessRuleException` si el nombre ya está en uso o relanza con `throw;` en otro caso. `CreateAsync`: nuevo `Dish`, `Apply`, `CreatedAt = UpdatedAt = now`, `db.Dishes.Add`, `SaveAsync`, devuelve `ToDto()`. Ejecutar `DishServiceTests` → Green (también las de US1).
- [X] T039 [US2] Editar `src/MiRestaurante.Api/Controllers/DishesController.cs` (FR-018; principio VI): `[HttpPost] Create(SaveDishRequest request, CancellationToken)` → `CreatedAtAction(nameof(GetById), new { id = dish.Id }, dish)`, con `[ProducesResponseType<DishDto>(201)]`, `[ProducesResponseType<ValidationProblemDetails>(400)]` y `[ProducesResponseType<ProblemDetails>(422)]`. Sin `try`/`catch`.
- [X] T040 [US2] Ejecutar `dotnet test --solution MiRestaurante.slnx` y confirmar Green de T027 a T032 y de todas las anteriores.

**Punto de control**: las historias P1 están completas; el menú se consulta y se amplía por la API.

## Fase 5: Historia de usuario 3 — Modificar un plato (P2)

**Objetivo**: `PUT /api/v1/dishes/{id}` reemplaza el plato con el mismo cuerpo que `POST`, fija `UpdatedAt`, conserva `CreatedAt` y evalúa en orden 400 → 404 → 422.

**Prueba independiente**: crear un plato, reemplazarlo y comprobar la respuesta y la consulta posterior; repetir con datos inválidos, con el nombre de otro plato y con un plato inexistente.

**Dependencia**: `SaveDishRequest`, `Apply`, `SaveAsync` y `InsertDuplicateInterceptor` de US2.

### Pruebas (Red)

- [X] T041 [US3] Editar `tests/MiRestaurante.Infrastructure.IntegrationTests/DishServiceTests.cs` (FR-016, FR-017, FR-019, FR-021): el plato de partida se inserta con `CreatedAt` = `UpdatedAt` anteriores a `Now`. Pruebas:
  - `UpdateAsync_Should_ReplaceAllFields_When_RequestIsValid` (registro guardado con los cinco campos nuevos, recortados).
  - `UpdateAsync_Should_SetUpdatedAtAndKeepCreatedAt_When_RequestIsValid`.
  - `UpdateAsync_Should_SetUpdatedAtToCurrentTime_When_DataIsUnchanged`.
  - `UpdateAsync_Should_AllowRename_When_OnlyCaseOfOwnNameChanges` («Ajiaco» → «AJIACO»; queda «AJIACO»).
  - `UpdateAsync_Should_ThrowBusinessRuleException_When_OtherActiveDishHasSameNameIgnoringCase`.
  - `UpdateAsync_Should_LeaveBothDishesUnchanged_When_NameIsDuplicated`.
  - `UpdateAsync_Should_ThrowNotFoundException_When_DishDoesNotExistOrIsDeleted` (`[Theory]`), aunque el nombre enviado coincida con otro plato activo (404 antes que 422).
  - `UpdateAsync_Should_ThrowBusinessRuleException_When_DuplicateIsInsertedConcurrently` (con `InsertDuplicateInterceptor`).
  - `UpdateAsync_Should_KeepLastWrite_When_TwoUpdatesAreAppliedInSequence` (FR-022: dos servicios actualizan el mismo plato con datos distintos y gana el segundo; sin control de concurrencia).
- [X] T042 [P] [US3] Crear `tests/MiRestaurante.Api.FunctionalTests/UpdateDishTests.cs` (FR-019, FR-021; contrato `PUT`): mismo andamiaje que T032; los platos se crean con `POST`. Pruebas:
  - `Update_Should_ReturnOkWithUpdatedDish_When_RequestIsValid` (cuerpo equivalente al plato reemplazado).
  - `Update_Should_KeepCreatedAt_When_RequestIsValid`.
  - `Update_Should_NotMoveUpdatedAtBackwards_When_RequestIsValid` (`updatedAt` posterior o igual al original).
  - `Update_Should_ReturnBadRequest_When_RequestIsInvalid` y `Update_Should_NotModifyDish_When_RequestIsInvalid` (un `GET` posterior devuelve el plato original).
  - `Update_Should_ReturnBadRequest_When_DishDoesNotExistAndRequestIsInvalid` (id 999 con precio `0`: 400, no 404; FR-021).
  - `Update_Should_ReturnBadRequest_When_BodyIsMissing` y `Update_Should_ReturnBadRequest_When_JsonIsMalformed` (estado 400).
  - `Update_Should_UseProblemJsonContentType_When_BodyIsMissingOrJsonIsMalformed` (`[Theory]`: `application/problem+json`).
  - `Update_Should_ReturnNotFound_When_DishDoesNotExistOrIsDeleted` (`[Theory]`: estado 404) y `Update_Should_UseSpanishDetail_When_DishDoesNotExistOrIsDeleted` (`[Theory]`: `detail`).
  - `Update_Should_ReturnNotFound_When_DishDoesNotExistAndNameIsInUse` (404 antes que 422).
  - `Update_Should_ReturnNotFound_When_IdIsNotAPositiveInteger` (`[Theory]` `abc`, `0`, `-1`).
  - `Update_Should_ReturnUnprocessableEntity_When_NameBelongsToOtherActiveDish` (estado 422) y `Update_Should_UseSpanishDetail_When_NameBelongsToOtherActiveDish` (`detail`).
  - `Update_Should_ExcludeDishFromAvailableList_When_DishIsMarkedUnavailable` (US3 escenario 7).
- [X] T043 [US3] Ejecutar `Infrastructure.IntegrationTests` y `Api.FunctionalTests` y confirmar Red (compilación por falta de `UpdateAsync`; 405 en las funcionales).

### Implementación

- [X] T044 [US3] Editar `src/MiRestaurante.Application/Dishes/IDishService.cs`: añadir `Task<DishDto> UpdateAsync(int id, SaveDishRequest request, CancellationToken cancellationToken)`.
- [X] T045 [US3] Editar `src/MiRestaurante.Application/Dishes/DishService.cs` (FR-019, FR-021, FR-022): `UpdateAsync`: `FindActiveAsync(id)` primero (404), luego `Apply(dish, request, now)` (no toca `CreatedAt`) y `SaveAsync(dish, excludedId: dish.Id, ...)` (422); devuelve `ToDto()`. Sin control de concurrencia optimista (gana la última escritura).
- [X] T046 [US3] Editar `src/MiRestaurante.Api/Controllers/DishesController.cs`: `[HttpPut("{id:int}")] Update(int id, SaveDishRequest request, CancellationToken)` → `Ok(dish)`, con `[ProducesResponseType<DishDto>(200)]`, `[ProducesResponseType<ValidationProblemDetails>(400)]`, `[ProducesResponseType<ProblemDetails>(404)]` y `[ProducesResponseType<ProblemDetails>(422)]`.
- [X] T047 [US3] Ejecutar `dotnet test --solution MiRestaurante.slnx` y confirmar Green.

**Punto de control**: los platos se pueden corregir y marcar como no disponibles.

## Fase 6: Historia de usuario 4 — Retirar un plato del menú (P2)

**Objetivo**: `DELETE /api/v1/dishes/{id}` hace un borrado lógico (fija `DeletedAt`) y responde 204; un plato eliminado es invisible (404) y no tiene restauración.

**Prueba independiente**: crear un plato, eliminarlo y comprobar el 204, que ya no aparece en ninguna consulta y que su registro y su fecha de eliminación se conservan en la base de datos.

**Dependencia**: `FindActiveAsync` de US1. Las pruebas funcionales crean los platos con `POST` (US2).

### Pruebas (Red)

- [X] T048 [US4] Editar `tests/MiRestaurante.Infrastructure.IntegrationTests/DishServiceTests.cs` (FR-020, FR-021, FR-023; SC-005):
  - `DeleteAsync_Should_SetDeletedAtToCurrentTime_When_DishIsActive`.
  - `DeleteAsync_Should_KeepRecordAndOtherFields_When_DishIsDeleted` (el registro, leído con un contexto nuevo, conserva todos sus demás valores).
  - `DeleteAsync_Should_ThrowNotFoundException_When_DishDoesNotExistOrIsDeleted` (`[Theory]`).
- [X] T049 [P] [US4] Crear `tests/MiRestaurante.Api.FunctionalTests/DeleteDishTests.cs` (FR-020, FR-021; SC-002, SC-004, SC-005; contrato `DELETE`): mismo andamiaje que T032. Pruebas:
  - `Delete_Should_ReturnNoContent_When_DishIsActive` (estado 204).
  - `Delete_Should_ReturnEmptyBody_When_DishIsActive` (cuerpo vacío).
  - `Delete_Should_ReturnNotFound_When_DishIsDeletedTwice`.
  - `Delete_Should_ReturnNotFound_When_DishDoesNotExist`.
  - Borrado hecho a través de la API (a diferencia de las pruebas de US1 y US3, que insertan el plato eliminado directamente): `Delete_Should_ReturnNotFoundOnGet_When_DishWasDeletedThroughApi`, `Delete_Should_ReturnNotFoundOnPut_When_DishWasDeletedThroughApi` y `Delete_Should_HideDishFromList_When_DishWasDeletedThroughApi`.
  - `Delete_Should_KeepRecordWithDeletedAt_When_DishIsDeleted` (consulta mediante un ámbito de `factory.Services`: la fila existe con `DeletedAt` no nulo).
  - `Delete_Should_CompleteCreateReadUpdateDeleteInFourRequests_When_DishIsManaged` (SC-002: códigos de las 4 peticiones = `[201, 200, 200, 204]`).
- [X] T050 [US4] Ejecutar `Infrastructure.IntegrationTests` y `Api.FunctionalTests` y confirmar Red (compilación por falta de `DeleteAsync`; 405 en las funcionales).

### Implementación

- [X] T051 [US4] Editar `src/MiRestaurante.Application/Dishes/IDishService.cs`: añadir `Task DeleteAsync(int id, CancellationToken cancellationToken)`.
- [X] T052 [US4] Editar `src/MiRestaurante.Application/Dishes/DishService.cs` (FR-020, FR-023): `DeleteAsync`: `FindActiveAsync(id)`, `dish.DeletedAt = timeProvider.GetUtcNow()` y `SaveChangesAsync`. Sin borrado físico ni método de restauración.
- [X] T053 [US4] Editar `src/MiRestaurante.Api/Controllers/DishesController.cs`: `[HttpDelete("{id:int}")] Delete(int id, CancellationToken)` → `NoContent()`, con `[ProducesResponseType(204)]` y `[ProducesResponseType<ProblemDetails>(404)]`.
- [X] T054 [US4] Ejecutar `dotnet test --solution MiRestaurante.slnx` y confirmar Green.

**Punto de control**: el ciclo de vida completo del plato está disponible (US1 a US4).

## Fase 7: Historia de usuario 5 — Menú inicial de ejemplo (P3)

**Objetivo**: la base recién migrada contiene los 5 platos semilla, la API los sirve sin pasos manuales y el primer plato creado no choca con sus identificadores.

**Prueba independiente**: sobre una base recién migrada, comprobar los 5 platos y que el primer alta recibe el Id 6.

**Dependencia**: la semilla y su prueba en Red se hicieron en los Fundamentos (T005 a T010). Esta fase añade pruebas de caracterización y exige verlas en Red por mutación (principio VII); necesita `ListAsync` (US1) y `CreateAsync` (US2).

- [X] T055 [US5] Editar `tests/MiRestaurante.Infrastructure.IntegrationTests/DishSeedTests.cs` (FR-025; US5 escenario 2; research D5): `CreateAsync_Should_AssignIdAfterSeed_When_DatabaseIsFreshlyMigrated`: sobre una base nueva y migrada (sin Respawn), `new DishService(context, timeProvider)` con un `Mock<TimeProvider>` crea un plato válido y el `Id` devuelto es `6`.

  Red por mutación: quitar de forma temporal de `src/MiRestaurante.Infrastructure/Migrations/<ts>_AddDishes.cs` la instrucción `setval` que genera Npgsql tras los `InsertData`, ejecutar `DishSeedTests` y confirmar que esta prueba falla por colisión de clave primaria. Revertir con `git checkout -- src/MiRestaurante.Infrastructure/Migrations` y confirmar Green.
- [X] T056 [P] [US5] Crear `tests/MiRestaurante.Api.FunctionalTests/DishSeedApiTests.cs` (SC-001): `IClassFixture<DevelopmentApiFactory>` propia y de solo lectura, **sin** `ResetDatabaseAsync()`. Pruebas: `List_Should_ReturnFiveSeedDishes_When_DatabaseIsFreshlyMigrated` (nombres en orden «Ajiaco santafereño», «Arepa de huevo», «Bandeja paisa», «Lechona», «Sancocho de hueso») y `List_Should_ReturnFourDishes_When_FilteringAvailableOnFreshDatabase` (sin «Lechona»).

  Red por mutación: marcar de forma temporal la lechona como disponible en la migración `AddDishes` (`InsertData`), ejecutar la clase y confirmar que `List_Should_ReturnFourDishes_When_FilteringAvailableOnFreshDatabase` falla. Revertir con `git checkout -- src/MiRestaurante.Infrastructure/Migrations` y confirmar Green.
- [X] T057 [US5] Ejecutar `dotnet test --project tests/MiRestaurante.Infrastructure.IntegrationTests --filter-class "*DishSeedTests"` y `dotnet test --project tests/MiRestaurante.Api.FunctionalTests --filter-class "*DishSeedApiTests"` y confirmar Green. Cada prueba se vio en Red por mutación (T055 y T056) y vuelve a Green tras revertir. Si alguna falla tras revertir, corregir `DishConfiguration` o la migración, nunca la prueba.

**Punto de control**: todas las historias de usuario están completas.

## Fase final: Pulido y validación transversal

**Propósito**: reglas de arquitectura nuevas, documentación de uso y puertas de calidad.

- [X] T058 [P] Crear `tests/MiRestaurante.Architecture.Tests/ControllerServiceDependencyTests.cs` (principios II y IV; research D13), con el estilo de `ControllerDataAccessTests`: `Controllers_Should_NotDependOnConcreteServices_When_HandlingRequests` (tipos que heredan de `ControllerBase` en `Layers.Load(Layers.Api)` no tienen `HaveDependencyOnAny` sobre los nombres completos de las clases concretas de `Layers.Load(Layers.Application)` cuyo nombre termina en `Service`, obtenidos por reflexión) y `Controllers_Should_NotDependOnDomain_When_HandlingRequests` (`HaveDependencyOnAny(Layers.Domain)`). Una sola aserción cada una: `result.FailingTypes.Should().BeNullOrEmpty(...)`. No modificar `LayerDependencyTests.cs`, `ControllerDataAccessTests.cs` ni `ControllerConventionTests.cs`.
- [X] T059 Demostrar Red de T058 (quickstart §8): crear temporalmente en `src/MiRestaurante.Api/Controllers/` un controlador con `[ApiController]`, ruta `api/v1/...` y `[ProducesResponseType]` que reciba `DishService` (clase concreta) y devuelva un `Dish`; ejecutar `dotnet test --project tests/MiRestaurante.Architecture.Tests --filter-class "*ControllerServiceDependencyTests"` y confirmar que fallan las dos y nombran el controlador. Borrar el controlador, repetir (Green) y comprobar con `git status` que no queda rastro.
- [X] T060 [P] Editar `src/MiRestaurante.Api/MiRestaurante.Api.http`: peticiones de ejemplo de las cinco operaciones (`GET` listado con y sin `?available=true`, `GET` por id, `POST`, `PUT` y `DELETE`) con cuerpos válidos del contrato.
- [X] T061 Ejecutar `dotnet format MiRestaurante.slnx` y luego `dotnet format --verify-no-changes MiRestaurante.slnx`: sin cambios pendientes, incluidos los archivos generados de `src/MiRestaurante.Infrastructure/Migrations/` (SC-006).
- [X] T062 Con `docker compose down` ejecutado, `dotnet build MiRestaurante.slnx` (0 advertencias, 0 errores) y `dotnet test --solution MiRestaurante.slnx` (código de salida 0); al terminar, `docker ps --filter ancestor=postgres:18` no lista contenedores (quickstart §9; FR-027, FR-031, SC-006).
- [X] T063 Ejecutar la puerta de cobertura de `CLAUDE.md` (`dotnet tool restore`, `rm -rf TestResults`, `dotnet test ... --coverlet --coverlet-output-format cobertura`, ReportGenerator con `minimumCoverageThresholds:lineCoverage=80`) e informar el resultado de `TestResults/report/Summary.txt`. **Obligatoria y sin exclusiones** (FR-030): ReportGenerator DEBE terminar con código 0 (el prototipo midió 98,1 %); si no, añadir pruebas antes de integrar. `git status` no muestra `TestResults/`.
- [X] T064 Revisión estática (principios III, IV, V, VI; FR-023): `grep -rn "try\|catch" src/MiRestaurante.Api/Controllers/` sin coincidencias; `grep -rn "DateTime.Now\|DateTime.UtcNow\|DateTimeOffset.Now\|DateTimeOffset.UtcNow" src/` sin coincidencias; ninguna referencia a `HasQueryFilter`, `migrationBuilder.Sql`, `UseInMemoryDatabase`, SQLite, AutoMapper ni MediatR; ningún endpoint ni método de restauración; `DishesController` sin dependencias de `MiRestaurante.Domain` ni de `IApplicationDbContext`.
- [X] T065 Ejecutar los escenarios §2 a §7 de `specs/004-dishes-crud/quickstart.md` (menú inicial, consultas y filtro, ciclo completo en 4 peticiones con comprobación de la fila en `psql`, reglas de negocio y validación, documento OpenAPI con `/api/v1/dishes` y `/api/v1/dishes/{id}`, y script idempotente con `CREATE COLLATION`, `CREATE UNIQUE INDEX ... WHERE "DeletedAt" IS NULL`, los 5 `INSERT` y el `setval`) y confirmar los resultados esperados. Al terminar, `docker compose down -v` si se quiere volver al menú inicial y comprobar con `git status` que no quedan archivos temporales.

## Dependencias y orden de ejecución

### Dependencias entre fases

- **Preparación (Fase 1)**: sin dependencias.
- **Fundamentos (Fase 2)**: depende de la Preparación y **bloquea todas las historias**.
- **US1 (P1)**: depende de los Fundamentos. Es el MVP.
- **US2 (P1)**: depende de US1 (`DishService`, `IDishService`, `DishesController`, ayudantes de `DishServiceTests`).
- **US3 (P2)**: depende de US2 (`SaveDishRequest`, `Apply`, `SaveAsync`, `InsertDuplicateInterceptor`).
- **US4 (P2)**: depende de US1 (`FindActiveAsync`); sus pruebas funcionales crean platos con `POST` (US2) y la del ciclo completo usa `PUT` (US3).
- **US5 (P3)**: la semilla está en los Fundamentos; sus pruebas restantes dependen de US1 (listado) y US2 (`CreateAsync`).
- **Pulido**: T058 y T059 dependen de US1 (`DishService` y `DishesController` existen); el resto, de todas las historias.

### Dentro de cada historia

- Las pruebas se escriben y se ejecutan en Red antes de implementar.
- Orden de implementación: DTOs y atributos → interfaz → servicio → controlador.
- Archivos compartidos entre historias, que NO son paralelizables entre sí (aplicar en orden de fase): `src/MiRestaurante.Application/Dishes/IDishService.cs` (T021, T037, T044, T051), `src/MiRestaurante.Application/Dishes/DishService.cs` (T022, T038, T045, T052), `src/MiRestaurante.Api/Controllers/DishesController.cs` (T025, T039, T046, T053), `tests/MiRestaurante.Infrastructure.IntegrationTests/DishServiceTests.cs` (T015, T031, T041, T048), `tests/MiRestaurante.Infrastructure.IntegrationTests/DishSeedTests.cs` (T005, T055) y `src/MiRestaurante.Api/Program.cs` (T024).
- Docker en marcha es requisito de T005 en adelante para las suites de integración y funcionales.

### Oportunidades de paralelismo

- Preparación: T002 y T003 (csproj distintos).
- Fundamentos: T007 y T008 (tras T006); T011 y T012 (tras T010).
- US1: T014 a T017 (pruebas en archivos distintos); T019 y T023.
- US2: T027 a T030 y T032 (pruebas e interceptor); T034 y T035 (atributos).
- US3: T042 en paralelo con T041.
- US4: T049 en paralelo con T048.
- US5: T056 en paralelo con T055.
- Pulido: T058 y T060.

## Ejemplos de ejecución en paralelo

```text
# Fundamentos (tras T006):
Tarea: "DbSet<Dish> Dishes en src/MiRestaurante.Application/IApplicationDbContext.cs"           (T007)
Tarea: "DishConfiguration en src/MiRestaurante.Infrastructure/Configurations/"                   (T008)

# US1, pruebas:
Tarea: "DishMappingsTests en tests/MiRestaurante.Application.UnitTests/Dishes/"                  (T014)
Tarea: "DishServiceTests (listado y detalle) en tests/MiRestaurante.Infrastructure.IntegrationTests/" (T015)
Tarea: "ListDishesTests en tests/MiRestaurante.Api.FunctionalTests/"                             (T016)
Tarea: "GetDishTests en tests/MiRestaurante.Api.FunctionalTests/"                                (T017)

# US2, pruebas:
Tarea: "MaxDecimalPlacesAttributeTests en tests/MiRestaurante.Application.UnitTests/Validation/"  (T027)
Tarea: "HttpUrlAttributeTests en tests/MiRestaurante.Application.UnitTests/Validation/"          (T028)
Tarea: "SaveDishRequestValidationTests en tests/MiRestaurante.Application.UnitTests/Dishes/"     (T029)
Tarea: "InsertDuplicateInterceptor en tests/MiRestaurante.Infrastructure.IntegrationTests/"      (T030)
Tarea: "CreateDishTests en tests/MiRestaurante.Api.FunctionalTests/"                             (T032)

# US2, implementación:
Tarea: "MaxDecimalPlacesAttribute en src/MiRestaurante.Application/Validation/"                  (T034)
Tarea: "HttpUrlAttribute en src/MiRestaurante.Application/Validation/"                           (T035)
```

## Estrategia de implementación

### MVP primero (solo historia 1)

1. Fase 1: Preparación.
2. Fase 2: Fundamentos (entidad, migración `AddDishes` con semilla, Respawn).
3. Fase 3: US1.
4. **Parar y validar**: `GET /api/v1/dishes` devuelve los 5 platos semilla en Development y las pruebas de US1 pasan.

### Entrega incremental

1. Preparación y Fundamentos.
2. US1 (MVP) y US2 para completar las historias P1 (consultar y dar de alta).
3. US3 (modificar) y US4 (retirar) para el ciclo de vida completo.
4. US5 (verificación del menú inicial por la API y de la identidad tras la semilla).
5. Pulido: reglas de arquitectura, `.http`, formato, suite completa y puerta de cobertura **obligatoria** (FR-030).
