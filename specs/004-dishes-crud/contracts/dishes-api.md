# Contrato: API de platos

Recurso `/api/v1/dishes`. Todas las respuestas de error siguen el formato ProblemDetails de la feature 002 ([contrato de convenciones HTTP](../../002-http-conventions/contracts/http-conventions.md)): `application/problem+json`, `title` en español, `status` y `traceId`. Los errores de validación incluyen además `errors`, y los 404 y 422 de negocio, `detail`. No hay autenticación.

## Representación de un plato (respuesta)

```json
{
  "id": 2,
  "name": "Ajiaco santafereño",
  "description": "Sopa de tres papas con pollo, mazorca y guascas, servida con crema, alcaparras y aguacate.",
  "price": 32000.00,
  "imageUrl": "https://upload.wikimedia.org/wikipedia/commons/d/d7/Ajiaco.jpg",
  "isAvailable": true,
  "createdAt": "2026-11-04T00:00:00+00:00",
  "updatedAt": "2026-11-04T00:00:00+00:00"
}
```

- Exactamente estas 8 propiedades (FR-007). `description` e `imageUrl` son `null` cuando no tienen valor. Nunca se incluye la fecha de eliminación.
- `price`: número en pesos colombianos con 2 decimales como máximo.
- `createdAt` / `updatedAt`: ISO 8601 en UTC (offset `+00:00`).

## Cuerpo de entrada (`POST` y `PUT`)

```json
{
  "name": "Ajiaco santafereño",
  "description": "Sopa de tres papas…",
  "price": 32000,
  "imageUrl": "https://upload.wikimedia.org/wikipedia/commons/d/d7/Ajiaco.jpg",
  "isAvailable": true
}
```

| Campo | Obligatorio | Reglas (se evalúan sobre el valor recibido) | Al guardar |
| --- | --- | --- | --- |
| `name` | Sí | Al menos un carácter distinto de espacio; ≤ 100 caracteres | Se recorta |
| `description` | No | ≤ 500 caracteres | Se recorta; vacía o solo con espacios → `null` |
| `price` | Sí | > 0; ≤ 99 999 999,99; como máximo 2 decimales significativos (`1000.550` es válido, `1000.555` no) | Tal cual |
| `imageUrl` | No | ≤ 2048 caracteres; una vez recortada, URL absoluta con esquema `http` o `https` | Se recorta; vacía o solo con espacios → `null` |
| `isAvailable` | Sí | `true` o `false` | Tal cual |

Un campo obligatorio ausente o `null` responde 400; no se aplican valores por defecto. Los campos desconocidos se ignoran.

## Endpoints

### `GET /api/v1/dishes`

Lista los platos activos, ordenados alfabéticamente por nombre sin distinguir mayúsculas (orden español: «n» < «ñ» < «o»). Sin paginación.

| Parámetro de consulta | Valores | Efecto |
| --- | --- | --- |
| `available` (opcional) | `true` / `false`, sin distinguir mayúsculas | Solo los platos con esa disponibilidad |
| | vacío (`?available=`) o ausente | Sin filtro |
| | cualquier otro (`1`, `si`…) | 400 |

| Código | Cuando | Cuerpo |
| --- | --- | --- |
| 200 | Siempre que el filtro sea válido | Array de platos (puede estar vacío) |
| 400 | Valor del filtro no válido | ProblemDetails con `errors.available` |

### `GET /api/v1/dishes/{id}`

| Código | Cuando | Cuerpo |
| --- | --- | --- |
| 200 | El plato existe y está activo | Plato |
| 404 | No existe, está eliminado, o `id` no es un entero (`abc`, desbordamiento), es 0 o negativo | ProblemDetails |

### `POST /api/v1/dishes`

| Código | Cuando | Cabeceras / cuerpo |
| --- | --- | --- |
| 201 | Datos válidos y nombre libre | `Location: …/api/v1/dishes/{id}` (URL absoluta); cuerpo: el plato creado, con `createdAt` = `updatedAt` = hora actual |
| 400 | Cuerpo ausente, JSON mal formado o datos inválidos | ProblemDetails con `errors` (un mensaje en español por regla incumplida) |
| 422 | Otro plato activo tiene el mismo nombre recortado sin distinguir mayúsculas (también si pierde una carrera) | ProblemDetails con `detail` |

### `PUT /api/v1/dishes/{id}`

Reemplazo completo con el mismo cuerpo que `POST`. Fija `updatedAt` en la hora actual aunque no cambie nada, y conserva `createdAt`.

| Código | Cuando | Cuerpo |
| --- | --- | --- |
| 200 | Datos válidos, plato activo y nombre libre (el propio plato no cuenta) | Plato actualizado |
| 400 | Datos inválidos (se evalúa primero) | ProblemDetails con `errors` |
| 404 | Plato inexistente o eliminado (se evalúa antes que la unicidad) | ProblemDetails con `detail` |
| 422 | Nombre en uso por otro plato activo | ProblemDetails con `detail` |

### `DELETE /api/v1/dishes/{id}`

Borrado lógico: fija la fecha de eliminación y conserva el registro.

| Código | Cuando | Cuerpo |
| --- | --- | --- |
| 204 | El plato estaba activo | Vacío |
| 404 | Inexistente o ya eliminado | ProblemDetails con `detail` |

## Mensajes

### Mensajes de validación

Aparecen en `errors`, bajo la clave del campo: `Name`, `Description`, `Price`, `ImageUrl` e `IsAvailable` para el cuerpo (como en la feature 002), y `available` para la consulta.

| Clave | Regla incumplida | Mensaje |
| --- | --- | --- |
| `Name` | Ausente, vacío o solo con espacios | El nombre es obligatorio. |
| `Name` | Más de 100 caracteres | El nombre no puede superar los 100 caracteres. |
| `Description` | Más de 500 caracteres | La descripción no puede superar los 500 caracteres. |
| `Price` | Ausente o `null` | El precio es obligatorio. |
| `Price` | ≤ 0 o > 99 999 999,99 | El precio debe ser mayor que 0 y menor o igual que 99.999.999,99. |
| `Price` | Más de 2 decimales significativos | El precio admite como máximo 2 decimales. |
| `ImageUrl` | Más de 2048 caracteres | La URL de la imagen no puede superar los 2048 caracteres. |
| `ImageUrl` | No es una URL absoluta `http`/`https` | La URL de la imagen debe ser una URL absoluta http o https. |
| `IsAvailable` | Ausente o `null` | La disponibilidad es obligatoria. |
| `available` | Valor distinto de `true`/`false`/vacío | El valor indicado no es válido. |

Un campo que incumple varias reglas a la vez (por ejemplo, `-1.555`) recibe un mensaje por cada regla. Los errores de formato del JSON siguen las reglas de la feature 002 (mensaje genérico, sin detalles internos).

### Mensajes de negocio (`detail`)

| Código | Mensaje |
| --- | --- |
| 404 | No existe el plato {id}. |
| 422 | Ya existe un plato con el nombre «{nombre recortado}». |

Los 404 de rutas que no coinciden (`/api/v1/dishes/abc`) no llevan `detail`, igual que cualquier ruta inexistente (feature 002).

## Documento OpenAPI

Cada acción declara sus códigos con `[ProducesResponseType]` (200/201/204/400/404/422 según las tablas anteriores). Solo está disponible en Development, en `/openapi/v1.json` y en la interfaz Scalar (`/scalar`).
