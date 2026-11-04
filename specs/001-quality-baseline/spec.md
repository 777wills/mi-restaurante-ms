# Especificación de la feature: Base de calidad y limpieza del esqueleto

**Rama de la feature**: `speckit-test`

**Creada**: 2026-11-04

**Estado**: Borrador

**Entrada**: descripción del usuario: «Base de calidad y limpieza del esqueleto. (1) Quitar `Microsoft.NET.Test.Sdk` y `xunit.runner.visualstudio` de los 5 proyectos de pruebas (VII). (2) Cambiar `NetArchTest.Rules` 1.3.2 por `NetArchTest.eNhancedEdition` 1.4.5 (VII). (3) Añadir `AwesomeAssertions` y `Moq` a los proyectos de pruebas que los necesiten (VII). (4) Crear `Directory.Build.props` con `TreatWarningsAsErrors` (VIII). (5) Crear el manifiesto de herramientas locales `.config/dotnet-tools.json` con `dotnet-ef` 10.0.14 y `dotnet-reportgenerator-globaltool` 5.5.12. (6) Montar la puerta de cobertura: informes Cobertura de cada proyecto, fusión con ReportGenerator y `lineCoverage=80`. (7) Subir los parches de `Microsoft.AspNetCore.OpenApi` y `Microsoft.AspNetCore.Mvc.Testing` a 10.0.14. (8) Borrar los archivos de plantilla (`Class1.cs` ×3, `UnitTest1.cs` ×4, `WeatherForecast` y su controlador). (9) Añadir una prueba de arquitectura que impida a Application referenciar `EntityFrameworkCore.Relational` o Npgsql (II).»

> **Nota sobre el nivel de detalle**: esta feature es de infraestructura de desarrollo. Sus «usuarios» son los desarrolladores del repositorio, y los paquetes, herramientas y versiones que se nombran no son decisiones de implementación: son obligaciones fijadas en la constitución (principios II, VII y VIII y la tabla del stack). Por eso se citan literalmente.

## Clarifications

### Session 2026-11-04

- Q: ¿La lista blanca de ensamblados permitidos en Application debe incluir `Microsoft.Extensions.Logging.Abstractions` (donde vive `ILogger<T>`)? → A: Sí, se añade solo ese ensamblado y de forma explícita (opción B). Llega como dependencia transitiva del paquete `Microsoft.EntityFrameworkCore`, y la sección de observabilidad de la constitución exige `ILogger<T>`.
- Q: ¿Las piezas de la API que exige la constitución y aún no existen (interfaz Scalar, ProblemDetails con `IExceptionHandler` y CORS abierto en Development) quedan fuera del alcance de esta feature? → A: Sí, las tres quedan fuera del alcance de forma explícita y llegan con la primera feature que exponga endpoints de negocio (opción A).
- Q: ¿Los ajustes repetidos en los cinco proyectos de pruebas (`OutputType` `Exe`, `IsPackable`, `using Xunit` y los paquetes comunes de pruebas) pasan a una configuración común? → A: No. La configuración común de la raíz solo fija el framework objetivo, la nulabilidad, los usings implícitos y `TreatWarningsAsErrors` (además de la tolerancia de FR-005, si el plan la ubica ahí). Los ajustes y paquetes propios de las pruebas se quedan en cada proyecto de pruebas, sin un segundo archivo común (opción A).
- Q: ¿Qué código cuenta para el porcentaje de cobertura de líneas del informe fusionado? → A: Solo los cuatro ensamblados de producción (`MiRestaurante.Domain`, `MiRestaurante.Application`, `MiRestaurante.Infrastructure` y `MiRestaurante.Api`), filtrados de forma explícita y sin excluir clases ni archivos (opción A).

## Escenarios de usuario y pruebas _(obligatorio)_

### Historia de usuario 1: ejecución de pruebas conforme a la constitución (Prioridad: P1)

Como desarrollador, quiero que los cinco proyectos de pruebas usen solo la plataforma y las librerías que fija la constitución, para que `dotnet test` funcione en modo Microsoft.Testing.Platform sin mezclar VSTest y con las herramientas de aserción y simulación acordadas.

**Por qué esta prioridad**: el principio VII es NO NEGOCIABLE. Todas las features posteriores escriben pruebas sobre esta base, y hoy los proyectos referencian dos paquetes prohibidos y una librería de arquitectura abandonada.

**Prueba independiente**: se revisan las referencias de paquetes de los cinco proyectos de pruebas y se ejecuta la suite completa con un solo comando. Termina en verde y sin paquetes prohibidos.

**Escenarios de aceptación**:

1. **Dado** el repositorio actual, **Cuando** se revisan las referencias de paquetes de los cinco proyectos de pruebas, **Entonces** ninguno referencia `Microsoft.NET.Test.Sdk` ni `xunit.runner.visualstudio`.
2. **Dado** el proyecto de pruebas de arquitectura, **Cuando** se revisan sus referencias, **Entonces** usa `NetArchTest.eNhancedEdition` 1.4.5 y ya no referencia `NetArchTest.Rules`.
3. **Dados** los cinco proyectos de pruebas, **Cuando** se revisan sus referencias, **Entonces** los cinco tienen `AwesomeAssertions` 9.6.0 y solo `MiRestaurante.Application.UnitTests` tiene `Moq` 4.21.0.
4. **Dado** que algunos proyectos de pruebas aún no contienen pruebas, **Cuando** se ejecuta la suite completa de la solución, **Entonces** la ejecución termina con éxito y los proyectos sin pruebas no la hacen fallar.
5. **Dadas** las pruebas de arquitectura existentes, **Cuando** se ejecutan con la nueva librería, **Entonces** todas siguen pasando con el mismo comportamiento que antes de la migración.

---

### Historia de usuario 2: compilación estricta y configuración común (Prioridad: P1)

Como desarrollador, quiero que toda la solución trate las advertencias como errores y comparta un único punto de configuración común, para que el análisis de nulabilidad sea efectivo y los proyectos nuevos hereden las reglas sin copiarlas.

**Por qué esta prioridad**: el principio VIII exige `TreatWarningsAsErrors` en toda la solución, y la puerta 1 exige compilar sin advertencias. Sin esto, las features siguientes podrían acumular advertencias.

**Prueba independiente**: se compila la solución y se comprueba que no hay advertencias ni errores. Luego se introduce a propósito una advertencia en cualquier proyecto y se comprueba que la compilación falla.

**Escenarios de aceptación**:

1. **Dada** la solución, **Cuando** se compila, **Entonces** termina sin errores ni advertencias.
2. **Dado** cualquier proyecto de la solución (producción o pruebas), **Cuando** su código produce una advertencia del compilador, **Entonces** la compilación falla.
3. **Dada** la configuración común en la raíz, **Cuando** se revisan los nueve archivos de proyecto, **Entonces** ninguno repite la versión de framework objetivo, la nulabilidad ni los usings implícitos, porque los heredan de la configuración común, y los ajustes propios de las pruebas siguen declarados en cada proyecto de pruebas.

---

### Historia de usuario 3: esqueleto sin código de plantilla (Prioridad: P2)

Como desarrollador, quiero que el repositorio no contenga código generado por las plantillas, para que nada de lo que hay contradiga la constitución ni sirva de mal ejemplo.

**Por qué esta prioridad**: el código de plantilla incumple la constitución (hora del sistema directa, rutas sin `/api/v1/`, pruebas vacías que no siguen la nomenclatura). No bloquea a nadie, pero confunde a quien lo use como referencia.

**Prueba independiente**: se busca en el repositorio cada archivo de plantilla y cada referencia a él, y no aparece ninguno. La solución compila y las pruebas pasan.

**Escenarios de aceptación**:

1. **Dado** el repositorio, **Cuando** se buscan `Class1`, `UnitTest1` y `WeatherForecast`, **Entonces** no existe ningún archivo ni referencia con esos nombres.
2. **Dado** el archivo de peticiones HTTP de la API, **Cuando** se abre, **Entonces** solo contiene la variable con la dirección del host y ninguna petición a endpoints inexistentes.
3. **Dada** la API sin controladores de ejemplo, **Cuando** se arranca en Development, **Entonces** arranca sin errores y sigue publicando el documento OpenAPI.

---

### Historia de usuario 4: regla de arquitectura para las dependencias de Application (Prioridad: P2)

Como responsable del proyecto, quiero que una prueba automática garantice que la capa Application solo depende del framework, de Domain, del núcleo de Entity Framework Core y de las abstracciones de registro (`ILogger<T>`), para que nadie le añada un proveedor de base de datos ni dependencias relacionales.

**Por qué esta prioridad**: el principio II exige que cada regla de dependencias esté verificada por una prueba en el proyecto de arquitectura. La regla de Application aún no tiene prueba, pero tampoco hay código en riesgo todavía.

**Prueba independiente**: se añade temporalmente a Application el uso de un ensamblado no permitido y la prueba falla. Al quitarlo, la prueba pasa.

**Escenarios de aceptación**:

1. **Dada** la capa Application actual, **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** la prueba de dependencias de Application pasa.
2. **Dada** la capa Application usando un tipo de `Microsoft.EntityFrameworkCore.Relational`, **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** la prueba falla e indica el ensamblado no permitido.
3. **Dada** la capa Application usando un tipo de Npgsql o de cualquier otro proveedor de base de datos, **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** la prueba falla e indica el ensamblado no permitido.
4. **Dada** la capa Application usando únicamente tipos del framework, de Domain, de `Microsoft.EntityFrameworkCore` y `ILogger<T>` de `Microsoft.Extensions.Logging.Abstractions`, **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** la prueba pasa.
5. **Dada** la capa Application usando un tipo de cualquier otro ensamblado `Microsoft.Extensions.*` (por ejemplo, `Microsoft.Extensions.DependencyInjection.Abstractions`), **Cuando** se ejecutan las pruebas de arquitectura, **Entonces** la prueba falla e indica el ensamblado no permitido.

---

### Historia de usuario 5: puerta de cobertura y herramientas locales reproducibles (Prioridad: P3)

Como desarrollador, quiero restaurar con un solo comando las herramientas locales del repositorio y obtener un informe de cobertura fusionado de toda la solución siguiendo la guía de comandos, para poder comprobar en local las puertas de calidad 3 y 4.

**Por qué esta prioridad**: la puerta de cobertura es obligatoria, pero mientras no exista código de negocio su resultado es solo informativo (ver «Excepción constitucional aceptada»). Lo importante en esta feature es que el mecanismo exista y sea reproducible.

**Prueba independiente**: en un clon limpio se restauran las herramientas, se ejecutan los comandos documentados y se obtiene un único informe fusionado con el porcentaje de cobertura de líneas de la solución.

**Escenarios de aceptación**:

1. **Dado** un clon limpio del repositorio, **Cuando** se restauran las herramientas locales, **Entonces** quedan disponibles `dotnet-ef` 10.0.14 y `dotnet-reportgenerator-globaltool` 5.5.12 sin instalar nada de forma global.
2. **Dada** la guía de comandos del repositorio (`CLAUDE.md`), **Cuando** un desarrollador sigue los comandos de la puerta de cobertura, **Entonces** se generan informes Cobertura de cada proyecto de pruebas y un informe fusionado de toda la solución.
3. **Dada** una ejecución anterior de la puerta, **Cuando** se vuelve a ejecutar, **Entonces** el informe fusionado solo incluye los resultados de la ejecución actual.
4. **Dado** el informe fusionado, **Cuando** se aplica el umbral de cobertura de líneas del 80 %, **Entonces** se muestra el resultado frente al umbral. En esta feature, no alcanzarlo no bloquea la integración (excepción documentada).
5. **Dados** los informes generados, **Cuando** se revisa el estado de git, **Entonces** ningún informe de cobertura ni de pruebas aparece como archivo pendiente de versionar.

---

### Casos límite

- **Proyecto de pruebas sin pruebas**: la plataforma de pruebas termina por defecto con un código de error cuando un proyecto ejecuta cero pruebas. La solución debe tolerarlo para que la suite no falle mientras los proyectos estén vacíos.
- **Advertencias al activar la compilación estricta**: si aparecen advertencias en el código existente, se corrigen en el código. No se suprimen ni se desactivan reglas para salir del paso.
- **Dependencia transitiva**: el proveedor de PostgreSQL arrastra `Microsoft.EntityFrameworkCore.Relational`. Si Application llega a usar tipos de cualquiera de los dos, la prueba de lista blanca debe fallar.
- **Paquete declarado pero sin uso**: la regla verifica los ensamblados que el ensamblado compilado de Application referencia realmente. Un paquete declarado cuyo código no se usa no altera el binario y queda fuera del alcance de la prueba.
- **Espacio de nombres de controladores vacío**: tras borrar el controlador de ejemplo, la prueba que impide a los controladores depender de Infrastructure se ejecuta sobre un conjunto vacío. Debe seguir ejecutándose y pasar, no desactivarse.
- **Ensamblados de producción vacíos**: Domain, Application e Infrastructure se quedan sin tipos al borrar `Class1`. Deben seguir compilando y las pruebas de arquitectura deben poder cargarlos.
- **Informes residuales**: los informes de cobertura de ejecuciones anteriores no deben mezclarse en la fusión.
- **Cobertura por debajo del umbral**: en esta feature el resultado del umbral es informativo. A partir de la siguiente feature, el umbral vuelve a ser bloqueante.
- **TDD sobre una regla sin código en riesgo**: la fase Red de la nueva prueba de arquitectura se demuestra introduciendo temporalmente en Application el uso de un ensamblado no permitido. Ese cambio se revierte antes de integrar.

## Requisitos _(obligatorio)_

### Requisitos funcionales

**Proyectos de pruebas (principio VII)**

- **FR-001**: Ninguno de los cinco proyectos de pruebas DEBE referenciar `Microsoft.NET.Test.Sdk` ni `xunit.runner.visualstudio`.
- **FR-002**: El proyecto de pruebas de arquitectura DEBE usar `NetArchTest.eNhancedEdition` 1.4.5 en lugar de `NetArchTest.Rules`. Las pruebas existentes DEBEN conservar su intención y sus nombres.
- **FR-003**: Los cinco proyectos de pruebas DEBEN referenciar `AwesomeAssertions` 9.6.0.
- **FR-004**: Solo `MiRestaurante.Application.UnitTests` DEBE referenciar `Moq` 4.21.0. Los demás proyectos de pruebas NO DEBEN referenciarlo en esta feature.
- **FR-005**: Ejecutar toda la suite de la solución NO DEBE fallar porque un proyecto de pruebas no contenga pruebas. La tolerancia DEBE configurarse en un único lugar común a toda la solución.

**Compilación y configuración común (principio VIII)**

- **FR-006**: DEBE existir en la raíz del repositorio un archivo de configuración común de compilación que active `TreatWarningsAsErrors` para todos los proyectos de la solución, de producción y de pruebas.
- **FR-007**: Ese archivo común DEBE fijar también el framework objetivo (`net10.0`), la nulabilidad habilitada y los usings implícitos. Los nueve archivos de proyecto NO DEBEN repetir esas propiedades. El archivo común NO DEBE centralizar ningún otro ajuste, salvo la tolerancia de FR-005 si el plan la ubica ahí. Los ajustes y paquetes propios de las pruebas (`OutputType` `Exe`, `IsPackable`, `using Xunit`, `xunit.v3`, `coverlet.MTP`, `AwesomeAssertions`) se quedan en cada proyecto de pruebas, y no se crea un segundo archivo común para `tests/`.
- **FR-008**: La solución DEBE compilar sin errores ni advertencias. Ninguna advertencia DEBE resolverse suprimiéndola o desactivando su regla.

**Versiones de paquetes (stack)**

- **FR-009**: `Microsoft.AspNetCore.OpenApi` DEBE estar en la versión 10.0.14.
- **FR-010**: `Microsoft.AspNetCore.Mvc.Testing` DEBE estar en la versión 10.0.14.

**Herramientas locales y puerta de cobertura (principio VII y puertas de calidad)**

- **FR-011**: DEBE existir un manifiesto de herramientas locales en `.config/dotnet-tools.json` con `dotnet-ef` 10.0.14 y `dotnet-reportgenerator-globaltool` 5.5.12, restaurable con un solo comando.
- **FR-012**: La ejecución de la suite con medición de cobertura DEBE producir un informe en formato Cobertura por cada proyecto de pruebas.
- **FR-013**: Los informes Cobertura DEBEN fusionarse con ReportGenerator en un único informe de toda la solución, aplicando el umbral `lineCoverage=80`. El porcentaje DEBE calcularse solo sobre los cuatro ensamblados de producción (`MiRestaurante.Domain`, `MiRestaurante.Application`, `MiRestaurante.Infrastructure` y `MiRestaurante.Api`), filtrados de forma explícita. Los ensamblados de pruebas y de terceros NO DEBEN contar, y NO DEBEN excluirse clases ni archivos de producción.
- **FR-014**: Los informes de cobertura DEBEN generarse en una ubicación única que se limpia antes de cada ejecución de la puerta, para no fusionar resultados de ejecuciones anteriores.
- **FR-015**: Los informes de pruebas y de cobertura generados NO DEBEN quedar como archivos pendientes de versionar (DEBEN estar ignorados por git).
- **FR-016**: Los comandos para restaurar herramientas, ejecutar la suite con cobertura, limpiar los informes previos y fusionarlos con el umbral DEBEN documentarse en la sección «Comandos» de `CLAUDE.md`. No se añaden scripts, Makefiles ni objetivos de compilación para ello.
- **FR-017**: En esta feature, no alcanzar el umbral del 80 % NO DEBE bloquear la integración. El comando de fusión DEBE seguir aplicando el umbral y mostrando el resultado (ver «Excepción constitucional aceptada»).

**Limpieza del esqueleto**

- **FR-018**: DEBEN eliminarse los tres archivos `Class1.cs` de Domain, Application e Infrastructure.
- **FR-019**: DEBEN eliminarse los cuatro archivos `UnitTest1.cs` de los proyectos de pruebas.
- **FR-020**: DEBEN eliminarse el modelo `WeatherForecast` y su controlador, y no DEBE quedar ninguna referencia a ellos.
- **FR-021**: El archivo de peticiones HTTP de la API DEBE conservarse con solo la variable de la dirección del host, sin peticiones.
- **FR-022**: Tras la limpieza, la API DEBE seguir arrancando en Development y publicando el documento OpenAPI.

**Arquitectura (principio II)**

- **FR-023**: El proyecto de pruebas de arquitectura DEBE incluir una prueba que verifique que el ensamblado compilado de Application solo referencia ensamblados de una lista blanca: ensamblados del framework (`System*`, `netstandard`, `mscorlib`), `MiRestaurante.Domain`, `Microsoft.EntityFrameworkCore` y `Microsoft.Extensions.Logging.Abstractions`. Este último se admite de forma explícita y por nombre exacto, no por prefijo, porque es una dependencia transitiva del paquete `Microsoft.EntityFrameworkCore` y la constitución exige `ILogger<T>` para el registro.
- **FR-024**: Esa prueba DEBE fallar si Application referencia `Microsoft.EntityFrameworkCore.Relational`, cualquier ensamblado de Npgsql o cualquier otro ensamblado que no esté en la lista blanca, y su mensaje DEBE indicar qué ensamblados no permitidos encontró.
- **FR-025**: La nueva prueba DEBE seguir la nomenclatura `{Metodo}_Should_{Resultado}_When_{Escenario}`, tener una sola aserción lógica y no contener lógica condicional en el cuerpo de la prueba.
- **FR-026**: Las pruebas de arquitectura existentes (dependencias entre capas, controladores sin Infrastructure, Domain solo con ensamblados del framework) DEBEN seguir ejecutándose y pasando.

## Criterios de éxito _(obligatorio)_

### Resultados medibles

- **SC-001**: 0 de los 5 proyectos de pruebas referencian paquetes que la constitución prohíbe, y el 100 % de las versiones de paquetes y herramientas tocadas en esta feature coinciden con la tabla del stack.
- **SC-002**: La compilación de la solución termina con 0 errores y 0 advertencias. Una advertencia introducida a propósito en cualquiera de los 9 proyectos hace fallar la compilación.
- **SC-003**: La suite completa se ejecuta con un único comando y termina con éxito, aunque 4 de los 5 proyectos de pruebas no contengan pruebas.
- **SC-004**: Quedan 0 archivos de plantilla (de 9) y 0 referencias a ellos en el repositorio.
- **SC-005**: En el 100 % de los intentos, introducir en Application el uso de un ensamblado fuera de la lista blanca (como mínimo, uno relacional y uno de Npgsql) hace fallar la suite de arquitectura.
- **SC-006**: Un desarrollador con un clon limpio obtiene el informe de cobertura fusionado de toda la solución siguiendo únicamente los comandos documentados, sin instalar herramientas globales ni pasos adicionales.
- **SC-007**: Tras ejecutar dos veces seguidas la puerta de cobertura, el informe fusionado contiene solo los resultados de la última ejecución, y git no muestra ningún archivo generado como pendiente.

## Excepción constitucional aceptada

- **Regla afectada**: principio VII (cobertura de líneas ≥ 80 % sobre toda la solución) y puerta de calidad 4 («ReportGenerator … `-minimumCoverageThresholds:lineCoverage=80`, terminando con código de salida 0» antes de integrar en `main`).
- **Desviación**: en esta feature, la puerta de cobertura se monta completa y aplica el umbral del 80 %, pero no alcanzarlo no bloquea la integración.
- **Motivo**: sin código de negocio, el único código medible es la raíz de composición de la API (`Program.cs`), que ninguna prueba ejecuta. El responsable del proyecto rechazó añadir una prueba de humo o excluir la raíz de composición solo para cumplir el umbral.
- **Vigencia**: termina con esta feature. A partir de la primera feature con código de negocio, la puerta 4 vuelve a ser bloqueante sin excepciones.
- **Decisión**: confirmada de forma explícita por el responsable del proyecto durante la entrevista de especificación, tras advertirle del conflicto. El plan DEBE registrarla en el «Constitution Check» y en «Complexity Tracking».

## Supuestos

- La rama de trabajo es la actual (`speckit-test`); no se crea una rama nueva.
- Las versiones exactas son las de la tabla del stack de la constitución (verificadas el 2026-11-04): `AwesomeAssertions` 9.6.0, `Moq` 4.21.0, `NetArchTest.eNhancedEdition` 1.4.5, `dotnet-ef` 10.0.14, `dotnet-reportgenerator-globaltool` 5.5.12, `Microsoft.AspNetCore.OpenApi` 10.0.14 y `Microsoft.AspNetCore.Mvc.Testing` 10.0.14.
- Las versiones se siguen fijando en cada archivo de proyecto. No se introduce gestión centralizada de paquetes porque no se pidió y añadiría una pieza más (principio I).
- La tolerancia a proyectos sin pruebas es temporal y debería retirarse cuando los cinco proyectos tengan pruebas reales. Retirarla queda fuera del alcance de esta feature.
- La regla de arquitectura de Application se verifica sobre los ensamblados que referencia el binario compilado, igual que la regla existente de Domain. El análisis por espacios de nombres no sirve porque el núcleo de EF Core y la parte relacional comparten el espacio `Microsoft.EntityFrameworkCore`.
- Si Application necesitara en el futuro otro ensamblado además de los de FR-023, ampliar la lista blanca exige primero enmendar el principio II.
- Quedan fuera del alcance: `compose.yaml`, PostgreSQL, Testcontainers, Respawn, `.editorconfig`, el análisis de estilo durante la compilación, CI/CD, cualquier endpoint de negocio o de salud, la interfaz Scalar, ProblemDetails con `IExceptionHandler` y CORS en Development. Cada uno llegará con la feature que lo necesite. Las tres últimas piezas llegan con la primera feature que exponga endpoints de negocio.
- La limpieza de comentarios de plantilla en `Program.cs` queda fuera del alcance, salvo que impida compilar sin advertencias.
- Los informes de cobertura van a una carpeta de resultados ignorada por git. Si la ubicación elegida no está ya cubierta por `.gitignore`, se añade la entrada correspondiente.
