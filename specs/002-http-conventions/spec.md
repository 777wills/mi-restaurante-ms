# Especificación de la feature: Convenciones HTTP transversales

**Rama de la feature**: `speckit-test`

**Creada**: 2026-11-04

**Estado**: Aclarada

**Entrada**: descripción del usuario: «Convenciones HTTP transversales. (1) `AddProblemDetails()` con un único `IExceptionHandler` (VI). (2) `NotFoundException` (404) y `BusinessRuleException` (422) definidas en Application; las demás excepciones responden 500 sin detalles internos (IV y VI). (3) Scalar (`Scalar.AspNetCore` 2.x) en Development, junto al `MapOpenApi` que ya existe (VI). (4) CORS abierto solo en Development (VI). (5) Convención de rutas con `/api/v1/` y `[ProducesResponseType]` en cada acción (VI). (6) Registrar `TimeProvider.System` en el contenedor de dependencias (III).»

> **Nota sobre el nivel de detalle**: esta feature es transversal y no contiene código de negocio. Sus «usuarios» son los clientes de la API y los desarrolladores del repositorio. Los mecanismos y paquetes que se nombran (ProblemDetails, `IExceptionHandler`, Scalar, `TimeProvider`, `[ProducesResponseType]`) no son decisiones de implementación: son obligaciones fijadas en la constitución (principios III, IV y VI y la tabla del stack). Por eso se citan literalmente.

## Aclaraciones

### Sesión 2026-11-04

- P: Cuando el framework rechaza un cuerpo con JSON mal formado, con un tipo incorrecto o vacío, ¿qué mensajes por campo debe devolver la respuesta 400? → R: Mensajes genéricos en español, sin nombres de tipos .NET ni posiciones internas; los mensajes de DataAnnotations siguen siendo los de cada DTO.
- P: Cuando el servidor rechaza una petición mal formada del cliente antes de llegar al controlador (`BadHttpRequestException`), ¿qué código debe devolver el manejador? → R: El código que trae la excepción (400, 413…), como ProblemDetails con el título genérico «Solicitud no válida», sin `detail` y registrada a nivel Information.
- P: Además de `status`, `title`, `detail` y `traceId`, ¿qué otros campos deben llevar las respuestas ProblemDetails? → R: `type` con el valor por defecto del framework (enlace a RFC 9110), sin personalizar, y sin `instance`.

## Escenarios de usuario y pruebas _(obligatorio)_

### Historia de usuario 1: errores de negocio y fallos como ProblemDetails (Prioridad: P1)

Como cliente de la API, quiero que cualquier error me llegue con el mismo formato estándar (ProblemDetails, RFC 9457), con el código HTTP correcto y un mensaje en español, para poder tratarlos de forma uniforme sin conocer detalles internos del servidor.

**Por qué esta prioridad**: toda feature de negocio posterior lanzará `NotFoundException` y `BusinessRuleException` desde sus servicios. Sin un único punto de traducción, cada controlador tendría que capturar excepciones, lo que el principio VI prohíbe.

**Prueba independiente**: con la API levantada en el entorno de pruebas y un controlador que solo existe en el proyecto de pruebas funcionales, se provoca cada tipo de excepción y se comprueba el código, el formato y el contenido de la respuesta.

**Escenarios de aceptación**:

1. **Dada** una petición cuya ejecución lanza `NotFoundException` con un mensaje, **Cuando** la API responde, **Entonces** el código es 404, el cuerpo es ProblemDetails con el título «Recurso no encontrado», el `detail` es el mensaje de la excepción y se incluye `traceId`.
2. **Dada** una petición cuya ejecución lanza `BusinessRuleException` con un mensaje, **Cuando** la API responde, **Entonces** el código es 422, el cuerpo es ProblemDetails con el título «Regla de negocio incumplida», el `detail` es el mensaje de la excepción y se incluye `traceId`.
3. **Dada** una petición cuya ejecución lanza cualquier otra excepción, **Cuando** la API responde, **Entonces** el código es 500, el cuerpo es ProblemDetails con el título «Error interno del servidor», sin `detail`, sin traza de pila, sin el mensaje ni el tipo de la excepción, y con `traceId`.
4. **Dada** una excepción no controlada, **Cuando** el manejador la procesa, **Entonces** queda registrada a nivel Error con la excepción completa. Una `NotFoundException` o `BusinessRuleException` queda registrada a nivel Information con su mensaje y sin traza de pila.
5. **Dado** un controlador que lanza excepciones, **Cuando** se revisa su código, **Entonces** no contiene `try`/`catch`: la traducción ocurre en un único manejador.
6. **Dada** una petición cuya ejecución lanza `BadHttpRequestException` con un código 4xx (por ejemplo, 413), **Cuando** la API responde, **Entonces** el código es el de la excepción, el cuerpo es ProblemDetails con el título «Solicitud no válida», sin `detail` y con `traceId`, y queda registrada a nivel Information sin traza de pila.

---

### Historia de usuario 2: contrato de errores uniforme y en español (Prioridad: P1)

Como cliente de la API, quiero que también los errores que genera el propio framework (validación, ruta inexistente, método no permitido, tipo de contenido no admitido) lleguen como ProblemDetails con títulos en español, para no tener que distinguir entre formatos ni idiomas.

**Por qué esta prioridad**: el principio VI exige que los errores se respondan como ProblemDetails y el principio VIII que los mensajes al usuario estén en español. Sin esto, una ruta mal escrita responde con cuerpo vacío y la validación responde en inglés.

**Prueba independiente**: se piden una ruta inexistente, un método no admitido, un cuerpo con tipo de contenido no admitido y un cuerpo inválido, y se comprueba el formato y el título de cada respuesta.

**Escenarios de aceptación**:

1. **Dada** una ruta que no existe, **Cuando** se pide, **Entonces** la respuesta es 404 en formato ProblemDetails con el título «Recurso no encontrado» y `traceId`.
2. **Dada** una ruta existente, **Cuando** se pide con un método HTTP que no admite, **Entonces** la respuesta es 405 en formato ProblemDetails con el título «Método no permitido».
3. **Dada** una acción que recibe un cuerpo, **Cuando** se envía con un tipo de contenido no admitido, **Entonces** la respuesta es 415 en formato ProblemDetails con el título «Tipo de contenido no admitido».
4. **Dado** un cuerpo que incumple las validaciones del DTO de entrada, **Cuando** se envía, **Entonces** la respuesta es 400 en formato ProblemDetails de validación con el título «Uno o más errores de validación», los errores por campo y `traceId`.
5. **Dado** un cuerpo con JSON mal formado, con un valor de tipo incorrecto, vacío o ausente, **Cuando** se envía, **Entonces** la respuesta es 400 en formato ProblemDetails de validación con el título «Uno o más errores de validación» y mensajes genéricos en español que no contienen nombres de tipos .NET, rutas JSON internas ni números de línea o posición.

---

### Historia de usuario 3: convención de rutas y contrato documentado verificada automáticamente (Prioridad: P2)

Como responsable del proyecto, quiero que una prueba automática garantice que todo controlador usa `[ApiController]`, expone sus rutas bajo `/api/v1/` sin poder saltarse el prefijo, y declara en cada acción sus códigos de respuesta, para que las features siguientes no puedan incumplir la convención sin que la suite falle.

**Por qué esta prioridad**: aún no hay controladores de negocio, así que nada está en riesgo hoy. Pero la primera feature de negocio llegará enseguida y la regla debe estar activa antes.

**Prueba independiente**: se añade temporalmente un controlador que incumple cada regla y la suite de arquitectura falla indicando el controlador o la acción. Al quitarlo, la suite pasa.

**Escenarios de aceptación**:

1. **Dados** los controladores de la API, **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** pasan, incluso si no existe ningún controlador.
2. **Dado** un controlador sin `[ApiController]`, **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** fallan e indican el controlador.
3. **Dado** un controlador cuya ruta no empieza por `api/v1/`, **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** fallan e indican el controlador.
4. **Dada** una acción con una plantilla de ruta absoluta (que empieza por `/` o `~/`) que no empieza por `api/v1/`, **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** fallan e indican la acción.
5. **Dada** una acción pública sin ningún `[ProducesResponseType]`, **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** fallan e indican la acción.

---

### Historia de usuario 4: documentación interactiva y CORS solo en Development (Prioridad: P2)

Como desarrollador, quiero explorar y probar la API desde una interfaz interactiva y llamarla desde un frontend local en otro origen, pero solo en Development, para depurar con comodidad sin exponer nada de eso en otros entornos.

**Por qué esta prioridad**: facilita el trabajo diario y la constitución lo exige, pero ningún cliente de producción depende de ello.

**Prueba independiente**: se arranca la API en Development y en otro entorno y se comprueba en cada uno la disponibilidad de la interfaz interactiva, del documento OpenAPI y de las cabeceras CORS.

**Escenarios de aceptación**:

1. **Dada** la API en Development, **Cuando** se pide la interfaz interactiva en su ruta por defecto (`/scalar`), **Entonces** responde con éxito y su página referencia el documento OpenAPI de la API (`/openapi/v1.json`).
2. **Dada** la API en Development, **Cuando** se pide el documento OpenAPI, **Entonces** responde con éxito.
3. **Dada** la API en un entorno distinto de Development, **Cuando** se piden la interfaz interactiva o el documento OpenAPI, **Entonces** la respuesta es 404 en formato ProblemDetails.
4. **Dada** la API en Development, **Cuando** llega una petición de verificación previa (preflight) desde cualquier origen con cualquier método y cabecera, **Entonces** la respuesta la autoriza para cualquier origen, método y cabecera.
5. **Dada** la API en un entorno distinto de Development, **Cuando** llega una petición desde otro origen, **Entonces** la respuesta no incluye ninguna cabecera CORS.

---

### Historia de usuario 5: hora actual inyectable (Prioridad: P3)

Como desarrollador, quiero que el contenedor de dependencias proporcione el reloj del sistema como `TimeProvider`, para que los servicios de las próximas features obtengan la hora actual de forma inyectable y las pruebas puedan sustituirla.

**Por qué esta prioridad**: el principio III lo exige, pero ningún código lo consume todavía.

**Prueba independiente**: se resuelve `TimeProvider` desde el contenedor de la API arrancada y se comprueba que es el reloj del sistema y que siempre es la misma instancia.

**Escenarios de aceptación**:

1. **Dada** la API arrancada, **Cuando** se resuelve `TimeProvider` desde el contenedor, **Entonces** se obtiene `TimeProvider.System`.
2. **Dadas** dos resoluciones de `TimeProvider`, **Cuando** se comparan, **Entonces** son la misma instancia.

---

### Casos límite

- **Respuesta ya iniciada**: si la excepción ocurre después de empezar a enviar la respuesta, ya no se puede escribir ProblemDetails. La excepción DEBE seguir registrándose y la conexión se cierra según el comportamiento por defecto del servidor. Es comportamiento del framework (ver `research.md`, D2) y no se prueba en esta feature.
- **Excepción derivada**: una excepción que herede de `NotFoundException` o `BusinessRuleException` se trata igual que su clase base.
- **Petición rechazada por el servidor**: `BadHttpRequestException` (por ejemplo, cuerpo mayor que el límite) no es un fallo del servidor; responde con su propio código 4xx según FR-004 y FR-004a, nunca con 500.
- **Mensaje vacío**: si una excepción de negocio llega sin mensaje, la respuesta mantiene el código y el título; el `detail` puede quedar vacío, pero nunca contiene datos internos. Se verifica con una prueba funcional.
- **Excepción en el entorno Development**: la respuesta 500 tampoco expone detalles internos en Development. No se usa la página de excepciones para desarrolladores.
- **Cancelación de la petición por el cliente**: queda fuera del alcance; se aplica el comportamiento por defecto del framework.
- **Suite de convenciones sin controladores**: hoy no existe ningún controlador. Las pruebas de convención DEBEN ejecutarse y pasar sobre el conjunto vacío, no desactivarse. La fase Red se demuestra con un controlador temporal que incumple cada regla y se revierte antes de integrar.
- **Controlador de pruebas**: el controlador que lanza excepciones vive solo en el proyecto de pruebas funcionales y se registra solo en la fábrica de pruebas. No DEBE aparecer en el ensamblado de la API ni en su documento OpenAPI de producción, y no lo DEBEN ver las pruebas de arquitectura.
- **Ruta inexistente fuera de `/api/v1/`**: también responde ProblemDetails 404; el formato no depende del prefijo.
- **Petición CORS con credenciales**: con «cualquier origen» no se admiten credenciales; no se habilitan.

## Requisitos _(obligatorio)_

### Requisitos funcionales

**Excepciones de negocio (principio IV)**

- **FR-001**: Application DEBE definir `NotFoundException` y `BusinessRuleException`, ambas construidas con un mensaje dirigido al cliente y redactado en español.
- **FR-002**: Esas excepciones NO DEBEN depender de ningún tipo HTTP ni de ASP.NET Core. Application sigue cumpliendo la lista blanca de ensamblados del principio II.

**Traducción de errores (principio VI)**

- **FR-003**: La API DEBE responder los errores como ProblemDetails (RFC 9457) mediante `AddProblemDetails()` y un único `IExceptionHandler`.
- **FR-004**: El manejador DEBE aplicar este mapeo: `NotFoundException` (y sus derivadas) → 404; `BusinessRuleException` (y sus derivadas) → 422; `BadHttpRequestException` → el código que trae la propia excepción (400, 413…); cualquier otra excepción → 500.
- **FR-004a**: La respuesta a una `BadHttpRequestException` DEBE llevar el título fijo «Solicitud no válida», sea cual sea su código, y NO DEBE incluir `detail` ni el mensaje de la excepción.
- **FR-005**: Las respuestas 404 y 422 originadas por excepciones DEBEN llevar el título fijo de la tabla de FR-008 y, en `detail`, el mensaje de la excepción.
- **FR-006**: La respuesta 500 DEBE llevar solo el título fijo «Error interno del servidor». NO DEBE incluir `detail`, traza de pila, mensaje, tipo de la excepción ni ningún otro dato interno, en ningún entorno.
- **FR-007**: Todas las respuestas ProblemDetails DEBEN incluir `traceId` para correlacionarlas con los registros.
- **FR-007a**: El campo `type` DEBE quedar con el valor que asigna el framework por defecto y NO DEBE personalizarse; las pruebas no verifican su valor. Las respuestas NO DEBEN incluir `instance`.
- **FR-008**: Los títulos de ProblemDetails DEBEN estar en español y ser fijos por código, tanto para los errores de las excepciones como para los generados por el framework. La única salvedad son las respuestas a `BadHttpRequestException`, que siguen FR-004a aunque su código sea 400:

  | Código | Título                          |
  | ------ | ------------------------------- |
  | 400    | Uno o más errores de validación |
  | 404    | Recurso no encontrado           |
  | 405    | Método no permitido             |
  | 415    | Tipo de contenido no admitido   |
  | 422    | Regla de negocio incumplida     |
  | 500    | Error interno del servidor      |

- **FR-009**: Las respuestas de error sin cuerpo que genera el framework (al menos 404 de ruta inexistente, 405 y 415) DEBEN devolverse como ProblemDetails.
- **FR-010**: Los errores de validación de DataAnnotations DEBEN seguir respondiendo 400 automáticamente como ProblemDetails de validación, con los errores por campo.
- **FR-010a**: Los mensajes de error que genera el framework al leer el cuerpo (JSON mal formado, valor de tipo incorrecto, cuerpo vacío o ausente) DEBEN sustituirse por mensajes genéricos en español que NO DEBEN contener nombres de tipos .NET, rutas JSON internas ni números de línea o posición. Los mensajes de DataAnnotations siguen siendo los definidos en cada DTO.
- **FR-011**: El manejador DEBE registrar con `ILogger<T>` y plantillas de mensaje: las excepciones que producen 500 a nivel Error con la excepción completa; `NotFoundException`, `BusinessRuleException` y `BadHttpRequestException` a nivel Information con su mensaje y sin traza de pila.
- **FR-012**: Ningún controlador DEBE contener `try`/`catch`.

**Convenciones de rutas y contrato (principio VI)**

- **FR-013**: Todo controlador de la API DEBE llevar `[ApiController]` y declarar una ruta que empiece por `api/v1/` (por ejemplo, `[Route("api/v1/[controller]")]`). No se introduce clase base ni prefijo global.
- **FR-014**: Ninguna acción DEBE declarar una plantilla de ruta absoluta (que empiece por `/` o `~/`) que no empiece por `api/v1/`.
- **FR-015**: Toda acción pública de un controlador DEBE declarar al menos un `[ProducesResponseType]`.
- **FR-016**: El proyecto de pruebas de arquitectura DEBE verificar FR-013, FR-014 y FR-015 por reflexión sobre los controladores del ensamblado de la API. Los mensajes de fallo DEBEN indicar los controladores o acciones que incumplen. Las pruebas DEBEN pasar cuando no exista ningún controlador.

**Documentación interactiva y CORS (principio VI)**

- **FR-017**: En Development, la API DEBE publicar la interfaz interactiva Scalar (`Scalar.AspNetCore` 2.18.7) en su ruta por defecto, `/scalar`, junto al documento OpenAPI nativo que ya existe. No se cambian los perfiles de arranque para abrir el navegador.
- **FR-018**: Fuera de Development, ni la interfaz interactiva ni el documento OpenAPI DEBEN estar disponibles.
- **FR-019**: En Development, CORS DEBE permitir cualquier origen, método y cabecera, sin credenciales.
- **FR-020**: Fuera de Development, la API NO DEBE emitir cabeceras CORS.

**Hora actual (principio III)**

- **FR-021**: El contenedor de dependencias DEBE registrar `TimeProvider.System` como `TimeProvider` con una única instancia para toda la aplicación.

**Pruebas (principio VII)**

- **FR-022**: El mapeo de excepciones DEBE verificarse de extremo a extremo en el proyecto de pruebas funcionales con un controlador que solo existe en ese proyecto y se registra solo en la fábrica de pruebas.
- **FR-023**: Las pruebas funcionales DEBEN arrancar la API tanto en Development como en un entorno distinto, para verificar FR-017 a FR-020.
- **FR-024**: Todas las pruebas nuevas DEBEN seguir TDD, la nomenclatura `{Metodo}_Should_{Resultado}_When_{Escenario}`, una sola aserción lógica y ninguna lógica condicional.

### Entidades clave

- **NotFoundException**: indica que el recurso pedido no existe. Lleva un mensaje en español para el cliente. Se traduce a 404.
- **BusinessRuleException**: indica que la petición incumple una regla de negocio (por ejemplo, pedir un plato no disponible). Lleva un mensaje en español para el cliente. Se traduce a 422.
- **Respuesta de error (ProblemDetails)**: cuerpo estándar de error con código, título fijo en español, `type` por defecto del framework, `detail` opcional y `traceId`, sin `instance`; en validación, además, los errores por campo.

## Criterios de éxito _(obligatorio)_

### Resultados medibles

- **SC-001**: El 100 % de las respuestas de error verificadas (400, 404 por excepción, 404 por ruta, 405, 415, 422, 4xx por `BadHttpRequestException` y 500) usan el formato ProblemDetails, tienen su título en español según FR-008 y FR-004a e incluyen `traceId`.
- **SC-002**: 0 respuestas 500 contienen traza de pila, mensaje o tipo de la excepción original, tanto en Development como en otro entorno.
- **SC-003**: En el 100 % de los intentos, un controlador temporal que incumple cualquiera de las cuatro reglas de convención (`[ApiController]`, prefijo del controlador, plantilla absoluta, `[ProducesResponseType]`) hace fallar la suite de arquitectura.
- **SC-004**: La interfaz interactiva y el documento OpenAPI responden con éxito en Development y con 404 en otro entorno; las cabeceras CORS aparecen solo en Development.
- **SC-005**: 0 controladores contienen `try`/`catch`, y existe exactamente 1 manejador de excepciones registrado.
- **SC-006**: La solución compila sin advertencias y toda la suite termina en verde; la cobertura de líneas es ≥ 80 % y ReportGenerator termina con código de salida 0 (principio VII y puerta de calidad 4, sin excepciones).

## Supuestos

- La rama de trabajo es la actual (`speckit-test`); no se crea una rama nueva.
- Las versiones son las de la tabla del stack de la constitución: `Scalar.AspNetCore` 2.18.7 y `Microsoft.AspNetCore.OpenApi` 10.0.14 (ya instalado).
- Las excepciones de negocio solo llevan un mensaje. Constructores de conveniencia (por ejemplo, con nombre de recurso e identificador) se añadirán cuando una spec los necesite.
- No se añaden otros códigos de error (por ejemplo, 409); solo llegarán cuando una spec los requiera, como fija el principio VI.
- Esta feature no introduce base de datos: las pruebas funcionales arrancan la API sin PostgreSQL. Testcontainers, Respawn, `compose.yaml` y el endpoint `/health` llegarán con la primera feature que use persistencia.
- Quedan fuera del alcance: controladores y endpoints de negocio, autenticación y autorización, la redirección HTTPS y `UseAuthorization` existentes (no se tocan), la personalización visual de Scalar, la configuración de CORS para entornos distintos de Development y la limpieza de comentarios de plantilla en `Program.cs`.
- Los títulos de FR-008 son valores exactos y forman parte del contrato que verifican las pruebas.
