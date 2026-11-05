# Especificación de la feature: Persistencia y entorno local

**Rama de la feature**: `speckit-test`

**Creada**: 2026-11-04

**Estado**: Borrador

**Entrada**: descripción del usuario: «Persistencia y entorno local. (1) `compose.yaml` solo con `postgres:18` (IX). (2) Cadena de conexión en `appsettings.Development.json` (IX). (3) Paquetes (V): `Microsoft.EntityFrameworkCore` en Application; `Npgsql.EntityFrameworkCore.PostgreSQL` en Infrastructure; `Microsoft.EntityFrameworkCore.Design` en Api, que es el proyecto de arranque para las migraciones. (4) `IApplicationDbContext` en Application y el `DbContext` concreto en Infrastructure (V). (5) Migraciones aplicadas automáticamente al arrancar, solo en Development (V). (6) Endpoint `/health` con `AddDbContextCheck` (VI). (7) Infraestructura de pruebas con `Testcontainers.PostgreSql` y `Respawn` en IntegrationTests y FunctionalTests (VII): el esquema se crea con las migraciones reales; Respawn limpia todo salvo `__EFMigrationsHistory`; las pruebas funcionales usan `WebApplicationFactory`. (8) Prueba de arquitectura: los controladores no pueden depender de `IApplicationDbContext` (II).»

> **Nota sobre el nivel de detalle**: esta feature es de infraestructura y no contiene código de negocio. Sus «usuarios» son los desarrolladores del repositorio y quien opera la API. Los mecanismos y paquetes que se nombran (PostgreSQL 18, Docker Compose, EF Core, migraciones, `IApplicationDbContext`, `AddDbContextCheck`, Testcontainers, `WebApplicationFactory`) no son decisiones de implementación: son obligaciones fijadas en la constitución (principios II, V, VI, VII y IX y la tabla del stack). Por eso se citan literalmente.

## Aclaraciones

### Sesión 2026-11-04 (entrevista previa)

- P: ¿Esta feature crea alguna tabla? → R: No. No se añade ninguna entidad: `IApplicationDbContext` no expone conjuntos de entidades todavía. Se genera una migración inicial vacía para verificar de extremo a extremo la tubería de migraciones.
- P: Como no hay tablas, Respawn no puede ejecutarse (lanza «No tables found» si solo existe `__EFMigrationsHistory`). ¿Qué se hace con él? → R: Se pospone a la primera feature que cree tablas. En esta feature entran Testcontainers y la creación del esquema con las migraciones reales.
- P: ¿La puerta de cobertura del 80 % es obligatoria? → R: Sí, obligatoria y sin exclusiones (el código generado de las migraciones también cuenta).
- P: ¿Qué cuerpo devuelve `/health`? → R: El texto plano por defecto: `Healthy` con 200 y `Unhealthy` con 503.
- P: ¿Cómo identifica la prueba de arquitectura a los controladores? → R: Por herencia de `ControllerBase`, como las pruebas de convención de la feature 002. La prueba existente de dependencia de Infrastructure no se modifica.
- P: ¿Cómo es el servicio de `compose.yaml`? → R: Puerto 5432, volumen con nombre y sin `healthcheck`.
- P: Si en Development la base de datos no está disponible al arrancar, ¿qué pasa? → R: El arranque falla y la excepción queda registrada.
- P: Fuera de Development, si falta la cadena de conexión, ¿qué pasa? → R: Comportamiento por defecto: la API arranca y `/health` responde 503.

## Escenarios de usuario y pruebas _(obligatorio)_

### Historia de usuario 1: base de datos local con un solo comando (Prioridad: P1)

Como desarrollador, quiero levantar PostgreSQL con un único comando de Docker Compose y que la API, al ejecutarla en Development, se conecte a ella y cree o actualice su esquema sola, para empezar a trabajar sin instalar ni configurar nada más.

**Por qué esta prioridad**: sin una base de datos local reproducible ninguna feature de negocio puede ejecutarse ni depurarse. Es el primer paso de cualquier trabajo con persistencia.

**Prueba independiente**: con Docker disponible, se levanta el entorno con Docker Compose, se arranca la API en Development y se comprueba que el historial de migraciones contiene la migración inicial y que `/health` responde `Healthy`.

**Escenarios de aceptación**:

1. **Dado** un clon limpio del repositorio con Docker disponible, **Cuando** el desarrollador levanta el entorno con Docker Compose, **Entonces** se inicia un único contenedor, de PostgreSQL 18, accesible en el puerto 5432 de la máquina local.
2. **Dado** el contenedor en marcha, **Cuando** se arranca la API en Development sin configuración adicional, **Entonces** la API usa la cadena de conexión de desarrollo y aplica todas las migraciones pendientes antes de atender peticiones.
3. **Dado** un arranque previo que ya aplicó las migraciones, **Cuando** se vuelve a arrancar la API en Development, **Entonces** arranca sin errores y sin volver a aplicar las migraciones ya registradas.
4. **Dado** el contenedor detenido con `docker compose down` y vuelto a levantar, **Cuando** se arranca la API, **Entonces** los datos y el historial de migraciones anteriores se conservan.
5. **Dado** que el contenedor no está en marcha, **Cuando** se arranca la API en Development, **Entonces** el arranque falla y el error queda registrado.

---

### Historia de usuario 2: comprobación de salud de la API y su base de datos (Prioridad: P1)

Como quien opera la API, quiero un endpoint de salud que indique si la API puede comunicarse con su base de datos, para detectar problemas de conexión o configuración sin depender de una petición de negocio.

**Por qué esta prioridad**: el principio VI lo exige y es la única señal de que la configuración de la base de datos es correcta fuera de Development, donde las migraciones no se aplican solas.

**Prueba independiente**: se arranca la API con una base de datos accesible y sin ella, y se pide `/health` en cada caso.

**Escenarios de aceptación**:

1. **Dada** la API con la base de datos accesible, **Cuando** se pide `/health`, **Entonces** responde 200 con el cuerpo de texto `Healthy`.
2. **Dada** la API con la base de datos inaccesible o sin cadena de conexión, **Cuando** se pide `/health`, **Entonces** responde 503 con el cuerpo de texto `Unhealthy`, sin mensajes de error ni detalles internos.
3. **Dada** la API en cualquier entorno, **Cuando** se pide `/health`, **Entonces** el endpoint está disponible en esa ruta exacta, fuera del prefijo `/api/v1/`.

---

### Historia de usuario 3: migraciones solo automáticas en Development (Prioridad: P2)

Como responsable del proyecto, quiero que las migraciones se apliquen solas únicamente en Development, para que ningún otro entorno cambie su esquema sin un script revisado.

**Por qué esta prioridad**: protege los entornos distintos de Development, pero hoy no hay ninguno desplegado.

**Prueba independiente**: se arranca la API en Development y en otro entorno contra una base de datos vacía y se comprueba en cuál se creó el historial de migraciones.

**Escenarios de aceptación**:

1. **Dada** una base de datos vacía, **Cuando** la API arranca en Development, **Entonces** al terminar el arranque el historial de migraciones contiene todas las migraciones del proyecto.
2. **Dada** una base de datos vacía, **Cuando** la API arranca en un entorno distinto de Development, **Entonces** al terminar el arranque la base de datos sigue sin historial de migraciones.
3. **Dado** el proyecto con sus migraciones, **Cuando** se genera el script idempotente de migraciones con la herramienta local de EF Core, **Entonces** se obtiene sin errores.

---

### Historia de usuario 4: infraestructura de pruebas contra PostgreSQL real (Prioridad: P2)

Como desarrollador, quiero que los proyectos de pruebas de integración y funcionales dispongan de una base de datos PostgreSQL real y efímera, con el esquema creado por las migraciones reales, para que la primera feature de negocio solo tenga que escribir sus pruebas.

**Por qué esta prioridad**: el principio VII prohíbe sustitutos en memoria. La infraestructura debe existir antes de la primera feature de negocio, pero hoy no hay datos que probar.

**Prueba independiente**: con Docker disponible, se ejecutan las pruebas de integración y funcionales sin ningún contenedor levantado manualmente y se comprueba que crean su propia base de datos, la migran y pasan.

**Escenarios de aceptación**:

1. **Dado** Docker disponible y ningún contenedor levantado a mano, **Cuando** se ejecutan las pruebas de integración, **Entonces** se crea una base de datos PostgreSQL 18 efímera, se le aplican las migraciones reales y las pruebas pasan.
2. **Dada** una base de datos recién migrada en las pruebas de integración, **Cuando** se compara el modelo de datos con las migraciones, **Entonces** no hay cambios del modelo sin migración.
3. **Dada** una base de datos recién migrada, **Cuando** se resuelve `IApplicationDbContext` con la configuración de la aplicación, **Entonces** se obtiene el contexto de datos concreto conectado a esa base de datos.
4. **Dado** Docker disponible, **Cuando** se ejecutan las pruebas funcionales, **Entonces** la API arranca con `WebApplicationFactory` contra una base de datos PostgreSQL 18 efímera y todas las pruebas funcionales existentes (features 001 y 002) siguen pasando.
5. **Dada** la suite completa, **Cuando** termina, **Entonces** no queda ningún contenedor de pruebas en marcha.

---

### Historia de usuario 5: los controladores no acceden directamente a los datos (Prioridad: P3)

Como responsable del proyecto, quiero que una prueba automática impida que un controlador dependa de `IApplicationDbContext`, para que el acceso a datos pase siempre por los servicios de aplicación.

**Por qué esta prioridad**: aún no hay controladores de negocio, pero la regla debe estar activa antes de que llegue el primero.

**Prueba independiente**: se añade temporalmente un controlador que depende de `IApplicationDbContext` y la suite de arquitectura falla indicándolo. Al quitarlo, la suite pasa.

**Escenarios de aceptación**:

1. **Dados** los controladores de la API, **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** pasan, incluso si no existe ningún controlador.
2. **Dado** un controlador, en cualquier namespace, que depende de `IApplicationDbContext`, **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** fallan e indican el controlador.

---

### Casos límite

- **Base de datos inaccesible al arrancar en Development**: el arranque falla con el error registrado; no se captura para seguir arrancando.
- **Cadena de conexión ausente fuera de Development**: la API arranca y `/health` responde 503 `Unhealthy`; no se valida al arrancar.
- **Puerto 5432 ocupado**: Docker Compose no puede publicar el puerto y avisa. Cambiar el puerto queda en manos del desarrollador; no se automatiza.
- **Recreación completa del volumen**: `docker compose down -v` borra los datos; al volver a arrancar en Development, las migraciones reconstruyen el esquema.
- **Respuesta de salud no sana**: el cuerpo es `Unhealthy` en texto plano. No se convierte en ProblemDetails ni expone la excepción de la base de datos.
- **Docker no disponible**: las pruebas de integración y funcionales fallan con el error de Testcontainers. No se omiten ni se sustituyen por otra base de datos.
- **Pruebas de arquitectura sin controladores**: la nueva regla DEBE ejecutarse y pasar sobre el conjunto vacío, no desactivarse. La fase Red se demuestra con un controlador temporal que se revierte antes de integrar.
- **Controlador que depende del contexto concreto de Infrastructure**: ya lo cubre la prueba de arquitectura existente sobre Infrastructure; esta feature no la modifica.

## Requisitos _(obligatorio)_

### Requisitos funcionales

**Entorno local (principio IX)**

- **FR-001**: El repositorio DEBE incluir un `compose.yaml` en su raíz que levante únicamente un servicio de PostgreSQL con la imagen oficial `postgres` y la etiqueta de versión mayor `18` (no `latest`).
- **FR-002**: El servicio DEBE publicar el puerto 5432 en la máquina local y guardar los datos en un volumen con nombre. No DEBE incluir `healthcheck` ni otros servicios.
- **FR-003**: El servicio DEBE definir credenciales y nombre de base de datos exclusivamente locales de desarrollo.
- **FR-004**: `appsettings.Development.json` DEBE contener la cadena de conexión de desarrollo, con nombre `DefaultConnection`, apuntando a ese contenedor con esas credenciales. `appsettings.json` NO DEBE contener ninguna cadena de conexión.
- **FR-005**: Fuera de Development, la cadena de conexión DEBE leerse de la configuración estándar (por ejemplo, la variable de entorno `ConnectionStrings__DefaultConnection`) y NO DEBE versionarse.

**Acceso a datos (principios II y V)**

- **FR-006**: Application DEBE declarar `IApplicationDbContext` con `SaveChangesAsync(CancellationToken)`. No expone conjuntos de entidades en esta feature.
- **FR-007**: Infrastructure DEBE contener el contexto de datos concreto, que implementa `IApplicationDbContext` con el proveedor de PostgreSQL.
- **FR-008**: El contexto concreto DEBE registrarse en el contenedor de dependencias desde la raíz de composición, y `IApplicationDbContext` DEBE resolverse a esa misma instancia dentro de cada ámbito.
- **FR-009**: Cada capa DEBE recibir únicamente sus paquetes: `Microsoft.EntityFrameworkCore` en Application; `Npgsql.EntityFrameworkCore.PostgreSQL` y `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` en Infrastructure; y `Microsoft.EntityFrameworkCore.Design` en Api, que es el proyecto de arranque de las migraciones. Las versiones son las de la tabla del stack de la constitución.
- **FR-010**: Application DEBE seguir cumpliendo la lista blanca de ensamblados del principio II (prueba existente sin cambios).
- **FR-011**: Las migraciones DEBEN versionarse en Infrastructure. Esta feature DEBE incluir una migración inicial, vacía porque aún no hay entidades.
- **FR-012**: La herramienta local `dotnet-ef` DEBE declararse en el manifiesto de herramientas locales del repositorio, con la versión de la tabla del stack.

**Migraciones al arrancar (principio V)**

- **FR-013**: En Development, la API DEBE aplicar las migraciones pendientes al iniciar, antes de empezar a atender peticiones.
- **FR-014**: En cualquier otro entorno, la API NO DEBE aplicar migraciones al iniciar.
- **FR-015**: Si en Development la aplicación de migraciones falla, el arranque DEBE fallar y el error DEBE quedar registrado. No se captura la excepción.

**Endpoint de salud (principio VI)**

- **FR-016**: La API DEBE exponer `/health` en todos los entornos, fuera del prefijo `/api/v1/`, con una única comprobación: la conexión a la base de datos mediante `AddDbContextCheck` sobre el contexto concreto.
- **FR-017**: `/health` DEBE responder 200 con el cuerpo de texto `Healthy` cuando la base de datos es accesible, y 503 con el cuerpo de texto `Unhealthy` cuando no lo es o falta la cadena de conexión. El cuerpo NO DEBE incluir mensajes de error ni detalles internos.

**Infraestructura de pruebas (principio VII)**

- **FR-018**: `MiRestaurante.Infrastructure.IntegrationTests` y `MiRestaurante.Api.FunctionalTests` DEBEN crear sus bases de datos con `Testcontainers.PostgreSql`, usando la misma imagen y versión mayor que `compose.yaml` (`postgres:18`).
- **FR-019**: El esquema de esas bases de datos DEBE crearse aplicando las migraciones reales de EF Core. NO DEBE crearse con `EnsureCreated` ni con DDL escrito a mano. La prohibición se refiere al esquema de la aplicación: crear una base de datos vacía por prueba con `CREATE DATABASE` (aislamiento) no cuenta como DDL escrito a mano.
- **FR-020**: Las pruebas funcionales DEBEN arrancar la API con `WebApplicationFactory`, sustituyendo la cadena de conexión por la del contenedor de pruebas, tanto en Development como en el otro entorno que ya usan.
- **FR-021**: Las pruebas de integración DEBEN verificar, sobre una base de datos recién migrada, que el historial contiene todas las migraciones del proyecto y que el modelo no tiene cambios sin migración.
- **FR-022**: Las pruebas funcionales DEBEN verificar FR-013, FR-014 y FR-017 (las dos respuestas de `/health`).
- **FR-023**: Respawn NO se integra en esta feature: se añadirá en la primera feature que cree tablas, configurado para limpiar todas las tablas salvo `__EFMigrationsHistory`.
- **FR-024**: Las pruebas existentes de las features 001 y 002 DEBEN seguir pasando sin cambiar sus aserciones.

**Arquitectura (principio II)**

- **FR-025**: `MiRestaurante.Architecture.Tests` DEBE verificar que ningún tipo de la API que herede de `ControllerBase`, sea cual sea su namespace, depende de `IApplicationDbContext`. El mensaje de fallo DEBE indicar los controladores que incumplen y la prueba DEBE pasar cuando no exista ningún controlador.

**Calidad (principios VII y VIII)**

- **FR-026**: Todas las pruebas nuevas DEBEN seguir TDD, la nomenclatura `{Metodo}_Should_{Resultado}_When_{Escenario}`, una sola aserción lógica y ninguna lógica condicional.
- **FR-027**: La puerta de cobertura (≥ 80 % de líneas sobre los cuatro ensamblados de producción) es obligatoria en esta feature, sin excluir el código generado de las migraciones.

### Entidades clave

- **Contexto de datos de la aplicación (`IApplicationDbContext`)**: contrato que usan los servicios de aplicación para guardar cambios. En esta feature solo ofrece guardar cambios; las features de negocio le añadirán sus conjuntos de entidades.
- **Historial de migraciones (`__EFMigrationsHistory`)**: registro, en la propia base de datos, de las migraciones aplicadas. Es la única tabla que existe tras esta feature.
- **Migración inicial**: primera migración versionada, sin cambios de esquema; sirve de punto de partida y para verificar la tubería.

## Criterios de éxito _(obligatorio)_

### Resultados medibles

- **SC-001**: Desde un clon limpio, con Docker disponible, un desarrollador tiene la base de datos y la API funcionando con 2 comandos (levantar Docker Compose y ejecutar la API), sin editar ningún archivo de configuración.
- **SC-002**: `/health` responde `Healthy` en el 100 % de las comprobaciones con la base de datos accesible y `Unhealthy` con 503 en el 100 % de las comprobaciones sin ella, sin exponer detalles internos.
- **SC-003**: Tras arrancar contra una base de datos vacía, el historial de migraciones contiene el 100 % de las migraciones del proyecto en Development y 0 en el otro entorno.
- **SC-004**: La suite completa se ejecuta sin levantar ningún contenedor a mano y no deja contenedores de prueba en marcha al terminar.
- **SC-005**: En el 100 % de los intentos, un controlador temporal que depende de `IApplicationDbContext` hace fallar la suite de arquitectura.
- **SC-006**: La solución compila sin advertencias, `dotnet format --verify-no-changes` no detecta cambios, toda la suite termina en verde, la cobertura de líneas es ≥ 80 % y ReportGenerator termina con código de salida 0.

## Supuestos

- La rama de trabajo es la actual (`speckit-test`); no se crea una rama nueva.
- Las versiones son las de la tabla del stack de la constitución: EF Core, `Microsoft.EntityFrameworkCore.Design` y `dotnet-ef` 10.0.14; `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3; `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 10.0.14 (necesario para `AddDbContextCheck`); PostgreSQL 18; `Testcontainers.PostgreSql` 4.16.0.
- Las pruebas de integración y funcionales requieren Docker, como ya fija la puerta de calidad 3.
- La convención de nombres de tablas y columnas es la predeterminada del proveedor; no se añade ningún paquete de convenciones de nombres.
- El endpoint `/health` no requiere autenticación, ya que no existe autenticación en el proyecto.
- Quedan fuera del alcance: entidades, conjuntos de entidades y datos semilla (`HasData`) y su prueba; Respawn (FR-023); controladores y servicios de negocio; el script idempotente para despliegues (solo se comprueba que se genera); la contenerización de la API; cambiar la prueba de arquitectura existente que filtra por namespace; y cualquier personalización del formato de `/health`.
