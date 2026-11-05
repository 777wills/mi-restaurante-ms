# Contrato: `/health` y configuración local

Interfaces que expone la feature a quien opera la API y al desarrollador.

## `GET /health`

- **Disponibilidad**: todos los entornos; ruta exacta `/health`, fuera del prefijo `/api/v1/`; sin autenticación.
- **Comprobación**: una sola, la conexión a la base de datos mediante `AddDbContextCheck<ApplicationDbContext>`.
- **Formato**: texto plano por defecto del middleware de health checks; no es ProblemDetails.

| Situación | Estado | `Content-Type` | Cuerpo |
| --------- | ------ | -------------- | ------ |
| Base de datos accesible | 200 | `text/plain` | `Healthy` |
| Base de datos inaccesible o sin cadena de conexión | 503 | `text/plain` | `Unhealthy` |

El cuerpo no incluye mensajes de error, nombres de servidor ni detalles de la excepción.

## Entorno local (`compose.yaml`)

| Propiedad | Valor |
| --------- | ----- |
| Servicios | Uno: `postgres` |
| Imagen | `postgres:18` (etiqueta de versión mayor; nunca `latest`) |
| Puerto | `5432` de la máquina local → `5432` del contenedor |
| Datos | Volumen con nombre `postgres-data` montado en `/var/lib/postgresql` |
| Credenciales | Base `mirestaurante`, usuario `mirestaurante`, contraseña `mirestaurante_dev` (solo desarrollo local) |
| `healthcheck` | Ninguno |

## Cadena de conexión

- Nombre: `DefaultConnection` (sección `ConnectionStrings`).
- Development: definida en `appsettings.Development.json`, apunta al contenedor anterior.
- Fuera de Development: se lee de la configuración estándar, p. ej. `ConnectionStrings__DefaultConnection`; no se versiona.
- `appsettings.json` no contiene ninguna.

## Herramientas

- `dotnet ef` (herramienta local 10.0.14): proyecto de migraciones `src/MiRestaurante.Infrastructure`, proyecto de arranque `src/MiRestaurante.Api`.
- Script para entornos distintos de Development: `dotnet ef migrations script --idempotent --project src/MiRestaurante.Infrastructure --startup-project src/MiRestaurante.Api`.
