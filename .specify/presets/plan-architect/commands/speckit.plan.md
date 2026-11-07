## Regla obligatoria: investigación con el subagente `architect`

En la **Fase 0 (Outline & Research)** de este comando, la investigación de cada duda técnica (`NEEDS CLARIFICATION`, elección de librerías, patrones o versiones) se delega en el subagente `architect`, que es de solo lectura.

1. Reúne las dudas técnicas del *Technical Context* y entrégalas al `architect` en una sola delegación, con la ruta de la feature y los artefactos que debe leer.
2. El `architect` devuelve, por cada decisión, opciones, versiones verificadas y una recomendación. No decide por el desarrollador.
3. Tú presentas al desarrollador las decisiones abiertas siguiendo la skill `interview` (`.claude/skills/interview/SKILL.md`): una pregunta por decisión, con la recomendación del `architect` marcada y su justificación.
4. Solo con las decisiones aprobadas escribes `research.md` y continúas con el plan. Los artefactos los escribes tú, no el subagente.

Escribe en el chat una línea al delegar y otra al recibir el informe:

```text
➡️ [DELEGA → architect] <n> dudas técnicas · motivo: investigación de la Fase 0
⬅️ [RESPUESTA ← architect] <ESTADO> · <n> decisiones · <n> preguntas para el desarrollador
```

Cuando termines de leer esta sección, continúa con el resto de las instrucciones de este comando.
