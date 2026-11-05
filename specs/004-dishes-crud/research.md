# Investigación: CRUD de platos

Decisiones de la Fase 0. Todas las incógnitas se resolvieron con un prototipo desechable (copia del repositorio en `/tmp`, fuera del árbol del proyecto) que implementó la feature y ejecutó la suite completa con cobertura: **186 pruebas en verde** (las de las features 001 a 003 sin cambios, más las del prototipo) y **98,1 % de cobertura de líneas** sobre los ensamblados de producción, sin exclusiones; ReportGenerator terminó con código 0.

## D1. Entidad y configuración del modelo

- **Decisión**: entidad anémica `Dish` en Domain (propiedades públicas, sin atributos). Configuración en `Infrastructure/Configurations/DishConfiguration.cs` (`IEntityTypeConfiguration<Dish>`), aplicada con `modelBuilder.ApplyConfigurationsFromAssembly(...)` en `OnModelCreating`. `IApplicationDbContext` expone `DbSet<Dish> Dishes { get; }`; el contexto lo implementa con `Set<Dish>()`.
- **Columnas**: `Name` `varchar(100)`, `Description` `varchar(500)`, `ImageUrl` `varchar(2048)`, `Price` `numeric(10,2)` (`HasPrecision(10, 2)`), fechas `timestamptz` (por convención de Npgsql para `DateTimeOffset`). Detalle en [data-model.md](data-model.md).
- **Justificación**: es lo que fijan los principios III y V; las longitudes de columna coinciden con los máximos de validación, y como se miden antes de recortar (FR-009, FR-010, FR-012), el valor guardado nunca los supera.
- **Verificado**: la migración `AddDishes` generada por `dotnet ef` compila sin advertencias y pasa `dotnet format --verify-no-changes` sin editarla. `HasPendingModelChanges()` es `false` tras migrar.

## D2. Unicidad del nombre sin distinguir mayúsculas (FR-015)

- **Decisión**: colación ICU no determinista declarada en el modelo, `modelBuilder.HasCollation("case_insensitive", locale: "es-u-ks-level2", provider: "icu", deterministic: false)`, asignada a la columna con `UseCollation`, más un índice único filtrado `HasIndex(d => d.Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL")`.
- **Justificación**: `ks-level2` compara letras base y diacríticos, pero no mayúsculas: «Ajiaco» = «AJIACO», pero «Ajiaco» ≠ «Ajíaco», como exige la spec. La misma colación sirve para el índice único (garantía en la base de datos), para la comprobación previa del servicio (`d.Name == name` se traduce con la colación de la columna) y para el orden del listado. Todo se genera desde el modelo: la migración contiene `CREATE COLLATION` y `CREATE UNIQUE INDEX ... WHERE "DeletedAt" IS NULL` sin DDL escrito a mano (principio V).
- **Locale**: con `und` (raíz), «ñame» se ordena antes que «nube»; con `es`, la «ñ» es una letra propia que va después de la «n». La spec no exige una colación concreta, pero `es` cuesta lo mismo y produce el orden que espera un usuario hispanohablante.
- **Alternativas descartadas**:
  - `citext`: necesita una extensión de PostgreSQL y añade un tipo de columna que no es estándar. La documentación de Npgsql recomienda las colaciones no deterministas.
  - Índice único sobre `lower("Name")`: EF Core no genera índices de expresión, así que haría falta `migrationBuilder.Sql(...)` (DDL a mano, prohibido por el principio V).
  - Solo comprobar en el servicio: no garantiza la unicidad ante peticiones simultáneas (FR-017).
- **Limitación conocida**: PostgreSQL no admite `LIKE` sobre colaciones no deterministas en versiones anteriores a la 18. Esta feature no busca por patrón.
- **Verificado**: «Ajiaco» + « AJIACO » → 422; «Ajiaco» + «Ajíaco» → 201; un duplicado eliminado no cuenta; actualizar «Ajiaco» a «AJIACO» en el propio plato → 200; orden «Apple, árbol, banana, cherry, nube, ñame, Zeta».

## D3. Carrera entre peticiones simultáneas (FR-017)

- **Decisión**: el servicio comprueba primero si el nombre ya está en uso (`AnyAsync` sobre los platos activos, excluyendo el propio plato al actualizar) y lanza `BusinessRuleException` si lo está. Después llama a `SaveChangesAsync` dentro de un `try`. Si se produce un `DbUpdateException`, repite la comprobación: si ahora el nombre está en uso, lanza `BusinessRuleException`; si no, relanza la excepción original (→ 500).
- **Justificación**: el caso normal no pasa por una excepción de base de datos (EF Core registra los `SaveChanges` fallidos con nivel Error). El caso de la carrera lo detecta el índice único, y repetir la comprobación permite identificarlo sin que Application conozca Npgsql ni los códigos `SQLSTATE`. El principio VI prohíbe `try`/`catch` en los controladores, no en los servicios.
- **Alternativas descartadas**:
  - Solo capturar la excepción, sin comprobación previa: cada duplicado ordinario generaría un registro de error.
  - Traducir `PostgresException` (`23505`) en Infrastructure: obliga a crear una excepción nueva o a que Infrastructure redacte mensajes de negocio.
  - Transacción `Serializable`: más conceptos y reintentos para el mismo resultado.
- **Verificado**: de forma determinista, con un `SaveChangesInterceptor` de prueba que inserta el duplicado entre la comprobación y el guardado (→ `BusinessRuleException`). Con 20 `POST` simultáneos con el mismo nombre en distintas mayúsculas, el resultado es exactamente un 201 y diecinueve 422.

## D4. Borrado lógico (FR-003, FR-020, FR-023)

- **Decisión**: `DeletedAt` (`DateTimeOffset?`) en la entidad. El servicio filtra de forma explícita con una propiedad privada `ActiveDishes => db.Dishes.Where(d => d.DeletedAt == null)`, que usan todas las consultas. `DELETE` solo fija `DeletedAt`.
- **Justificación**: la regla de negocio queda visible en el servicio, que es donde el principio IV sitúa las reglas.
- **Alternativa descartada**: filtro global (`HasQueryFilter`): oculta la regla en la configuración y obliga a usar `IgnoreQueryFilters()` en las pruebas que comprueban que el registro sigue existiendo.

## D5. Datos semilla e identidad (FR-024, FR-025)

- **Decisión**: `HasData` con los 5 platos de la spec (Id 1 a 5), `CreatedAt` = `UpdatedAt` = `2026-11-04T00:00:00Z` y `DeletedAt` nulo. No se configura nada sobre la identidad.
- **Hallazgo**: Npgsql 10 añade a la migración, después de los `INSERT` de la semilla, un `setval(pg_get_serial_sequence('"Dishes"', 'Id'), GREATEST(MAX("Id") + 1, nextval(...)), false)`. El primer plato creado recibe el Id 6 sin necesidad de `HasIdentityOptions(startValue: ...)`, que se probó y se descartó por innecesario.
- **Verificado**: sobre una base recién migrada existen los Ids 1 a 5, y el primer plato creado por el servicio recibe el Id 6. El script idempotente se aplicó dos veces seguidas en un `postgres:18` limpio, terminó con código 0 ambas veces, y el `INSERT` posterior también recibió el Id 6.

## D6. Validación de entrada (FR-008 a FR-014)

- **Decisión**: un único DTO de entrada `SaveDishRequest` en Application para crear y actualizar, con propiedades anulables para que `[Required]` detecte la ausencia (no se aplican valores por defecto):

  | Propiedad | Atributos |
  | --- | --- |
  | `string? Name` | `[Required]`, `[MaxLength(100)]` |
  | `string? Description` | `[MaxLength(500)]` |
  | `decimal? Price` | `[Required]`, `[Range(typeof(decimal), "0", "99999999.99", MinimumIsExclusive = true, ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]`, `[MaxDecimalPlaces(2)]` |
  | `string? ImageUrl` | `[MaxLength(2048)]`, `[HttpUrl]` |
  | `bool? IsAvailable` | `[Required]` |

  Todos los atributos llevan `ErrorMessage` en español (textos en el [contrato](contracts/dishes-api.md#mensajes-de-validación)).
- **Atributos propios** (en `Application/Validation/`):
  - `MaxDecimalPlacesAttribute(int)`: válido si el valor no es `decimal` o si `decimal.Round(valor, n) == valor`. La igualdad de `decimal` ignora la escala, así que `1000.550` es válido y `1000.555` no (FR-011). No desborda con valores grandes.
  - `HttpUrlAttribute`: válido si el valor es nulo o solo tiene espacios (se guardará como ausente), o si, una vez recortado, `Uri.TryCreate(..., UriKind.Absolute, ...)` tiene éxito con esquema `http` o `https`. Rechaza `ftp:`, `javascript:`, `file:`, `data:` y las rutas relativas (FR-012).
- **Comportamiento de los atributos estándar**: `[Required]` rechaza las cadenas vacías o solo con espacios (FR-009, caso límite «nombre solo con espacios»); `[MaxLength]` mide el valor recibido, antes de recortar.
- **Recorte (FR-014)**: lo hace el servicio al copiar el DTO a la entidad: `Trim()` en el nombre; la descripción y la URL vacías o solo con espacios pasan a `null`, y en otro caso se recortan.
- **Alternativas descartadas**: `[Url]` (acepta `ftp://` y no recorta); `[RegularExpression]` sobre el precio (depende de la cultura y de la escala escrita); `IValidatableObject` (mezcla las reglas de varios campos en un método); propiedades `required` de C# (la ausencia produce un error de JSON genérico en lugar de un mensaje por campo).
- **Verificado**: cada caso límite de la spec produce un 400 con su mensaje en español bajo la clave del campo (`Name`, `Price`, `ImageUrl`, `IsAvailable`). Un precio de 0 o de 100 000 000 da el mensaje de rango; 1000,555 da el mensaje de decimales.

## D7. Filtro `available` (FR-005)

- **Decisión**: parámetro de acción `[FromQuery] bool? available`. El servicio solo filtra si no es nulo.
- **Comportamiento del enlace de modelos (verificado)**: `true`, `True`, `TRUE`, `false` y `FALSE` se aceptan (el convertidor de `bool` no distingue mayúsculas); `?available=` produce `null` (sin filtro); `si` o `1` producen un error de `ModelState` → 400 ProblemDetails automático por `[ApiController]`.
- **Mensaje en español**: por defecto el error es «The value 'si' is not valid.». Para un parámetro de acción (no una propiedad), ASP.NET usa `NonPropertyAttemptedValueIsInvalidAccessor`, así que se configura `options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => RequestBodyMessages.InvalidValue)` en `Program.cs`, junto al mensaje de cuerpo ausente de la feature 002. El texto («El valor indicado no es válido.») no repite el valor recibido.
- **Verificado**: `{"errors":{"available":["El valor indicado no es válido."]}}` con `application/problem+json`.

## D8. Identificadores en la ruta

- **Decisión**: plantilla `{id:int}` en `GET`, `PUT` y `DELETE`.
- **Comportamiento (verificado)**: `abc` o un valor que desborda `int` (`99999999999`) no coinciden con ninguna ruta → 404 ProblemDetails desde `UseStatusCodePages` (feature 002). `0` y `-1` llegan al servicio, que no encuentra el plato y lanza `NotFoundException` → 404 con `detail`.
- **Alternativa descartada**: `{id:int:min(1)}` (añade una restricción sin cambiar el resultado observable, que en ambos casos es 404).

## D9. Respuestas y serialización

- **Decisión**: DTO de salida `DishDto` (record posicional) con mapeo manual `dish.ToDto()` (método de extensión en `DishMappings`). El controlador devuelve el DTO (200), `CreatedAtAction(nameof(GetById), new { id }, dto)` (201) y `NoContent()` (204).
- **Verificado**: las propiedades JSON salen en camelCase (`id`, `name`, `description`, `price`, `imageUrl`, `isAvailable`, `createdAt`, `updatedAt`), con `null` explícito en las opcionales y sin `deletedAt`. `Location` es absoluta (`http://localhost/api/v1/dishes/6`) y su ruta es `/api/v1/dishes/{id}`. El 204 no tiene cuerpo. El precio leído de la base conserva la escala 2 (`32000.00`). Las fechas salen con offset `+00:00`.
- **Precisión de las fechas**: PostgreSQL guarda microsegundos y `TimeProvider.System` da hasta 100 ns, así que la respuesta de un `POST`/`PUT` puede tener un dígito decimal más que un `GET` posterior del mismo plato. No se trunca: la spec no exige igualdad por debajo del microsegundo, y truncar añadiría código en cada escritura. Las pruebas usan horas fijas sin fracción de microsegundo.

## D10. Hora actual en el servicio y en las pruebas

- **Decisión**: `DishService(IApplicationDbContext db, TimeProvider timeProvider)` con constructor primario; una sola llamada a `GetUtcNow()` por operación. Al crear: `CreatedAt` = `UpdatedAt` = ahora. Al actualizar: `UpdatedAt` = ahora, aunque no cambie ningún valor. Al eliminar: `DeletedAt` = ahora. El `TimeProvider.System` ya está registrado en `Program.cs` (feature 002).
- **Pruebas**: `Mock<TimeProvider>` con `Setup(t => t.GetUtcNow())`, que es virtual (principio VII: Moq para los dobles). Se añade Moq 4.21.0 a `Infrastructure.IntegrationTests`, donde se prueban los servicios contra PostgreSQL.
- **Registro**: `builder.Services.AddScoped<IDishService, DishService>()` en `Program.cs`, la raíz de composición (principio IV). `DishService` es `public sealed`.

## D11. Respawn (FR-028)

- **Decisión**: Respawn 7.0.0 en `Infrastructure.IntegrationTests` y `Api.FunctionalTests`, con `new RespawnerOptions { DbAdapter = DbAdapter.Postgres, TablesToIgnore = ["__EFMigrationsHistory"] }`. El `Respawner` se crea una sola vez por base de datos, sobre una `NpgsqlConnection` abierta, y se reutiliza.
  - Integración: un fixture de clase `MigratedDatabaseFixture` (crea la base con `PostgreSqlFixture.CreateDatabaseAsync()`, la migra, expone `CreateContext(...)` y `ResetAsync()`). Las clases de prueba de servicios lo usan con `IClassFixture` e implementan `IAsyncLifetime` para llamar a `ResetAsync()` antes de cada prueba.
  - Funcionales: `ApiFactory` gana `ResetDatabaseAsync()`, que primero fuerza el arranque del host (`_ = Services`) para que las migraciones de Development existan antes de que Respawn inspeccione las tablas.
- **Consecuencia**: Respawn también borra la semilla, así que cada prueba crea sus datos (principio VII). Las pruebas de la semilla usan una base recién migrada y sin reiniciar: en integración, una base nueva por prueba (patrón de la feature 003); en las funcionales, una clase propia de solo lectura con su propia fábrica.
- **Paquete**: 7.0.0, publicado el 2025-11-30 (menos de 12 meses), sin dependencias (`netstandard2.1`), sin vulnerabilidades ni deprecación en NuGet.
- **Verificado**: tras `ResetAsync()` la tabla `Dishes` queda vacía y `__EFMigrationsHistory` intacta. Las pruebas posteriores insertan sin errores, y la colación y el índice siguen activos.

## D12. Pruebas de la carrera

- **Integración (determinista)**: un `SaveChangesInterceptor` de prueba, `InsertDuplicateInterceptor`, que en `SavingChangesAsync` inserta con otro contexto un plato con el mismo nombre. El contexto bajo prueba se construye con `.AddInterceptors(...)`. Cubre la rama `catch` del servicio, tanto al crear como al actualizar.
- **Funcional**: 20 `POST` simultáneos con `Enumerable.Repeat(cuerpo, 20)` y `Task.WhenAll`. La aserción única compara los códigos ordenados con `[201, 422 × 19]`. No hay `if`, bucles ni ternarios en la prueba.

## D13. Reglas de arquitectura nuevas

- **Decisión**: una clase nueva `ControllerServiceDependencyTests` en `Architecture.Tests` con dos reglas (NetArchTest, mismo estilo que `ControllerDataAccessTests`):
  1. Los tipos que heredan de `ControllerBase` no dependen de ninguna clase concreta de Application cuyo nombre termine en `Service` (lista obtenida por reflexión). Verifica el principio II: «DEBEN usar únicamente interfaces de servicios de aplicación».
  2. Los controladores no dependen de `MiRestaurante.Domain`. Verifica el principio IV: «los servicios NO DEBEN devolver entidades de dominio a la capa Api».
- **Verificado**: ambas pasan con `DishesController` y fallan (Red) con un controlador temporal que recibe `DishService` y devuelve `Dish`. Las pruebas de arquitectura y convenciones existentes pasan sin cambios con el nuevo controlador.

## D14. Cobertura

- **Medido**: 98,1 % de líneas (490 de 499): Api 100 %, Application 98,5 % (`DishService` 98,2 %), Infrastructure 97,4 % (solo quedan sin cubrir los `Down()` de las migraciones). Domain no aparece en el informe porque `Dish` solo tiene propiedades automáticas. No hace falta excluir nada.
- `--ignore-exit-code 8` se mantiene en `Directory.Build.props`: `Domain.UnitTests` sigue sin pruebas, porque la entidad anémica no tiene comportamiento.

## D15. Paquetes

| Proyecto | Paquete | Versión | Motivo |
| --- | --- | --- | --- |
| `Infrastructure.IntegrationTests` | `Respawn` | 7.0.0 | FR-028, principio VII |
| `Infrastructure.IntegrationTests` | `Moq` | 4.21.0 | Doble de `TimeProvider` (principio VII) |
| `Api.FunctionalTests` | `Respawn` | 7.0.0 | FR-028, principio VII |

No se añade ningún paquete de producción. `Npgsql` llega a los proyectos de pruebas de forma transitiva, como en la feature 003. `dotnet list package --vulnerable` y `--deprecated` no informan de nada.
