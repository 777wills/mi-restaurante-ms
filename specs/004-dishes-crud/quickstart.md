# Guía de validación: CRUD de platos

Escenarios para comprobar, desde la raíz del repositorio, que la feature está completa. No contiene implementación: las decisiones están en [research.md](research.md), el diseño en [plan.md](plan.md), el modelo en [data-model.md](data-model.md) y el contrato HTTP en [contracts/dishes-api.md](contracts/dishes-api.md).

## Requisitos previos

- SDK de .NET indicado en `global.json`.
- Docker disponible, con el puerto 5432 libre.
- `dotnet tool restore` ejecutado una vez.
- `curl` (los ejemplos usan el perfil `http`: `http://localhost:5110`).

## 1. Compilación y formato

```bash
dotnet build MiRestaurante.slnx
dotnet format --verify-no-changes MiRestaurante.slnx
```

**Esperado**: 0 advertencias y 0 errores; ningún cambio de formato pendiente (incluida la migración `AddDishes`, que se versiona tal como la genera `dotnet ef`).

## 2. Menú inicial (US5, SC-001)

```bash
docker compose up -d
dotnet run --project src/MiRestaurante.Api --launch-profile http
# en otra terminal, tras el arranque
curl -s http://localhost:5110/api/v1/dishes
curl -s 'http://localhost:5110/api/v1/dishes?available=true'
```

**Esperado**: la API aplica `AddDishes` al arrancar en Development, sin pasos manuales. El primer listado devuelve los 5 platos semilla en orden alfabético («Ajiaco santafereño», «Arepa de huevo», «Bandeja paisa», «Lechona», «Sancocho de hueso»); el segundo devuelve 4 (sin la lechona).

## 3. Consultas y filtro (US1)

```bash
curl -s 'http://localhost:5110/api/v1/dishes?available=FALSE'      # 200, solo la lechona
curl -s 'http://localhost:5110/api/v1/dishes?available='           # 200, los 5
curl -si 'http://localhost:5110/api/v1/dishes?available=si'        # 400
curl -s http://localhost:5110/api/v1/dishes/1                      # 200, Bandeja paisa
curl -si http://localhost:5110/api/v1/dishes/999                   # 404
curl -si http://localhost:5110/api/v1/dishes/abc                   # 404
```

**Esperado**: los códigos indicados; el 400 es `application/problem+json` con `errors.available` = «El valor indicado no es válido.»; los 404 son ProblemDetails; los platos tienen exactamente las 8 propiedades del [contrato](contracts/dishes-api.md#representación-de-un-plato-respuesta).

## 4. Ciclo completo en 4 peticiones (US2, US3, US4, SC-002)

```bash
curl -si -X POST http://localhost:5110/api/v1/dishes -H 'Content-Type: application/json' \
  -d '{"name":"  Mote de queso ","description":" ","price":18000.50,"imageUrl":null,"isAvailable":true}'
# anotar el id de la cabecera Location (por ejemplo, 6)
curl -s http://localhost:5110/api/v1/dishes/6
curl -s -X PUT http://localhost:5110/api/v1/dishes/6 -H 'Content-Type: application/json' \
  -d '{"name":"Mote de queso","description":"Sopa costeña de ñame con queso.","price":19000,"imageUrl":null,"isAvailable":false}'
curl -si -X DELETE http://localhost:5110/api/v1/dishes/6
```

**Esperado**:

- `POST`: 201, `Location` termina en `/api/v1/dishes/6` (el primer Id libre después de la semilla), `name` = «Mote de queso» (recortado), `description` = `null`, y `createdAt` = `updatedAt`.
- `GET`: 200 con el mismo plato.
- `PUT`: 200, `isAvailable` = `false`, el mismo `createdAt` y un `updatedAt` posterior.
- `DELETE`: 204 sin cuerpo.

Después:

```bash
curl -si http://localhost:5110/api/v1/dishes/6                    # 404
curl -si -X DELETE http://localhost:5110/api/v1/dishes/6          # 404
docker compose exec postgres psql -U mirestaurante -d mirestaurante \
  -c 'select "Id","Name","IsAvailable","DeletedAt" from "Dishes" where "Id" = 6'
```

**Esperado**: los dos 404; la fila sigue existiendo, con sus datos y `DeletedAt` relleno (US4, escenario 2; SC-005).

## 5. Reglas de negocio y validación (US2, US3, SC-003)

```bash
curl -si -X POST http://localhost:5110/api/v1/dishes -H 'Content-Type: application/json' \
  -d '{"name":" LECHONA ","price":1,"isAvailable":true}'                         # 422 (nombre en uso)
curl -si -X POST http://localhost:5110/api/v1/dishes -H 'Content-Type: application/json' \
  -d '{"name":"Léchona","price":1,"isAvailable":true}'                           # 201 (las tildes distinguen)
curl -si -X POST http://localhost:5110/api/v1/dishes -H 'Content-Type: application/json' \
  -d '{"name":"   ","price":1000.555,"imageUrl":"ftp://x/y"}'                     # 400
curl -si -X PUT http://localhost:5110/api/v1/dishes/999 -H 'Content-Type: application/json' \
  -d '{"name":"Lechona","price":1,"isAvailable":true}'                           # 404 (antes que el 422)
```

**Esperado**: los códigos indicados. El 422 trae `detail` «Ya existe un plato con el nombre «LECHONA».». El 400 lista mensajes en español para `Name`, `Price`, `ImageUrl` e `IsAvailable` ([tabla de mensajes](contracts/dishes-api.md#mensajes-de-validación)), y no se crea nada.

## 6. Documento OpenAPI

```bash
curl -s http://localhost:5110/openapi/v1.json | grep -o '"/api/v1/dishes[^"]*"' | sort -u
```

**Esperado**: `"/api/v1/dishes"` y `"/api/v1/dishes/{id}"`. Scalar (`http://localhost:5110/scalar`) muestra las cinco operaciones con sus códigos de respuesta.

## 7. Script idempotente

```bash
dotnet ef migrations script --idempotent --project src/MiRestaurante.Infrastructure --startup-project src/MiRestaurante.Api -o /tmp/mirestaurante.sql
```

**Esperado**: código de salida 0. El script contiene `CREATE COLLATION case_insensitive`, `CREATE TABLE "Dishes"`, los 5 `INSERT` de la semilla, `CREATE UNIQUE INDEX "IX_Dishes_Name" ... WHERE "DeletedAt" IS NULL` y el `setval` de la secuencia, todo dentro de bloques condicionales. Aplicado dos veces sobre una base vacía, no da errores.

## 8. Reglas de arquitectura en Red

Añadir temporalmente a `src/MiRestaurante.Api/Controllers/` un controlador con `[ApiController]`, una ruta `api/v1/...` y `[ProducesResponseType]`, que reciba `DishService` (la clase concreta) en el constructor y devuelva un `Dish`. Ejecutar:

```bash
dotnet test --project tests/MiRestaurante.Architecture.Tests --filter-class "*ControllerServiceDependencyTests"
```

**Esperado**: fallan las dos pruebas y nombran el controlador. Borrar el controlador y repetir: pasan.

## 9. Suite completa, Respawn y contenedores de prueba

Con `docker compose down` ejecutado:

```bash
dotnet test --solution MiRestaurante.slnx
docker ps --filter ancestor=postgres:18
```

**Esperado**: código de salida 0, con todas las pruebas de las features 001 a 003 sin cambios en sus aserciones (FR-031) y las de arquitectura y convenciones sin modificar (FR-027). Las clases que escriben datos reinician la base con Respawn antes de cada prueba, y la prueba de 20 altas simultáneas termina con un 201 y diecinueve 422. Al terminar, `docker ps` no lista ningún contenedor.

## 10. Puerta de cobertura (SC-006)

```bash
dotnet tool restore
rm -rf TestResults
dotnet test --solution MiRestaurante.slnx --results-directory TestResults --coverlet --coverlet-output-format cobertura
dotnet reportgenerator -reports:"TestResults/coverage.cobertura.*.xml" -targetdir:TestResults/report -reporttypes:"TextSummary;Html" "-assemblyfilters:+MiRestaurante.Domain;+MiRestaurante.Application;+MiRestaurante.Infrastructure;+MiRestaurante.Api" minimumCoverageThresholds:lineCoverage=80
```

**Esperado**: ReportGenerator termina con código 0 y `TestResults/report/Summary.txt` indica ≥ 80 % de líneas, sin exclusiones (el prototipo midió 98,1 %).

## Limpieza

Para volver al menú inicial en local: `docker compose down -v && docker compose up -d` y arrancar la API de nuevo.
