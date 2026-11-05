# Lista de calidad de la especificación: CRUD de platos

**Propósito**: validar que la especificación está completa y tiene calidad antes de pasar a la planificación
**Creada**: 2026-11-04
**Feature**: [spec.md](../spec.md)

## Calidad del contenido

- [x] Sin detalles de implementación (lenguajes, frameworks, APIs)
- [x] Centrada en el valor para el usuario y las necesidades del negocio
- [x] Redactada para partes interesadas no técnicas
- [x] Todas las secciones obligatorias completadas

## Completitud de los requisitos

- [x] No quedan marcadores [NEEDS CLARIFICATION]
- [x] Los requisitos son verificables y no ambiguos
- [x] Los criterios de éxito son medibles
- [x] Los criterios de éxito son agnósticos de la tecnología
- [x] Todos los escenarios de aceptación están definidos
- [x] Se han identificado los casos límite
- [x] El alcance está claramente delimitado
- [x] Se han identificado dependencias y supuestos

## Preparación de la feature

- [x] Todos los requisitos funcionales tienen criterios de aceptación claros
- [x] Los escenarios de usuario cubren los flujos principales
- [x] La feature cumple los resultados medibles definidos en los criterios de éxito
- [x] No se filtran detalles de implementación en la especificación

## Notas

- El producto es una API REST: las rutas, los códigos de estado y las propiedades JSON son su interfaz de usuario y se especifican como tal. Los mecanismos citados (prefijo `/api/v1/`, ProblemDetails, 400/404/422, hora controlable, semilla en migraciones, Respawn, cobertura) son obligaciones de la constitución (principios II, III, IV, V, VI y VII), no decisiones de implementación. La nota del encabezado de la spec lo justifica.
- SC-006 nombra herramientas porque reproduce las puertas de calidad de la constitución.
- Desviación de la recomendación aceptada en la entrevista: borrado lógico en lugar de físico (FR-020), con unicidad del nombre solo entre platos activos (FR-015).
- Respawn, pospuesto en la feature 003 (FR-023), se integra en esta feature (FR-028).
- Las URLs de imagen semilla se obtuvieron de las API de Wikipedia y Wikimedia Commons; no son inventadas.
- Todas las decisiones abiertas se resolvieron en la entrevista previa (sección «Aclaraciones»); no quedan marcadores.
