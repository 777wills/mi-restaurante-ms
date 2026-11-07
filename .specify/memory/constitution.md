<!--
- Cambio de versión: 1.2.0 → 1.3.0 (MINOR: se establece CI obligatorio y protección de `main`).
- Secciones modificadas:
  · «Flujo de desarrollo y puertas de calidad»: workflow de GitHub Actions por PR y push,
    job obligatorio `calidad`, job informativo `mutacion` y ruleset requerido para `main`.
- Secciones añadidas o eliminadas: ninguna.
- TODO diferidos: ninguno.
-->

# Constitución de MiRestaurante

Repositorio `mi-restaurante-ms`. Las palabras **DEBE** (MUST, obligatorio), **NO DEBE**
(MUST NOT, prohibido) y **DEBERÍA** (SHOULD, recomendado salvo justificación escrita) se
interpretan según BCP 14 (RFC 2119 y RFC 8174) cuando aparecen en mayúsculas.

## Principios fundamentales

### I. Simplicidad (principio rector)

Este principio prevalece sobre todos los demás. Ante un conflicto o una duda de
interpretación se aplica primero.

- Ante dos alternativas válidas se DEBE elegir la que tenga menos piezas, menos dependencias
  y menos conceptos.
- Toda complejidad adicional (proyecto, capa, paquete NuGet, patrón, abstracción, proceso o
  infraestructura) DEBE justificarse en la tabla **Complexity Tracking** del plan, indicando
  qué se necesita, por qué, y qué alternativa más simple se descartó y por qué.
- NO DEBEN introducirse abstracciones, patrones ni paquetes «por si acaso» (YAGNI): solo
  cuando una spec los requiera.
- Existe un único servicio, con un único despliegue y una única base de datos. NO DEBEN
  introducirse mensajería, eventos de integración ni comunicación entre servicios hasta que
  una spec lo requiera. El sufijo «-ms» solo anticipa servicios futuros.
- Antes de añadir un paquete DEBE comprobarse que no está deprecado ni tiene vulnerabilidades
  conocidas. Un paquete sin versiones publicadas en los últimos 12 meses se considera en
  riesgo, y su uso DEBE justificarse en Complexity Tracking.

**Razón**: es un proyecto pequeño y académico; cada pieza extra cuesta aprenderla, probarla y
mantenerla. La complejidad que no se justifica por escrito no entra.

### II. Arquitectura limpia en cuatro capas

- La solución `MiRestaurante.slnx` DEBE mantener exactamente cuatro proyectos de producción:
  `src/MiRestaurante.Domain`, `src/MiRestaurante.Application`,
  `src/MiRestaurante.Infrastructure` y `src/MiRestaurante.Api`, sobre .NET 10 con el SDK
  fijado en `global.json`.
- Regla de dependencias: Domain ← Application ← Infrastructure. Api es la raíz de composición
  y referencia Application e Infrastructure; a Infrastructure solo la referencia para
  registrar dependencias.
- Domain NO DEBE depender de paquetes NuGet ni de otros proyectos: solo de ensamblados del
  framework.
- Application DEBE depender solo de Domain y del paquete `Microsoft.EntityFrameworkCore`
  (núcleo, sin proveedor), necesario para declarar `IApplicationDbContext`. Se admite además
  el ensamblado `Microsoft.Extensions.Logging.Abstractions`, dependencia transitiva del
  núcleo, para usar `ILogger<T>` como exige la sección de Observabilidad. NO DEBE
  referenciar Infrastructure, Api, `Microsoft.EntityFrameworkCore.Relational` ni ningún
  proveedor de base de datos (por ejemplo, Npgsql).
- Infrastructure implementa las interfaces declaradas en Application y NO DEBE referenciar Api.
- Los controladores NO DEBEN contener lógica de negocio ni depender de Infrastructure ni de
  `IApplicationDbContext`; DEBEN usar únicamente interfaces de servicios de aplicación.
- Cada regla de dependencias de este principio DEBE estar verificada por una prueba en
  `tests/MiRestaurante.Architecture.Tests`. Si esa prueba falla, el cambio viola la
  arquitectura: no se corrige ajustando la prueba.

**Razón**: el dominio y los casos de uso quedan aislados de la tecnología, y la regla se
verifica automáticamente en lugar de depender de la disciplina.

### III. Modelo de dominio simple

- Las entidades DEBEN ser simples (modelo anémico) con propiedades públicas. NO DEBEN usarse
  agregados, value objects, eventos de dominio ni especificaciones DDD.
- Las entidades NO DEBEN llevar atributos de mapeo de persistencia.
- Los identificadores DEBEN ser `int` autoincrementales.
- Las fechas y horas DEBEN ser `DateTimeOffset` en UTC (offset cero), almacenadas como
  `timestamptz` en PostgreSQL; Npgsql solo admite escribir valores `DateTimeOffset` con
  offset cero en `timestamptz`.
- La hora actual DEBE obtenerse de un `TimeProvider` inyectado (`GetUtcNow()`). NO DEBEN
  usarse `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now` ni `DateTimeOffset.UtcNow`.
- Los precios DEBEN ser `decimal` con precisión (10,2), en una única moneda implícita (pesos
  colombianos, COP). NO DEBEN usarse `float` ni `double` para valores monetarios.

**Razón**: un menú y unos pedidos no necesitan un modelo táctico DDD. Controlar el tiempo
hace las pruebas deterministas, y `decimal` evita errores de redondeo con el dinero.

### IV. Capa de aplicación con servicios clásicos

- Los casos de uso DEBEN implementarse como servicios de aplicación por área funcional
  (por ejemplo, `IMenuService`/`MenuService`, `IOrderService`/`OrderService`), con un método
  por caso de uso, registrados en el contenedor de DI desde la raíz de composición e
  inyectados en los controladores.
- NO DEBEN usarse CQRS, mediadores (MediatR, Mediator, Cortex.Mediator ni similares) ni
  librerías de pipeline.
- El mapeo entre entidades y DTOs DEBE ser manual (métodos de extensión `ToDto()` o
  constructores de DTO). NO DEBEN usarse mapeadores automáticos (AutoMapper, Mapster,
  Mapperly).
- Los servicios NO DEBEN devolver entidades de dominio a la capa Api: devuelven DTOs.
- La validación de entrada DEBE usar DataAnnotations sobre los DTOs de entrada
  (`[Required]`, `[Range]`, `[MaxLength]`…) con mensajes en español.
- Las reglas de negocio (por ejemplo, no pedir un plato no disponible) DEBEN validarse en el
  servicio, no en atributos, lanzando las excepciones de negocio definidas en Application
  (`NotFoundException`, `BusinessRuleException`).
- Todo método de servicio que haga I/O DEBE ser asíncrono y recibir un `CancellationToken`.

**Razón**: los servicios con un método por caso de uso son el patrón con menos conceptos y
el más fácil de seguir. El mapeo manual es explícito, depurable y no añade dependencias.

### V. Acceso a datos con Entity Framework Core y PostgreSQL

- El ORM DEBE ser Entity Framework Core con el proveedor `Npgsql.EntityFrameworkCore.PostgreSQL`;
  la base de datos DEBE ser PostgreSQL.
- Application DEBE declarar `IApplicationDbContext` (propiedades `DbSet<T>` y
  `SaveChangesAsync`), e Infrastructure DEBE implementarla en el `DbContext` concreto. NO
  DEBEN crearse repositorios (genéricos ni específicos) ni una Unit of Work propia: el
  `DbContext` cumple ese papel.
- La configuración del modelo DEBE hacerse con clases `IEntityTypeConfiguration<T>` en
  Infrastructure.
- El esquema DEBE gestionarse exclusivamente con migraciones de EF Core versionadas en
  Infrastructure. NO DEBE escribirse DDL a mano.
- Las migraciones DEBEN aplicarse automáticamente al iniciar la API solo en el entorno
  Development. Para cualquier otro entorno DEBE generarse un script idempotente
  (`dotnet ef migrations script --idempotent`).
- Los datos iniciales del menú DEBEN cargarse con `HasData` en la configuración del modelo,
  de modo que formen parte de las migraciones.
- NO DEBE construirse SQL concatenando ni interpolando valores en cadenas. Si se necesita SQL
  explícito, DEBE vivir en Infrastructure y usar las variantes parametrizadas de EF Core
  (`FromSql`, `ExecuteSql` o `FromSqlRaw`/`ExecuteSqlRaw` con parámetros).

**Razón**: el `DbContext` ya es un repositorio y una unidad de trabajo, así que envolverlo
duplica conceptos. Con las migraciones como única fuente del esquema, los entornos son
reproducibles, y el SQL parametrizado evita la inyección SQL (OWASP A03).

### VI. Convenciones de la API HTTP

- Los endpoints DEBEN ser controladores MVC con `[ApiController]`. NO DEBEN usarse Minimal
  APIs ni FastEndpoints.
- Todas las rutas de negocio DEBEN llevar el prefijo fijo `/api/v1/`
  (por ejemplo, `[Route("api/v1/[controller]")]`).
- El documento OpenAPI DEBE generarse de forma nativa con `Microsoft.AspNetCore.OpenApi`
  (`AddOpenApi`/`MapOpenApi`), y la interfaz interactiva DEBE ser Scalar
  (`Scalar.AspNetCore`), ambos expuestos solo en Development. NO DEBE usarse Swashbuckle.
- Cada acción DEBE declarar sus códigos de respuesta con `[ProducesResponseType]`. No se
  exigen comentarios de documentación XML.
- Los errores DEBEN responderse como ProblemDetails (RFC 9457) mediante `AddProblemDetails()`
  y un único `IExceptionHandler` nativo con este mapeo: `NotFoundException` → 404,
  `BusinessRuleException` → 422 y cualquier otra excepción → 500. La respuesta 500 NO DEBE
  exponer trazas de pila ni detalles internos. Una nueva excepción con otro código (por
  ejemplo, 409) solo se añade cuando una spec la requiera.
- Los controladores NO DEBEN contener `try`/`catch`. Los errores de validación de
  DataAnnotations responden 400 automáticamente gracias a `[ApiController]`.
- DEBE existir el endpoint de salud `/health`, que verifica la conexión a la base de datos
  con `AddDbContextCheck`. Está fuera del prefijo `/api/v1/`.
- CORS DEBE estar abierto (cualquier origen, método y cabecera) únicamente en Development.

**Razón**: los controladores con convenciones uniformes, OpenAPI nativo y un único punto de
traducción de errores producen un contrato HTTP predecible con el mínimo de piezas.

### VII. Desarrollo guiado por pruebas (NO NEGOCIABLE)

- Todo código nuevo DEBE seguir TDD: **Red** (escribir una prueba y verla fallar al
  ejecutarla) → **Green** (el mínimo código para que pase) → **Refactor** (limpiar sin
  romper pruebas). En `tasks.md` la tarea de prueba DEBE preceder a la de implementación.
- La cobertura de líneas DEBE ser al menos del 80 % sobre el conjunto de la solución. Se mide
  con `coverlet.MTP` (`dotnet test --coverlet --coverlet-output-format cobertura`) y se
  exige fusionando los informes con ReportGenerator
  (`minimumCoverageThresholds:lineCoverage=80`), ya que coverlet.MTP no fusiona informes
  entre proyectos de prueba.
- Herramientas: xUnit v3 (`xunit.v3`) sobre Microsoft.Testing.Platform (modo MTP de
  `dotnet test` activado en `global.json`), con proyectos de prueba ejecutables
  (`OutputType` `Exe`), AwesomeAssertions para las aserciones, Moq para los dobles de prueba
  y Stryker.NET para mutation testing informativo. NO DEBEN mezclarse VSTest ni xUnit v2:
  los proyectos de prueba NO DEBEN referenciar `Microsoft.NET.Test.Sdk` ni
  `xunit.runner.visualstudio`.
- Responsabilidad de cada proyecto de pruebas (cada uno referencia solo su capa):
  - `MiRestaurante.Domain.UnitTests`: pruebas unitarias de Domain.
  - `MiRestaurante.Application.UnitTests`: pruebas unitarias de servicios de aplicación y
    validaciones, sin base de datos. Moq se usa para dependencias como `TimeProvider` u otras
    interfaces. NO DEBEN simularse con Moq `DbSet`, el `DbContext` ni `IApplicationDbContext`.
  - `MiRestaurante.Infrastructure.IntegrationTests`: pruebas de persistencia y de los
    servicios de aplicación que dependen de `IApplicationDbContext`, contra PostgreSQL real.
  - `MiRestaurante.Api.FunctionalTests`: pruebas de extremo a extremo de los endpoints con
    `WebApplicationFactory` contra PostgreSQL real.
  - `MiRestaurante.Architecture.Tests`: pruebas de arquitectura con
    `NetArchTest.eNhancedEdition` para todas las reglas del principio II. Toda regla nueva
    de dependencias DEBE añadirse aquí como prueba.
- Las pruebas contra base de datos DEBEN usar `Testcontainers.PostgreSql` con la misma
  versión mayor de PostgreSQL que `compose.yaml`. El esquema DEBE crearse aplicando las
  migraciones reales de EF Core.
- Respawn DEBE limpiar todas las tablas, salvo `__EFMigrationsHistory`, entre pruebas. Cada
  prueba DEBE crear los datos que necesita. Los datos semilla de `HasData` DEBEN verificarse
  con una prueba específica sobre una base de datos recién migrada.
- NO DEBEN usarse el proveedor InMemory de EF Core ni SQLite como sustitutos de PostgreSQL.
- Nomenclatura: clase `{Sujeto}Tests`; método `{Metodo}_Should_{Resultado}_When_{Escenario}`.
  Cada prueba DEBE tener una sola aserción lógica y NO DEBE contener lógica condicional (`if`,
  `switch`, bucles). Los escenarios parametrizados DEBEN usar `[Theory]` con `[InlineData]`.

**Razón**: probar primero fija el comportamiento antes del código. Probar contra
PostgreSQL real evita los falsos positivos de los sustitutos en memoria, y las reglas de
nomenclatura y de una aserción hacen que cada fallo explique por sí solo qué se rompió.

### VIII. Convenciones de código e idioma

- DEBEN seguirse las C# Coding Conventions oficiales de Microsoft, con este núcleo
  obligatorio: namespaces de ámbito de archivo, `Nullable` habilitado, `async`/`await` con
  `CancellationToken` propagado en todo I/O e inyección de dependencias por constructor (se
  permiten constructores primarios). NO DEBEN usarse `.Result` ni `.Wait()`.
- Toda la solución DEBE compilar con `TreatWarningsAsErrors` habilitado.
- El código DEBE respetar `dotnet format` (verificado con `--verify-no-changes`).
- Idioma: el código, los identificadores, las rutas de la API y las propiedades JSON DEBEN
  estar en inglés (por ejemplo, `Dish`, `Order`, `/api/v1/orders`). Los mensajes dirigidos al
  usuario, la documentación y los artefactos de Spec Kit (spec, plan, tasks) DEBEN estar en
  español.
- Los agentes de IA que trabajen en este repositorio DEBEN responder siempre en español en
  toda interacción con el equipo (respuestas, explicaciones, resúmenes, preguntas y mensajes
  de commit), incluso si la petición está escrita en otro idioma. Las únicas excepciones son
  el código y los identificadores, que siguen la regla de inglés anterior, y las citas
  literales de errores, comandos o documentación externa.

**Razón**: unas convenciones uniformes y verificables por herramienta eliminan discusiones de
estilo, y tratar las advertencias como errores hace efectivo el análisis de nulabilidad. Fijar
el español como idioma de los agentes mantiene toda la comunicación del proyecto coherente
con su documentación.

### IX. Entorno de desarrollo local

- Un archivo `compose.yaml` de Docker Compose DEBE levantar únicamente PostgreSQL, con una
  etiqueta de imagen que fije la versión mayor (NO DEBE usarse `latest`). La API se ejecuta
  con `dotnet run` o desde el IDE.
- La cadena de conexión de desarrollo DEBE apuntar al contenedor de Docker Compose y
  definirse en `appsettings.Development.json`, solo con credenciales locales de desarrollo.

**Razón**: un único contenedor reproduce la base de datos sin contenerizar la API, que se
depura de forma más sencilla de forma nativa.

## Alcance del producto y stack tecnológico

**Alcance**: una única API web REST que muestra el menú de un restaurante y permite hacer
pedidos.

**Seguridad**:

- NO DEBE implementarse autenticación ni autorización hasta que una spec lo requiera.
- Las credenciales de cualquier entorno distinto de Development NO DEBEN versionarse: se
  inyectan mediante variables de entorno.

**Observabilidad**: DEBE usarse solo el `ILogger<T>` nativo de .NET, con plantillas de
mensaje. Cualquier otra herramienta (Serilog, OpenTelemetry…) DEBE justificarse en
Complexity Tracking.

**Stack**: la versión mayor es normativa. La versión exacta es informativa, se verificó en
NuGet, Docker Hub y las notas de versión de .NET el 2026-11-04, y se fija en los `.csproj`,
`global.json`, `compose.yaml` y el manifiesto de herramientas locales.

| Componente                                                        | Versión mayor (normativa) | Versión verificada                                               | Estado                                                          |
| ----------------------------------------------------------------- | ------------------------- | ---------------------------------------------------------------- | --------------------------------------------------------------- |
| .NET (SDK y runtime)                                              | 10 (LTS)                  | SDK 10.0.112 en `global.json` (`latestFeature`); runtime 10.0.14 | Soporte hasta 2028-11-14                                        |
| Microsoft.AspNetCore.OpenApi                                      | 10                        | 10.0.14                                                          | Activo                                                          |
| Scalar.AspNetCore                                                 | 2                         | 2.18.7                                                           | Activo                                                          |
| Microsoft.EntityFrameworkCore (+ .Design, dotnet-ef)              | 10                        | 10.0.14                                                          | Activo                                                          |
| Npgsql.EntityFrameworkCore.PostgreSQL                             | 10                        | 10.0.3                                                           | Activo                                                          |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10                        | 10.0.14                                                          | Activo                                                          |
| PostgreSQL (imagen oficial `postgres`)                            | 18                        | 18.6                                                             | Activo                                                          |
| xunit.v3                                                          | 4                         | 4.0.2                                                            | Activo                                                          |
| AwesomeAssertions                                                 | 9                         | 9.6.0                                                            | Activo                                                          |
| Moq                                                               | 4                         | 4.21.0                                                           | Activo; NO DEBEN usarse 4.20.0 ni 4.20.1 (incluían SponsorLink) |
| Testcontainers.PostgreSql                                         | 4                         | 4.16.0                                                           | Activo                                                          |
| Respawn                                                           | 7                         | 7.0.0                                                            | Activo                                                          |
| coverlet.MTP                                                      | 10                        | 10.1.0                                                           | Activo                                                          |
| ReportGenerator (`dotnet-reportgenerator-globaltool`)             | 5                         | 5.5.12                                                           | Activo                                                          |
| Microsoft.AspNetCore.Mvc.Testing                                  | 10                        | 10.0.14                                                          | Activo                                                          |
| NetArchTest.eNhancedEdition                                       | 1                         | 1.4.5                                                            | En riesgo (sin versiones desde 2025-06)                         |
| Stryker.NET (`dotnet-stryker`, herramienta local)                 | 5                         | 5.0.0                                                            | Activo; runner MTP en preview                                   |

- **NetArchTest.eNhancedEdition**: se acepta pese al riesgo porque sustituye directamente a
  `NetArchTest.Rules` 1.3.2, abandonado desde 2021-05, y su API es mínima. Si deja de cargar
  ensamblados de una nueva versión de .NET, DEBE evaluarse `TngTech.ArchUnitNET.xUnitV3`.
- **Prohibidos**: Swashbuckle, FastEndpoints, MediatR/Mediator/Cortex.Mediator,
  AutoMapper/Mapster/Mapperly, el proveedor InMemory de EF Core, SQLite, xUnit v2,
  `Microsoft.NET.Test.Sdk` y `xunit.runner.visualstudio`.

## Flujo de desarrollo y puertas de calidad

- Flujo de Spec Kit: `/speckit-specify` → `/speckit-clarify` (si hay ambigüedades) →
  `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`. Todos los artefactos se
  redactan en español.
- El «Constitution Check» de cada plan DEBE evaluar cada principio. Toda violación DEBE
  justificarse en Complexity Tracking o, si no puede justificarse, eliminarse del plan.
- Antes de integrar cambios en `main` DEBEN superarse, en local, estas puertas:
  1. `dotnet build MiRestaurante.slnx` sin errores ni advertencias.
  2. `dotnet format --verify-no-changes MiRestaurante.slnx`.
  3. `dotnet test --solution MiRestaurante.slnx --coverlet --coverlet-output-format cobertura`
     con todas las pruebas en verde, incluidas las de arquitectura (requiere Docker).
  4. ReportGenerator sobre los informes Cobertura con
     `minimumCoverageThresholds:lineCoverage=80`, terminando con código de salida 0.
- La mutación con Stryker.NET es un paso informativo de revisión, no una puerta de calidad.
  El job `mutacion` DEBERÍA ejecutarla únicamente sobre los archivos `src/**/*.cs` cambiados
  frente a `main`. NO DEBE bloquear la integración y DEBE publicar la puntuación y los
  mutantes supervivientes en el resumen del job y como artefacto. Cada mutante superviviente
  DEBE clasificarse como prueba débil, mutante equivalente o código sobrante.
- Un workflow de GitHub Actions DEBE ejecutarse en cada pull request hacia `main` y en cada
  push a `main`. Su job `calidad` DEBE ejecutar las mismas cuatro puertas locales y bloquear
  la integración si alguna falla.
- La rama `main` DEBE protegerse en GitHub con un ruleset que exija pull request y que el
  check `calidad` esté en verde.
- Los commits DEBERÍAN seguir Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`,
  `test:`…), con la descripción en español.

## Gobernanza

- Esta constitución prevalece sobre cualquier otra práctica, guía o plantilla del
  repositorio. `CLAUDE.md` y demás guías de ejecución DEBEN mantenerse coherentes con ella;
  ante una discrepancia, se corrige la guía.
- **Enmiendas**: se hacen con `/speckit-constitution` sobre `.specify/memory/constitution.md`,
  con un Sync Impact Report, la aprobación del responsable del proyecto y un commit
  `docs: amend constitution to vX.Y.Z (...)`.
- **Versionado** (SemVer):
  - MAJOR: eliminar o redefinir de forma incompatible un principio o una regla de gobierno.
  - MINOR: añadir un principio o una sección, o ampliar materialmente una guía.
  - PATCH: aclaraciones, redacción o actualización de la columna informativa de versiones.
- **Cumplimiento**: todo plan pasa el Constitution Check, toda revisión verifica los
  principios y `MiRestaurante.Architecture.Tests` automatiza el principio II. Un cambio que
  viole un principio sin justificación en Complexity Tracking NO DEBE integrarse.

**Versión**: 1.3.0 | **Ratificada**: 2026-11-04 | **Última enmienda**: 2026-11-06
