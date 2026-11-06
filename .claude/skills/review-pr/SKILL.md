---
name: review-pr
description: Revisa la rama actual antes de abrir una solicitud de incorporación (pull request). Delega en el subagente reviewer los archivos que cambian frente a la rama base y clasifica cada observación (aceptar, cuestionar o descartar) para que el desarrollador decida.
argument-hint: "[rama base, por defecto main]"
---

# Revisión de la rama antes de la PR

La IA actúa como **primer revisor**; la decisión final sobre cada observación es del desarrollador. Responde siempre en español.

## Pasos

1. **Alcance**: la rama base es el argumento recibido o `main`. Ejecuta `git diff --name-only <base>...HEAD` y `git diff --stat <base>...HEAD`. Si no hay cambios, informa y termina.
2. **Delegación**: delega en el subagente `reviewer` con este alcance: «diff de rama `<base>...HEAD`», la lista de archivos cambiados y, si la rama corresponde a una carpeta de `specs/`, la ruta de esa feature. Pídele su informe habitual.
3. **Clasificación**: para cada hallazgo del informe propone una clasificación con estos criterios:

   | Clasificación  | Cuándo                                                                                                                                                                                                          |
   | -------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
   | **Aceptar**    | Hay evidencia verificable: viola un DEBE/NO DEBE de la constitución o un requisito de la spec (cita el texto), o lo demuestra una prueba que falla, un mutante superviviente o la salida de un comando          |
   | **Cuestionar** | Es plausible pero falta evidencia, o contradice una decisión registrada en `plan.md`, `research.md` o _Complexity Tracking_. Antes de actuar, se pide reproducirlo (una prueba que falle o un ejemplo concreto) |
   | **Descartar**  | Es una preferencia de estilo que ninguna norma recoge, un falso positivo comprobado contra el código, o una sugerencia que añade complejidad sin justificarla (principio de simplicidad)                        |

4. **Decisión humana**: presenta una tabla con ID, severidad, `archivo:línea`, observación, clasificación propuesta y motivo. Pregunta al desarrollador, hallazgo por hallazgo, si confirma la clasificación.
5. **Resultado**: lista las observaciones aceptadas como acciones a corregir antes de abrir la PR y las cuestionadas con la evidencia que falta. **No corrijas código** en esta skill.

## Traza en el chat

```text
➡️ [DELEGA → reviewer] diff <base>...HEAD · <n> archivos
⬅️ [RESPUESTA ← reviewer] <ESTADO> · <n> hallazgos
🧭 [CLASIFICACIÓN] <n> aceptar · <n> cuestionar · <n> descartar (propuesta)
```
