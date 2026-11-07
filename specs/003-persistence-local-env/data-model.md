# Modelo de datos: Persistencia y entorno local

Esta feature no crea entidades ni tablas de negocio. Describe los elementos de persistencia que introduce y su configuración.

## Elementos de persistencia

| Elemento | Capa | Descripción | Reglas |
| -------- | ---- | ----------- | ------ |
| `IApplicationDbContext` | Application | Contrato que usarán los servicios de aplicación. Solo `Task<int> SaveChangesAsync(CancellationToken)`. | No expone `DbSet<T>` hasta que una feature añada entidades. Sin referencias a proveedores. |
| `ApplicationDbContext` | Infrastructure | `DbContext` con el proveedor Npgsql; implementa `IApplicationDbContext`. | Sin `DbSet` ni configuraciones de entidad. Se resuelve a la misma instancia que la interfaz en cada ámbito. |
| `InitialCreate` | Infrastructure (`Migrations/`) | Primera migración, vacía (`Up` y `Down` sin operaciones). | Generada por `dotnet ef`; no se edita. |
| `ApplicationDbContextModelSnapshot` | Infrastructure (`Migrations/`) | Instantánea del modelo (vacío). | Generada; el modelo no debe tener cambios pendientes respecto a ella. |
| `__EFMigrationsHistory` | Base de datos | Única tabla tras la feature: `MigrationId` (clave) y `ProductVersion`. | La crea EF al aplicar migraciones. Respawn la excluirá cuando se integre. |

## Relaciones

```text
Api ──(AddInfrastructure, MigrateDatabaseAsync)──▶ Infrastructure
Infrastructure: ApplicationDbContext ──implementa──▶ Application: IApplicationDbContext
Infrastructure: Migrations ──describen──▶ esquema de PostgreSQL
```

Los controladores no dependen de `IApplicationDbContext` (FR-025); los servicios de aplicación futuros sí.

## Estados de `/health`

| Estado de la base de datos | Respuesta | Cuerpo |
| -------------------------- | --------- | ------ |
| Accesible | `200` | `Healthy` |
| Inaccesible (servidor caído, base inexistente) | `503` | `Unhealthy` |
| Sin cadena de conexión | `503` | `Unhealthy` |

Todas con `Content-Type: text/plain`. Ver [contracts/persistence-local-env.md](contracts/persistence-local-env.md).

## Comportamiento al arrancar según entorno

| Entorno | Migraciones al arrancar | Base inaccesible al arrancar |
| ------- | ----------------------- | ---------------------------- |
| Development | Se aplican todas las pendientes antes de atender peticiones; las ya registradas no se repiten | El arranque falla; EF registra un Error y la excepción no se captura |
| Cualquier otro | No se aplican | La API arranca; `/health` responde 503 |

## Configuración

| Clave | Dónde | Valor |
| ----- | ----- | ----- |
| `ConnectionStrings:DefaultConnection` | `appsettings.Development.json` | `Host=localhost;Port=5432;Database=mirestaurante;Username=mirestaurante;Password=mirestaurante_dev` (solo desarrollo local) |
| `ConnectionStrings__DefaultConnection` | Variable de entorno, fuera de Development | Sin versionar |
| — | `appsettings.json` | Sin cadena de conexión |
