# Especificación de la feature: CRUD de platos

**Rama de la feature**: `speckit-test`

**Creada**: 2026-11-04

**Estado**: Aclarada

**Entrada**: descripción del usuario: «crea el crud de platos»

> **Nota sobre el nivel de detalle**: el producto es una API REST, así que sus rutas, códigos de estado y cuerpos de respuesta son la interfaz de usuario y se especifican como tal. Los mecanismos que se nombran (prefijo `/api/v1/`, ProblemDetails, 404/422, `TimeProvider`, `HasData`, Respawn, precio `decimal(10,2)` en COP) no son decisiones de implementación de esta spec: son obligaciones de la constitución (principios III, IV, V, VI y VII).

## Aclaraciones

### Sesión 2026-11-04 (entrevista previa)

- P: ¿Los platos se agrupan por categoría? → R: No. No hay categorías en esta feature.
- P: ¿Qué atributos tiene un plato, además de nombre, precio y disponibilidad? → R: Descripción, URL de imagen y fechas de creación y de última modificación.
- P: ¿Cómo se elimina un plato? → R: Con borrado lógico: el plato queda registrado con su fecha de eliminación (nula = activo).
- P: ¿Cómo se comporta la API con un plato eliminado? → R: Es invisible: consultar, actualizar o eliminar un plato eliminado responde 404. No hay restauración.
- P: ¿El nombre es único? → R: Sí, sin distinguir mayúsculas, solo entre platos no eliminados, y un duplicado responde 422. Las tildes sí distinguen («Ajiaco» ≠ «Ajíaco»).
- P: ¿Cómo es el listado? → R: Todos los platos activos, ordenados por nombre, con un filtro opcional por disponibilidad. Sin paginación.
- P: ¿Cómo se actualiza un plato? → R: Solo con un reemplazo completo (PUT). No hay actualización parcial ni un endpoint específico de disponibilidad.
- P: ¿Qué reglas tienen el nombre y la descripción? → R: Nombre obligatorio de hasta 100 caracteres; descripción opcional de hasta 500. Se recortan los espacios iniciales y finales.
- P: ¿Qué valores admite el precio? → R: Mayor que 0 y hasta 99.999.999,99, con 2 decimales como máximo.
- P: ¿Qué reglas tiene la URL de imagen? → R: Opcional, URL absoluta `http` o `https`, de hasta 2048 caracteres.
- P: ¿La disponibilidad es obligatoria? → R: Sí, al crear y al actualizar.
- P: ¿Qué responden las escrituras? → R: Crear: 201 con `Location` y el plato. Actualizar: 200 con el plato. Eliminar: 204.
- P: ¿Se siembra un menú inicial? → R: Sí, 5 platos típicos colombianos con imágenes de Wikimedia Commons.
- P: ¿Qué fecha de modificación tiene un plato recién creado? → R: La misma que la de creación. Todo PUT la actualiza, aunque no cambie ningún valor.
- P: ¿Qué campos devuelve un plato? → R: Identificador, nombre, descripción, precio, URL de imagen, disponibilidad, fecha de creación y fecha de modificación. Nunca la fecha de eliminación.
- P: ¿Cómo se manejan las escrituras concurrentes? → R: Gana la última escritura. Si dos peticiones simultáneas intentan usar el mismo nombre, la que pierde responde 422, nunca 500.

### Sesión 2026-11-04

- P: ¿Los máximos de 100 (nombre) y 500 (descripción) caracteres se miden sobre el valor recibido o sobre el valor ya recortado? → R: Sobre el valor recibido, antes de recortar.
- P: ¿Una URL de imagen con espacios al principio o al final se recorta y se acepta, o se rechaza con 400? → R: Se recorta antes de validar su formato y se guarda recortada.
- P: ¿Un precio escrito con ceros sobrantes, como 1000.550 o 8000.000, se acepta o se rechaza con 400? → R: Se acepta: cuenta el valor numérico, no los decimales escritos.
- P: ¿Qué responde un PUT con datos válidos sobre un plato inexistente o eliminado si el nombre ya lo usa otro plato activo: 404 o 422? → R: 404; la existencia se comprueba antes que la unicidad.
- P: ¿Qué valores acepta el filtro available: también True/TRUE, y cómo se trata ?available= vacío? → R: `true`/`false` sin distinguir mayúsculas; vacío equivale a sin filtro; cualquier otro valor → 400.

## Escenarios de usuario y pruebas _(obligatorio)_

### Historia de usuario 1: consultar el menú (Prioridad: P1)

Como cliente del restaurante, quiero ver la lista de platos, con la opción de ver solo los disponibles, y consultar el detalle de un plato, para decidir qué pedir.

**Por qué esta prioridad**: es la razón de ser del producto («una API que muestra el menú»). Con los datos semilla, esta historia aporta valor por sí sola, incluso sin las operaciones de escritura.

**Prueba independiente**: sobre una base de datos con platos disponibles, no disponibles y eliminados, se pide el listado con y sin filtro y el detalle de cada plato, y se comprueba qué platos aparecen y en qué orden.

**Escenarios de aceptación**:

1. **Dados** varios platos activos, **Cuando** se pide el listado sin filtro, **Entonces** responde 200 con todos los platos activos, ordenados alfabéticamente por nombre sin distinguir mayúsculas.
2. **Dados** platos disponibles y no disponibles, **Cuando** se pide el listado filtrando por disponibles, **Entonces** solo aparecen los disponibles; y al filtrar por no disponibles, solo los no disponibles.
3. **Dado** un plato eliminado, **Cuando** se pide el listado, con o sin filtro, **Entonces** no aparece.
4. **Dado** que no hay platos activos, **Cuando** se pide el listado, **Entonces** responde 200 con una lista vacía.
5. **Dado** un plato activo, **Cuando** se pide su detalle, **Entonces** responde 200 con todos sus campos.
6. **Dado** un identificador inexistente o de un plato eliminado, **Cuando** se pide su detalle, **Entonces** responde 404 como ProblemDetails.
7. **Dado** un valor del filtro que no es verdadero ni falso (por ejemplo, `1` o `si`), **Cuando** se pide el listado, **Entonces** responde 400 como ProblemDetails.
8. **Dado** el filtro escrito con otras mayúsculas (`True`, `FALSE`) o vacío (`?available=`), **Cuando** se pide el listado, **Entonces** responde 200: con mayúsculas filtra igual que en minúsculas, y vacío devuelve todos los platos activos.

---

### Historia de usuario 2: dar de alta un plato (Prioridad: P1)

Como administrador del restaurante, quiero añadir un plato al menú, para ofrecer nuevas preparaciones.

**Por qué esta prioridad**: sin altas, el menú queda limitado a los datos semilla.

**Prueba independiente**: se crea un plato válido y se comprueba la respuesta, la cabecera `Location` y que el plato aparece después en el listado. Se repite con datos inválidos y con un nombre duplicado.

**Escenarios de aceptación**:

1. **Dados** datos válidos, **Cuando** se crea un plato, **Entonces** responde 201, la cabecera `Location` apunta a `/api/v1/dishes/{id}` y el cuerpo es el plato creado, con su identificador, la fecha de creación igual a la hora actual y la fecha de modificación igual a la de creación.
2. **Dado** un nombre o descripción con espacios al principio o al final, **Cuando** se crea el plato, **Entonces** se guardan sin esos espacios.
3. **Dados** datos que incumplen las reglas de validación (FR-008 a FR-013), **Cuando** se crea el plato, **Entonces** responde 400 como ProblemDetails, con un mensaje en español por cada campo inválido, y no se crea nada.
4. **Dado** un plato activo llamado «Ajiaco», **Cuando** se crea otro llamado «ajiaco» o « AJIACO », **Entonces** responde 422 como ProblemDetails y no se crea nada.
5. **Dado** un plato eliminado llamado «Ajiaco», **Cuando** se crea otro llamado «Ajiaco», **Entonces** responde 201.
6. **Dado** un plato llamado «Ajiaco», **Cuando** se crea otro llamado «Ajíaco», **Entonces** responde 201 (las tildes distinguen).

---

### Historia de usuario 3: modificar un plato (Prioridad: P2)

Como administrador del restaurante, quiero corregir los datos de un plato o marcarlo como no disponible, para que el menú refleje lo que realmente se puede pedir.

**Por qué esta prioridad**: cambiar la disponibilidad es la operación más frecuente en el día a día, pero el menú es útil sin ella.

**Prueba independiente**: se crea un plato, se reemplaza con datos nuevos y se comprueba la respuesta y la consulta posterior. Se repite con datos inválidos, con un nombre de otro plato y con un plato inexistente.

**Escenarios de aceptación**:

1. **Dados** un plato activo y datos válidos, **Cuando** se actualiza, **Entonces** responde 200 con el plato actualizado, la misma fecha de creación y la fecha de modificación igual a la hora actual.
2. **Dados** un plato activo y los mismos datos que ya tiene, **Cuando** se actualiza, **Entonces** responde 200 y la fecha de modificación pasa a la hora actual.
3. **Dado** un plato llamado «Ajiaco», **Cuando** se actualiza con el nombre «AJIACO», **Entonces** responde 200 y el nombre queda «AJIACO» (un plato no entra en conflicto consigo mismo).
4. **Dados** dos platos activos, **Cuando** se actualiza uno con el nombre del otro, sin distinguir mayúsculas, **Entonces** responde 422 y ninguno cambia.
5. **Dados** datos inválidos, **Cuando** se actualiza, **Entonces** responde 400 y el plato no cambia.
6. **Dado** un identificador inexistente o de un plato eliminado, **Cuando** se actualiza con datos válidos, **Entonces** responde 404, aunque el nombre enviado coincida con el de otro plato activo.
7. **Dado** un plato marcado como no disponible, **Cuando** se pide el listado filtrando por disponibles, **Entonces** no aparece.

---

### Historia de usuario 4: retirar un plato del menú (Prioridad: P2)

Como administrador del restaurante, quiero eliminar un plato que ya no se ofrece, para que deje de aparecer en el menú sin perder su registro.

**Por qué esta prioridad**: completa el ciclo de vida del plato; mientras tanto, marcarlo como no disponible es una alternativa.

**Prueba independiente**: se crea un plato, se elimina y se comprueba la respuesta, que ya no aparece en ninguna consulta y que su registro y su fecha de eliminación se conservan en la base de datos.

**Escenarios de aceptación**:

1. **Dado** un plato activo, **Cuando** se elimina, **Entonces** responde 204 sin cuerpo.
2. **Dado** un plato eliminado, **Cuando** se consulta su registro en la base de datos, **Entonces** existe, con sus datos intactos y la fecha de eliminación igual a la hora actual.
3. **Dado** un plato eliminado, **Cuando** se pide su detalle, se actualiza o se elimina de nuevo, **Entonces** responde 404.
4. **Dado** un identificador inexistente, **Cuando** se elimina, **Entonces** responde 404.

---

### Historia de usuario 5: menú inicial de ejemplo (Prioridad: P3)

Como desarrollador o evaluador del proyecto, quiero que la base de datos recién creada ya contenga un menú de ejemplo, para probar la API nada más arrancarla.

**Por qué esta prioridad**: facilita las demostraciones y lo exige el principio V, pero no añade ninguna capacidad nueva.

**Prueba independiente**: sobre una base de datos recién migrada, se comprueba que existen exactamente los 5 platos semilla con sus datos, y que el primer plato creado después no choca con sus identificadores.

**Escenarios de aceptación**:

1. **Dada** una base de datos recién migrada, **Cuando** se consultan los platos, **Entonces** existen exactamente los 5 platos de la tabla de datos semilla, con esos valores.
2. **Dada** una base de datos recién migrada, **Cuando** se crea un plato nuevo, **Entonces** responde 201 con un identificador distinto de los de la semilla.

---

### Casos límite

- **Nombre solo con espacios**: se rechaza con 400 como nombre obligatorio.
- **Nombre de 100 caracteres con espacios alrededor** (102 en total): 400; los límites de longitud se miden antes de recortar.
- **Descripción o URL de imagen vacías o solo con espacios**: se guardan como ausentes (sin descripción o sin imagen); no son un error.
- **Precio con más de 2 decimales** (por ejemplo, 1000,555): 400; no se redondea.
- **Precio con ceros sobrantes** (por ejemplo, 1000,550 u 8000,000): se acepta, porque su valor solo tiene 2 decimales significativos.
- **Precio 0, negativo o mayor que 99.999.999,99**: 400.
- **URL de imagen relativa o con otro esquema** (`ftp:`, `javascript:`, `file:`, `data:`): 400.
- **URL de imagen válida con espacios alrededor**: se acepta y se guarda recortada.
- **Campo obligatorio ausente en el cuerpo** (por ejemplo, sin disponibilidad o sin precio): 400. No se aplican valores por defecto.
- **Cuerpo ausente o JSON mal formado**: 400 como ProblemDetails, según las convenciones de la feature 002.
- **Identificador no numérico, cero o negativo en la ruta**: 404.
- **Altas o actualizaciones simultáneas con el mismo nombre**: como mucho una tiene éxito; las demás responden 422, nunca 500.
- **Ediciones simultáneas del mismo plato**: gana la última; no se detecta el conflicto.
- **Eliminar dos veces el mismo plato**: la segunda vez responde 404.
- **Listado con muchos platos**: se devuelven todos; no hay paginación.
- **Imagen cuyo enlace ya no existe**: la API no comprueba que el enlace responda; solo valida su formato.

## Requisitos _(obligatorio)_

### Requisitos funcionales

**Modelo del plato (principio III)**

- **FR-001**: Un plato DEBE tener: identificador entero autoincremental, nombre, descripción opcional, precio en pesos colombianos, URL de imagen opcional, disponibilidad, fecha de creación, fecha de última modificación y fecha de eliminación opcional.
- **FR-002**: Las tres fechas DEBEN registrarse en UTC a partir de la hora actual del sistema obtenida de forma controlable en las pruebas (principio III), nunca de la hora del equipo de forma directa.
- **FR-003**: Un plato está **activo** si no tiene fecha de eliminación, y **eliminado** si la tiene.

**Consultas**

- **FR-004**: `GET /api/v1/dishes` DEBE devolver 200 con la lista de platos activos, ordenada alfabéticamente por nombre según el orden del español, sin distinguir mayúsculas y colocando las vocales con tilde junto a su letra base y la «ñ» entre «n» y «o». Si no hay ninguno, la lista está vacía.
- **FR-005**: El listado DEBE aceptar un parámetro de consulta opcional `available` (`true` o `false`, sin distinguir mayúsculas) que limita el resultado a los platos con esa disponibilidad. Un valor vacío (`?available=`) equivale a no enviar el filtro. Cualquier otro valor (por ejemplo, `1` o `si`) DEBE responder 400 como ProblemDetails.
- **FR-006**: `GET /api/v1/dishes/{id}` DEBE devolver 200 con el plato si está activo, y 404 como ProblemDetails si no existe o está eliminado.
- **FR-007**: Cada plato devuelto DEBE tener exactamente estas propiedades JSON: `id`, `name`, `description`, `price`, `imageUrl`, `isAvailable`, `createdAt` y `updatedAt`. `description` e `imageUrl` son `null` cuando no tienen valor. La fecha de eliminación NO DEBE exponerse.

**Validación de entrada (principio IV; responde 400 con mensajes en español)**

- **FR-008**: Crear y actualizar DEBEN recibir el mismo cuerpo, con `name`, `description`, `price`, `imageUrl` e `isAvailable`. `name`, `price` e `isAvailable` son obligatorios; no se aplican valores por defecto.
- **FR-009**: `name` DEBE contener al menos un carácter distinto de espacio y tener 100 caracteres como máximo, contados sobre el valor recibido (antes del recorte de FR-014).
- **FR-010**: `description`, si se envía, DEBE tener 500 caracteres como máximo, contados sobre el valor recibido (antes del recorte de FR-014).
- **FR-011**: `price` DEBE ser mayor que 0 y menor o igual que 99.999.999,99, con 2 decimales significativos como máximo. Se evalúa el valor numérico, no los decimales escritos: los ceros finales sobrantes (1000,550) no cuentan.
- **FR-012**: `imageUrl`, si se envía con contenido, DEBE ser, una vez recortada, una URL absoluta con esquema `http` o `https`, y tener 2048 caracteres como máximo, contados sobre el valor recibido.
- **FR-013**: Cada campo inválido DEBE producir un mensaje en español en la respuesta 400, y una petición inválida NO DEBE modificar ningún dato.
- **FR-014**: Antes de guardar, el nombre, la descripción y la URL de imagen DEBEN recortarse (espacios iniciales y finales). Una descripción o URL de imagen vacía o solo con espacios DEBE guardarse como ausente.

**Reglas de negocio (principio IV; responde 422)**

- **FR-015**: No DEBEN existir dos platos activos cuyos nombres, ya recortados, coincidan sin distinguir mayúsculas. Las tildes y demás diacríticos sí distinguen. Los platos eliminados no cuentan.
- **FR-016**: Crear un plato, o actualizarlo, con un nombre que incumple FR-015 DEBE responder 422 como ProblemDetails, con un mensaje en español que indique que el nombre ya está en uso, y NO DEBE modificar ningún dato. Al actualizar, el propio plato no cuenta como duplicado.
- **FR-017**: La unicidad de FR-015 DEBE garantizarse también ante peticiones simultáneas: la petición que pierde la carrera DEBE responder 422, nunca 500.

**Escrituras**

- **FR-018**: `POST /api/v1/dishes` con datos válidos DEBE crear el plato con la fecha de creación igual a la hora actual, la de modificación igual a la de creación y sin fecha de eliminación, y responder 201 con la cabecera `Location` (`/api/v1/dishes/{id}`) y el plato creado.
- **FR-019**: `PUT /api/v1/dishes/{id}` con datos válidos DEBE reemplazar nombre, descripción, precio, URL de imagen y disponibilidad, fijar la fecha de modificación en la hora actual (aunque no cambie ningún valor), conservar la fecha de creación y responder 200 con el plato actualizado.
- **FR-020**: `DELETE /api/v1/dishes/{id}` DEBE fijar la fecha de eliminación del plato en la hora actual, sin borrar el registro ni modificar sus demás datos, y responder 204 sin cuerpo.
- **FR-021**: `PUT` y `DELETE` sobre un identificador inexistente o de un plato eliminado DEBEN responder 404 como ProblemDetails. El orden de evaluación es: validación de la entrada (400), existencia del plato (404) y unicidad del nombre (422).
- **FR-022**: Ante ediciones simultáneas del mismo plato DEBE prevalecer la última escritura; no se detectan conflictos de versión.
- **FR-023**: No DEBE existir ninguna forma de restaurar un plato eliminado.

**Datos semilla (principios V y VII)**

- **FR-024**: Las migraciones DEBEN crear los 5 platos de la tabla «Datos semilla», todos con fecha de creación y de modificación `2026-11-04T00:00:00Z` y sin fecha de eliminación.
- **FR-025**: Los platos creados después de la semilla DEBEN recibir identificadores que no colisionen con los de la semilla.
- **FR-026**: Una prueba específica DEBE verificar los datos semilla sobre una base de datos recién migrada.

**Calidad y pruebas (principios II, VI y VII)**

- **FR-027**: Las rutas DEBEN cumplir las convenciones HTTP vigentes (feature 002): prefijo `/api/v1/`, códigos de respuesta declarados y errores como ProblemDetails. Las pruebas de convenciones y de arquitectura existentes DEBEN pasar sin modificarse.
- **FR-028**: Respawn DEBE integrarse en las pruebas contra base de datos (pospuesto en la feature 003, FR-023) para limpiar todas las tablas, salvo `__EFMigrationsHistory`, entre pruebas. Cada prueba DEBE crear los datos que necesita.
- **FR-029**: Todas las pruebas nuevas DEBEN seguir TDD, la nomenclatura `{Metodo}_Should_{Resultado}_When_{Escenario}`, una sola aserción lógica y ninguna lógica condicional.
- **FR-030**: La puerta de cobertura (≥ 80 % de líneas sobre los cuatro ensamblados de producción, sin exclusiones) es obligatoria.
- **FR-031**: Las pruebas existentes de las features 001, 002 y 003 DEBEN seguir pasando sin cambiar sus aserciones.

### Datos semilla

| Id  | Nombre             | Descripción                                                                                          | Precio (COP) | Disponible | URL de imagen                                                                     |
| --- | ------------------ | ---------------------------------------------------------------------------------------------------- | -----------: | ---------- | --------------------------------------------------------------------------------- |
| 1   | Bandeja paisa      | Fríjoles, arroz, chicharrón, carne molida, chorizo, huevo frito, tajada de maduro, aguacate y arepa. |     38000.00 | Sí         | https://upload.wikimedia.org/wikipedia/commons/1/12/Bandepaisabog.JPG             |
| 2   | Ajiaco santafereño | Sopa de tres papas con pollo, mazorca y guascas, servida con crema, alcaparras y aguacate.           |     32000.00 | Sí         | https://upload.wikimedia.org/wikipedia/commons/d/d7/Ajiaco.jpg                    |
| 3   | Sancocho de hueso  | Caldo de hueso de res con yuca, plátano, papa y mazorca, acompañado de arroz y aguacate.             |     28000.00 | Sí         | https://upload.wikimedia.org/wikipedia/commons/4/42/Sancocho-hueso.JPG            |
| 4   | Lechona            | Cerdo relleno de arroz y arveja, horneado durante horas, servido con arepa e insulso.                |     30000.00 | No         | https://upload.wikimedia.org/wikipedia/commons/9/92/Plato_Lechona.jpg             |
| 5   | Arepa de huevo     | Arepa de maíz frita rellena de huevo, típica de la costa Caribe.                                     |      8000.00 | Sí         | https://upload.wikimedia.org/wikipedia/commons/2/21/Arepa_de_huevo_por_dentro.jpg |

La lechona se siembra como no disponible para que el filtro por disponibilidad pueda demostrarse con los datos iniciales.

### Entidades clave

- **Plato (`Dish`)**: preparación que ofrece el restaurante. Atributos: identificador, nombre (único entre los activos, sin distinguir mayúsculas), descripción opcional, precio en COP, URL de imagen opcional, disponibilidad, fecha de creación, fecha de última modificación y fecha de eliminación opcional (marca de borrado lógico). En esta feature no se relaciona con ninguna otra entidad; los pedidos futuros lo referenciarán.

## Criterios de éxito _(obligatorio)_

### Resultados medibles

- **SC-001**: Con la API recién arrancada en Development, el listado devuelve los 5 platos semilla sin ningún paso manual, y filtrando por disponibles devuelve 4.
- **SC-002**: Un administrador completa el ciclo crear → consultar → modificar → eliminar de un plato con 4 peticiones, una por operación, sin consultas auxiliares.
- **SC-003**: El 100 % de las peticiones con datos inválidos responde 400 con un mensaje en español por campo y sin modificar datos; el 100 % de los nombres duplicados responde 422, también ante peticiones simultáneas.
- **SC-004**: El 100 % de las operaciones sobre platos inexistentes o eliminados responde 404, y ningún plato eliminado aparece en el listado.
- **SC-005**: Tras eliminar un plato, su registro y su fecha de eliminación siguen existiendo en el 100 % de los casos.
- **SC-006**: La solución compila sin advertencias, `dotnet format --verify-no-changes` no detecta cambios, toda la suite termina en verde, la cobertura de líneas es ≥ 80 % y ReportGenerator termina con código de salida 0.

## Supuestos

- La rama de trabajo es la actual (`speckit-test`); no se crea una rama nueva.
- No existe autenticación (constitución, sección Seguridad): cualquier cliente puede consultar y modificar platos. «Cliente» y «administrador» son roles de uso, no permisos.
- El menú tiene decenas de platos, no miles; por eso no hay paginación.
- Las imágenes semilla son archivos de Wikimedia Commons con licencias libres que exigen atribución si se publican; la API solo guarda el enlace. Su disponibilidad depende de un servicio externo y no se verifica.
- La comparación sin distinguir mayúsculas se refiere a letras con y sin mayúscula; no se normalizan tildes, diéresis ni la «ñ».
- El orden alfabético del listado es el de la colación ICU `es-u-ks-level2` (research D2): sin distinguir mayúsculas, con las tildes tras su letra base y la «ñ» como letra propia.
- Quedan fuera del alcance: categorías, paginación, restauración de platos eliminados, actualización parcial (PATCH), endpoint específico de disponibilidad, subida o verificación de imágenes, control de concurrencia optimista, normalización de tildes, pedidos y la restricción de eliminar platos con pedidos (se decidirá en la feature de pedidos).
