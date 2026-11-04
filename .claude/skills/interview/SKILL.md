---
name: interview
description: Indicaciones sobre la forma en que debe enrevistar al developer para alcanzar un entendimiento compartido y completo sobre cada aspecto de la solicitud.
metadata:
  version: "2.0.0"
  language: "es"
  opencode/slash: "true"
---

Revisa y analiza detalladamente la solicitud del desarrollador y entrevístalo sin
descanso sobre cada aspecto de la solicitud hasta alcanzar un entendimiento
compartido y completo.

Recorre cada rama del árbol de decisiones resolviendo dependencias entre decisiones
una por una — no pases a la siguiente hasta que la anterior esté resuelta.

Si una pregunta puede responderse explorando el codebase, explora el codebase en
lugar de preguntar.

Para cada pregunta que sí debas hacerle al desarrollador, usa la herramienta nativa de preguntas estructuradas del agente si existe:

| Agente                                                   | Herramienta       | Contexto y recomendación            | Marca de recomendada              | Respuesta libre                               |
| -------------------------------------------------------- | ----------------- | ----------------------------------- | --------------------------------- | --------------------------------------------- |
| GitHub Copilot (VS Code)                                 | `askQuestions`    | Campo `message`                     | `recommended: true`               | Opción explícita + `allowFreeformInput: true` |
| GitHub Copilot CLI                                       | `ask_user`        | Campo `message` del formulario      | `default` del campo               | Nativa                                        |
| Claude Code                                              | `AskUserQuestion` | Texto en el chat antes de invocarla | Primera opción + ` (Recomendada)` | Nativa ("Other")                              |
| OpenCode                                                 | `question`        | Texto en el chat antes de invocarla | Primera opción + ` (Recomendada)` | Nativa                                        |
| Sin herramienta (Codex, Cursor, Gemini CLI, Aider, etc.) | Texto plano       | En el mismo mensaje                 | ` (Recomendada)` en el label      | Opción "Otra" explícita                       |

Sigue OBLIGATORIAMENTE este formato:

**question**: La pregunta en sí, concisa y directa.

**message** (Markdown): Compuesto por tres bloques en este orden:

1. **Contexto / explicación**: 2-4 oraciones que expliquen el tema que se está
   decidiendo, por qué importa y qué tensiones existen (ej. consistencia vs. seguridad,
   rendimiento vs. mantenibilidad). El desarrollador debe entender el panorama antes
   de ver las opciones.
2. **Recomendación**: Un párrafo con el formato exacto:
   > **Recomendación → Opción N — «label de la opción»**: [justificación de por qué
   >
   > > es la mejor elección en este contexto, citando la evidencia del codebase si existe].
3. (Nada más; los pros/contras van en cada opción, no aquí.)

**options**: Lista de opciones con:

- `label`: nombre/descripción corta de la opción.
- `description`: pros y contras concretos de esa opción (1-3 líneas). Usa prefijos
  claros: "✅ Beneficio: ..." y "⚠️ Contra: ...".
- `recommended: true` en la opción recomendada (solo una).

**Opción abierta obligatoria**: La ÚLTIMA opción de TODA pregunta debe ser siempre:

- label: "Otra — escribe tu respuesta"
- description: "Responde libremente si ninguna opción anterior se ajusta a tu caso."
- Sin `recommended`.

Todas las preguntas y respuestas SIEMPRE deben ser en español.

Una vez completada la entrevista, confirma al desarrollador que tienes suficiente
información y que el proceso de especificación puede comenzar.
