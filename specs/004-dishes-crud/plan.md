# Plan de implementación: CRUD de platos

**Rama**: `speckit-test` (feature `004-dishes-crud`) | **Fecha**: 2026-11-04 | **Spec**: [spec.md](spec.md)

**Entrada**: especificación de la feature en `specs/004-dishes-crud/spec.md`

## Resumen

Primera feature de negocio: CRUD de platos sobre `/api/v1/dishes`, con borrado lógico, nombre único entre los platos activos sin distinguir mayúsculas y 5 platos semilla.

1. **Domain**: entidad anémica `Dish` (Id `int`, precio `decimal`, fechas `DateTimeOffset`, `DeletedAt` anulable).
2. **Application**: `IApplicationDbContext` gana `DbSet<Dish> Dishes`. Carpeta `Dishes/` con `IDishService`/`DishService` (un método por caso de uso), `SaveDishRequest` (DataAnnotations en español), `DishDto` y `DishMappings.ToDto()`. Carpeta `Validation/` con dos atributos propios: `MaxDecimalPlaces` y `HttpUrl`. El servicio recorta los textos, toma la hora de `TimeProvider`, filtra los eliminados de forma explícita y traduce la unicidad a `BusinessRuleException`, también cuando pierde una carrera.
3. **Infrastructure**: `DishConfiguration` (longitudes, `numeric(10,2)`, colación ICU no determinista `es-u-ks-level2`, índice único filtrado por `DeletedAt IS NULL`, `HasData`) y la migración generada `AddDishes`.
4. **Api**: `DishesController` (5 acciones, sin lógica), registro de `IDishService` en `Program.cs` y mensaje en español para valores de consulta inválidos.
5. **Pruebas**: validación en `Application.UnitTests`; servicio y semilla contra PostgreSQL en `Infrastructure.IntegrationTests` (Moq para `TimeProvider`, interceptor para la carrera); contrato HTTP en `Api.FunctionalTests`; Respawn integrado en ambos proyectos con base de datos; dos reglas de arquitectura nuevas.

El diseño se validó con un prototipo desechable: **186 pruebas en verde** y **98,1 % de cobertura de líneas** sin exclusiones (ver [research.md](research.md)).

## Contexto técnico

**Lenguaje/Versión**: C# sobre .NET 10 (SDK 10.0.112 de `global.json` con `latestFeature`)

**Dependencias principales**: las existentes (ASP.NET Core 10, EF Core 10.0.14, Npgsql EF Core 10.0.3). Paquetes nuevos, solo de pruebas, con las versiones de la tabla del stack de la constitución:

| Proyecto | Paquete nuevo | Versión |
| --- | --- | --- |
| `Infrastructure.IntegrationTests` | `Respawn` | 7.0.0 |
| `Infrastructure.IntegrationTests` | `Moq` | 4.21.0 |
| `Api.FunctionalTests` | `Respawn` | 7.0.0 |

**Almacenamiento**: PostgreSQL 18; tabla `Dishes`, colación ICU `case_insensitive` e índice único filtrado, todo generado desde el modelo por migraciones de EF Core

**Pruebas**: xUnit v3 sobre Microsoft.Testing.Platform, AwesomeAssertions, Moq, `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers.PostgreSql (`postgres:18`), Respawn y NetArchTest.eNhancedEdition. Docker es obligatorio.

**Plataforma de destino**: API web REST ejecutada en local (`dotnet run`) con PostgreSQL en Docker Compose

**Tipo de proyecto**: web-service (API REST en cuatro capas)

**Objetivos de rendimiento**: N/A (decenas de platos, sin paginación; supuesto de la spec)

**Restricciones**: cero advertencias (`TreatWarningsAsErrors`); `dotnet format` limpio, incluida la migración generada; cobertura ≥ 80 % obligatoria y sin exclusiones (FR-030); sin `try`/`catch` en los controladores; sin DDL a mano; sin InMemory ni SQLite; las pruebas existentes (FR-027, FR-031) no se modifican; el código en inglés y los mensajes y artefactos en español

**Escala/Alcance**: en `src`, 1 entidad, 1 interfaz y 1 servicio, 2 DTOs con su mapeo, 2 atributos de validación, 1 configuración, 1 migración generada y 1 controlador; cambios en `IApplicationDbContext`, `ApplicationDbContext`, `Program.cs`, `RequestBodyMessages` y `MiRestaurante.Api.http`. En `tests`, 1 fixture de base migrada, 1 interceptor de prueba, `ApiFactory` con `ResetDatabaseAsync()` y clases nuevas en 4 proyectos de pruebas.

No quedan marcadores `NEEDS CLARIFICATION`: las 21 aclaraciones de la spec y las decisiones técnicas de [research.md](research.md) los resuelven todos.

## Constitution Check

_Puerta: debe pasar antes de la Fase 0. Se reevalúa tras el diseño de la Fase 1._

| Principio | Evaluación | Resultado |
| --- | --- | --- |
| I. Simplicidad | Ningún proyecto ni paquete de producción nuevo. Los tres paquetes de pruebas están en la tabla del stack, activos y sin vulnerabilidades (Respawn 7.0.0, publicado el 2025-11-30). Sin repositorios, filtros globales, mediadores ni mapeadores. Las piezas añadidas que no son triviales (dos atributos de validación, la colación ICU y el interceptor de prueba) se justifican en Seguimiento de complejidad. | Pasa |
| II. Arquitectura en cuatro capas | `Dish` en Domain, sin dependencias. Servicio, DTOs y atributos en Application, que solo usa Domain, EF Core (núcleo) y ensamblados del framework (`System.ComponentModel.Annotations`). La configuración y la migración van en Infrastructure. El controlador solo depende de `IDishService` y de los DTOs. Se añaden pruebas para la regla «los controladores usan únicamente interfaces de servicios» (D13). | Pasa |
| III. Modelo de dominio simple | Entidad anémica sin atributos, Id `int` autoincremental, fechas `DateTimeOffset` UTC → `timestamptz`, hora desde `TimeProvider.GetUtcNow()`, precio `decimal` `numeric(10,2)` en COP. | Pasa |
| IV. Servicios clásicos | `IDishService`/`DishService` con un método por caso de uso, registrado en `Program.cs`. Mapeo manual (`ToDto()`); el servicio devuelve DTOs. Validación de entrada con DataAnnotations en español; las reglas de negocio (existencia y unicidad) están en el servicio y lanzan `NotFoundException`/`BusinessRuleException`. Todo es asíncrono y propaga el `CancellationToken`. | Pasa |
| V. Datos con EF Core y PostgreSQL | `IApplicationDbContext` con `DbSet<Dish>`; `IEntityTypeConfiguration<Dish>`; esquema, colación, índice y semilla (`HasData`) generados por la migración `AddDishes`; el script idempotente se comprobó dos veces; sin SQL concatenado. `HasFilter("\"DeletedAt\" IS NULL")` es configuración del modelo, no DDL escrito a mano. | Pasa |
| VI. API HTTP | Controlador `[ApiController]` en `api/v1/dishes`, con `[ProducesResponseType]` en cada acción y sin `try`/`catch`. Errores como ProblemDetails con el mapeo existente (404/422/500), sin excepciones nuevas. Los 400 los produce `[ApiController]`. Las pruebas de convenciones pasan sin cambios. | Pasa |
| VII. TDD (no negociable) | En `tasks.md` cada prueba precede a su implementación y se ve fallar. Las pruebas usan PostgreSQL real con Testcontainers `postgres:18` y las migraciones reales; Respawn limpia todas las tablas salvo `__EFMigrationsHistory` (salda la desviación de la feature 003). La semilla se verifica sobre una base recién migrada. Moq para `TimeProvider`, sin simular `DbSet` ni el contexto. Nomenclatura `{Metodo}_Should_{Resultado}_When_{Escenario}`, una sola aserción lógica, sin `if`, bucles ni ternarios (los escenarios usan `[Theory]`/`[InlineData]`). Cobertura medida: 98,1 %. | Pasa |
| VIII. Convenciones de código | Namespaces de ámbito de archivo, `Nullable`, constructores primarios, `async`/`await` sin `.Result`/`.Wait()`, `TreatWarningsAsErrors` y `dotnet format` verificados en el prototipo. Identificadores, rutas y JSON en inglés; mensajes en español. | Pasa |
| IX. Entorno local | Sin cambios en `compose.yaml` ni en la cadena de conexión. La migración se aplica sola en Development. | Pasa |
| Seguridad | Sin autenticación (fuera de alcance). La URL de imagen solo admite `http`/`https` (se rechazan `javascript:`, `data:` y `file:`); los mensajes de error no exponen datos internos; el SQL es siempre parametrizado (LINQ). | Pasa |
| Observabilidad | No se añade ninguna herramienta. El `ApiExceptionHandler` existente ya registra los 404 y 422. | Pasa |

**Reevaluación tras la Fase 1**: el diseño ([data-model.md](data-model.md), [contrato](contracts/dishes-api.md)) no añade proyectos, capas ni paquetes de producción. Al integrarse Respawn, queda saldada la desviación de la feature 003 y el principio VII se cumple sin desviaciones. Resultado: **Pasa**.

## Estructura del proyecto

### Documentación (esta feature)

```text
specs/004-dishes-crud/
├── plan.md              # Este archivo
├── research.md          # Fase 0
├── data-model.md        # Fase 1
├── quickstart.md        # Fase 1
├── contracts/
│   └── dishes-api.md    # Fase 1: contrato HTTP de /api/v1/dishes
├── checklists/
│   └── requirements.md
└── tasks.md             # Fase 2 (/speckit-tasks; no lo crea /speckit-plan)
```

### Código fuente (raíz del repositorio)

```text
src/
├── MiRestaurante.Domain/
│   └── Dish.cs                                        # NUEVO: entidad anémica
├── MiRestaurante.Application/
│   ├── IApplicationDbContext.cs                       # MODIFICADO: + DbSet<Dish> Dishes
│   ├── Dishes/
│   │   ├── IDishService.cs                            # NUEVO: List, GetById, Create, Update, Delete
│   │   ├── DishService.cs                             # NUEVO: reglas, recorte, hora, unicidad y carrera
│   │   ├── SaveDishRequest.cs                         # NUEVO: DTO de entrada con DataAnnotations
│   │   ├── DishDto.cs                                 # NUEVO: DTO de salida (record)
│   │   └── DishMappings.cs                            # NUEVO: ToDto()
│   └── Validation/
│       ├── MaxDecimalPlacesAttribute.cs               # NUEVO
│       └── HttpUrlAttribute.cs                        # NUEVO
├── MiRestaurante.Infrastructure/
│   ├── ApplicationDbContext.cs                        # MODIFICADO: Dishes, colación, ApplyConfigurationsFromAssembly
│   ├── Configurations/
│   │   └── DishConfiguration.cs                       # NUEVO: columnas, índice filtrado, HasData
│   └── Migrations/
│       ├── <ts>_AddDishes.cs                          # NUEVO (generado)
│       ├── <ts>_AddDishes.Designer.cs                 # NUEVO (generado)
│       └── ApplicationDbContextModelSnapshot.cs       # MODIFICADO (generado)
└── MiRestaurante.Api/
    ├── Controllers/
    │   └── DishesController.cs                        # NUEVO
    ├── ErrorHandling/
    │   └── RequestBodyMessages.cs                     # MODIFICADO: + InvalidValue
    ├── Program.cs                                     # MODIFICADO: IDishService + mensaje de valor inválido
    └── MiRestaurante.Api.http                         # MODIFICADO: peticiones de ejemplo de platos

tests/
├── MiRestaurante.Application.UnitTests/
│   ├── Dishes/
│   │   ├── SaveDishRequestValidationTests.cs          # NUEVO: FR-008 a FR-013 con Validator
│   │   └── DishMappingsTests.cs                       # NUEVO: FR-007
│   └── Validation/
│       ├── MaxDecimalPlacesAttributeTests.cs          # NUEVO
│       └── HttpUrlAttributeTests.cs                   # NUEVO
├── MiRestaurante.Infrastructure.IntegrationTests/
│   ├── MiRestaurante.Infrastructure.IntegrationTests.csproj  # MODIFICADO: + Respawn, + Moq
│   ├── MigratedDatabaseFixture.cs                     # NUEVO: base migrada por clase + Respawn
│   ├── InsertDuplicateInterceptor.cs                  # NUEVO: provoca la carrera de forma determinista
│   ├── DishServiceTests.cs                            # NUEVO (puede dividirse por caso de uso)
│   └── DishSeedTests.cs                               # NUEVO: base recién migrada, sin Respawn
├── MiRestaurante.Api.FunctionalTests/
│   ├── MiRestaurante.Api.FunctionalTests.csproj       # MODIFICADO: + Respawn
│   ├── ApiFactory.cs                                  # MODIFICADO: + ResetDatabaseAsync()
│   ├── ListDishesTests.cs                             # NUEVO (US1)
│   ├── GetDishTests.cs                                # NUEVO (US1)
│   ├── CreateDishTests.cs                             # NUEVO (US2, incluida la concurrencia)
│   ├── UpdateDishTests.cs                             # NUEVO (US3)
│   ├── DeleteDishTests.cs                             # NUEVO (US4)
│   └── DishSeedApiTests.cs                            # NUEVO (SC-001; solo lectura, sin Respawn)
└── MiRestaurante.Architecture.Tests/
    └── ControllerServiceDependencyTests.cs            # NUEVO: 2 reglas (D13)
```

**Decisión de estructura**: se conservan las cuatro capas y los cinco proyectos de pruebas. Lo nuevo de la feature se agrupa en carpetas por área funcional (`Dishes/`); los atributos reutilizables van en `Validation/`; las configuraciones de EF, en `Configurations/`. El controlador va en el namespace `MiRestaurante.Api.Controllers`, que ya vigila `LayerDependencyTests`. Ninguna prueba existente se edita: en `ApiFactory` solo se añade un método.

## Diseño técnico

### Domain

`Dish` con `Id`, `Name`, `Description?`, `Price`, `ImageUrl?`, `IsAvailable`, `CreatedAt`, `UpdatedAt` y `DeletedAt?` ([data-model.md](data-model.md)). `Name` se inicializa a `string.Empty` para cumplir `Nullable`.

### Application

- **`IApplicationDbContext`**: `DbSet<Dish> Dishes { get; }` + `SaveChangesAsync(CancellationToken)`.
- **`IDishService`**:
  - `Task<IReadOnlyList<DishDto>> ListAsync(bool? available, CancellationToken)`
  - `Task<DishDto> GetByIdAsync(int id, CancellationToken)`
  - `Task<DishDto> CreateAsync(SaveDishRequest request, CancellationToken)`
  - `Task<DishDto> UpdateAsync(int id, SaveDishRequest request, CancellationToken)`
  - `Task DeleteAsync(int id, CancellationToken)`
- **`DishService(IApplicationDbContext db, TimeProvider timeProvider)`**:
  - `ActiveDishes` (privada) = `db.Dishes.Where(d => d.DeletedAt == null)`; la usan todas las operaciones (D4).
  - Listado: filtro opcional por `IsAvailable`; `OrderBy(Name).ThenBy(Id)` (la colación de la columna da el orden sin mayúsculas; el Id desempata).
  - `FindActiveAsync(id)` lanza `NotFoundException("No existe el plato {id}.")` si no lo encuentra.
  - `Apply(dish, request, now)` copia los datos del DTO y los recorta (FR-014), y fija `UpdatedAt`. Al crear, `CreatedAt` = `UpdatedAt` = el mismo `now`.
  - Guardado con unicidad (D3): `NameInUseAsync` (otro plato activo con el mismo nombre; el `==` usa la colación) → `BusinessRuleException("Ya existe un plato con el nombre «{nombre}».")`. Luego `SaveChangesAsync` en un `try`; ante un `DbUpdateException`, repite la comprobación y lanza el 422 o relanza la excepción.
  - Orden FR-021: el 400 lo resuelve `[ApiController]` antes de la acción; el servicio comprueba la existencia antes que la unicidad.
- **`SaveDishRequest`** y atributos propios: ver [research.md D6](research.md#d6-validación-de-entrada-fr-008-a-fr-014) y los mensajes del [contrato](contracts/dishes-api.md#mensajes-de-validación).
- **`DishDto`** (record posicional) y `DishMappings.ToDto(this Dish)`.

### Infrastructure

- `ApplicationDbContext`: `public DbSet<Dish> Dishes => Set<Dish>();`. `OnModelCreating` declara la colación (`HasCollation(..., "es-u-ks-level2", "icu", deterministic: false)`) y llama a `ApplyConfigurationsFromAssembly`.
- `DishConfiguration` (`internal sealed`): `HasMaxLength` 100/500/2048, `HasPrecision(10, 2)`, `UseCollation` en `Name`, `HasIndex(Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL")` y `HasData` con los 5 platos de la spec (D5).
- Migración: `dotnet ef migrations add AddDishes --project src/MiRestaurante.Infrastructure --startup-project src/MiRestaurante.Api --output-dir Migrations`. Se versiona tal como la genera la herramienta.
- `DependencyInjection` no cambia.

### Api

- `DishesController(IDishService dishService)` con `[ApiController]` y `[Route("api/v1/dishes")]`:

  | Acción | Verbo y plantilla | Devuelve | `[ProducesResponseType]` |
  | --- | --- | --- | --- |
  | `List([FromQuery] bool? available, ct)` | `GET` | `IReadOnlyList<DishDto>` | 200, 400 |
  | `GetById(int id, ct)` | `GET {id:int}` | `DishDto` | 200, 404 |
  | `Create(SaveDishRequest request, ct)` | `POST` | `CreatedAtAction(nameof(GetById), ...)` | 201, 400, 422 |
  | `Update(int id, SaveDishRequest request, ct)` | `PUT {id:int}` | `DishDto` | 200, 400, 404, 422 |
  | `Delete(int id, ct)` | `DELETE {id:int}` | `NoContent()` | 204, 404 |

- `Program.cs`: `builder.Services.AddScoped<IDishService, DishService>();` y, en `AddControllers`, `options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => RequestBodyMessages.InvalidValue)` (D7).
- `MiRestaurante.Api.http`: peticiones de ejemplo de las cinco operaciones (documentación de uso).

### Pruebas

| Proyecto | Clase | Cubre | FR / historia |
| --- | --- | --- | --- |
| Application.UnitTests | `SaveDishRequestValidationTests` | Cada regla con `Validator.TryValidateObject(..., validateAllProperties: true)`: obligatorios, longitudes antes del recorte, solo espacios, rango y decimales del precio (incluidos `1000.550` válido y `1000.555` inválido), esquemas de URL, URL con espacios alrededor; mensajes en español | FR-008 a FR-013 |
| Application.UnitTests | `MaxDecimalPlacesAttributeTests`, `HttpUrlAttributeTests` | Casos límite de cada atributo (`[Theory]`) | FR-011, FR-012 |
| Application.UnitTests | `DishMappingsTests` | `ToDto()` copia los 8 campos | FR-007 |
| Infrastructure.IntegrationTests | `DishServiceTests` (con `MigratedDatabaseFixture` + Respawn) | Listado (orden, filtro, sin eliminados, vacío); detalle y 404; alta (fechas desde `TimeProvider`, recorte, ausentes → `null`, unicidad sin mayúsculas, tildes, eliminados no cuentan); actualización (fechas, autoconflicto, 404 antes que 422, ninguno cambia ante un 422); borrado (`DeletedAt`, datos intactos, 404 repetido); carrera con `InsertDuplicateInterceptor` al crear y al actualizar; un `DbUpdateException` que no es de unicidad se relanza | US1 a US4, FR-002 a FR-023 |
| Infrastructure.IntegrationTests | `DishSeedTests` (base nueva por prueba) | Los 5 platos semilla con todos sus valores; el primer plato creado no colisiona con ellos | FR-024 a FR-026, US5 |
| Infrastructure.IntegrationTests | `ApplicationDbContextTests` (existente) | Sigue pasando; `HasPendingModelChanges()` cubre la migración nueva | FR-031 |
| Api.FunctionalTests | `ListDishesTests`, `GetDishTests`, `CreateDishTests`, `UpdateDishTests`, `DeleteDishTests` (con `ResetDatabaseAsync()` antes de cada prueba) | Códigos, ProblemDetails, `Location`, propiedades JSON exactas, filtro `available` (mayúsculas, vacío, inválido con mensaje en español), ids no numéricos/0/negativos, 400 con mensajes por campo, 422, 204 sin cuerpo; 20 altas simultáneas → 1×201 + 19×422 | US1 a US4, FR-004 a FR-021, SC-002 a SC-004 |
| Api.FunctionalTests | `DishSeedApiTests` (fábrica propia, solo lectura) | El listado devuelve 5 platos y el filtro por disponibles, 4 | SC-001 |
| Architecture.Tests | `ControllerServiceDependencyTests` | Los controladores no dependen de servicios concretos ni de Domain | Principios II y IV |

**Infraestructura de pruebas** (D11, D12): `MigratedDatabaseFixture` (`IAsyncLifetime`, fixture de clase) recibe el `PostgreSqlFixture` de ensamblado, crea y migra una base, y expone `ConnectionString`, `CreateContext(params IInterceptor[])` y `ResetAsync()`. `ApiFactory.ResetDatabaseAsync()` fuerza el arranque del host y reinicia su base. El `Respawner` se crea una vez por base con `DbAdapter.Postgres` y `TablesToIgnore = ["__EFMigrationsHistory"]`. La prueba funcional de concurrencia genera las peticiones con `Enumerable.Repeat` + `Task.WhenAll`, sin lógica condicional.

**Orden TDD**: en `tasks.md`, cada prueba precede a su implementación y se ejecuta en Red. Las de validación fallan primero por no compilar (falta el DTO) y después por falta de atributos o mensajes; las de servicio, por falta de `Dishes`/migración; las funcionales, por 404 de ruta inexistente; las de arquitectura, con un controlador temporal que después se revierte (quickstart, paso 8). La migración se genera después de ver fallar la prueba de semilla.

### Puerta de cobertura

Mismos comandos que en las features 001 y 003 ([quickstart.md, paso 10](quickstart.md#10-puerta-de-cobertura-sc-006)). Es obligatoria y no admite exclusiones. `--ignore-exit-code 8` se mantiene porque `Domain.UnitTests` sigue sin pruebas (la entidad anémica no tiene comportamiento).

## Seguimiento de complejidad

Ningún principio se incumple. Por el principio I, se justifican las piezas añadidas que no son triviales:

| Pieza añadida | Por qué se necesita | Alternativa más simple descartada y por qué |
| --- | --- | --- |
| Atributos `MaxDecimalPlacesAttribute` y `HttpUrlAttribute` | FR-011 (2 decimales significativos, sin depender de la escala escrita) y FR-012 (solo `http`/`https`, comprobando la URL ya recortada) | `[RegularExpression]` depende de la cultura y de la escala; `[Url]` acepta `ftp://` y no recorta; `IValidatableObject` mezcla las reglas de varios campos en un método |
| Colación ICU no determinista `es-u-ks-level2` | Unicidad sin mayúsculas pero con tildes, garantizada por la base de datos ante peticiones simultáneas (FR-015, FR-017), y orden alfabético sin mayúsculas | `citext` necesita una extensión; un índice sobre `lower()` exige DDL a mano (principio V); comprobar solo en el servicio no resiste la concurrencia |
| `try`/`catch (DbUpdateException)` con una segunda comprobación en `DishService` | Convertir en 422 la carrera perdida (FR-017) sin que Application conozca Npgsql | Traducir `PostgresException` en Infrastructure: exige una excepción nueva o mensajes de negocio en Infrastructure |
| `InsertDuplicateInterceptor` (solo pruebas) | Provocar la carrera de forma determinista y cubrir la rama `catch` | Confiar solo en la prueba funcional de 20 peticiones simultáneas: no garantiza recorrer esa rama |
| Respawn en dos proyectos de pruebas y Moq en `Infrastructure.IntegrationTests` | Los exige el principio VII (limpieza entre pruebas y dobles de `TimeProvider`) | Una base nueva por prueba: más lento, y la constitución exige Respawn; un `TimeProvider` escrito a mano: el principio VII fija Moq para los dobles |

No se añaden otros paquetes, proyectos ni patrones que requieran justificación.
