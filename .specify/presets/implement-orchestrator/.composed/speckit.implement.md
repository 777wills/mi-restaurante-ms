---
description: Execute the implementation plan by processing and executing all tasks
  defined in tasks.md
scripts:
  sh: scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks
  ps: scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks
  py: scripts/python/check_prerequisites.py --json --require-tasks --include-tasks
---

## Rol obligatorio: orquestador

En este comando actúas como **orquestador**. Coordinas a subagentes especializados; **no escribes código**. Las instrucciones de esta sección prevalecen sobre las del resto del comando cuando hablen de quién ejecuta una tarea.

### Lo que puedes y no puedes hacer

- **Puedes**: leer cualquier archivo, ejecutar los scripts de `.specify/scripts/`, hacer preguntas al desarrollador y editar **solo** el `tasks.md` de la feature (marcar `[X]` y añadir fases de revisión).
- **No puedes**: crear ni modificar código, pruebas, configuración, dependencias ni archivos de ignorados; tampoco ejecutar build, pruebas o herramientas del proyecto. Todo eso se delega. Si te descubres a punto de hacerlo, delega.

### Subagentes disponibles

| Subagente | Recibe |
| --- | --- |
| `test-developer` | Tareas que crean o modifican pruebas o piden verlas fallar (Red); verificaciones de cobertura |
| `developer` | Tareas de código de producción (Green y Refactor), configuración, dependencias, migraciones, archivos de ignorados (paso 4 del comando base) y verificaciones de línea base o de paquetes |
| `reviewer` | Revisiones en cada punto de control de historia y la revisión final (solo lectura) |
| `spec-auditor` | La auditoría de convergencia al terminar todas las tareas |

Si una tarea no encaja claramente en una fila, decide tú según las rutas de archivo que toca y deja el motivo en la traza.

### Reglas de comunicación

- Los subagentes **no** se comunican entre sí ni se invocan unos a otros: toda coordinación pasa por ti.
- Cada subagente termina con un informe con los campos `ESTADO`, `TAREAS`, `ARCHIVOS`, `EVIDENCIA`, `NECESITA DE OTRO AGENTE` y `PREGUNTAS PARA EL DESARROLLADOR`. Si falta, pídeselo de nuevo.
- `NECESITA DE OTRO AGENTE` → tú decides si delegas esa necesidad, en qué orden, o si preguntas al desarrollador.
- `PREGUNTAS PARA EL DESARROLLADOR` → se las haces tú y transmites la respuesta en la siguiente delegación.

### Cómo delegar (reemplaza la ejecución de los pasos 6 a 8 del comando base)

1. Recorre las fases de `tasks.md` en orden. Dentro de cada fase, agrupa en **un bloque** las tareas consecutivas que van al mismo subagente.
2. Respeta el TDD: dentro de una historia, el bloque Red del `test-developer` va antes que el bloque Green del `developer`.
3. Bloques de tareas marcadas `[P]` que van a subagentes distintos y tocan archivos distintos pueden delegarse **en paralelo**; todo lo demás, en secuencia.
4. Cada delegación incluye: ruta de la feature (`FEATURE_DIR`), el texto completo de cada tarea con su ID, los artefactos que debe leer, las restricciones relevantes de la constitución y la respuesta del desarrollador a preguntas previas, si las hubo.
5. Al recibir el informe:
   - Marca `[X]` solo las tareas reportadas con ✓ y con evidencia.
   - `FALLIDO` o `BLOQUEADO` en una tarea no paralela: no sigas con las dependientes. Decide si vuelves a delegar con más contexto, delegas en otro rol o preguntas al desarrollador.

### Punto de control de cada historia

Al terminar las tareas de una historia de usuario (su «Punto de control»):

1. Delega en `test-developer` una verificación de cobertura. Por cada línea sin cubrir de los archivos de la feature, decide:
   - si responde a un requisito de `spec.md` → delega la prueba al `test-developer` y anota la desviación de TDD en la traza;
   - si no responde a ningún requisito → delega su eliminación al `developer`;
   - si no está claro → pregunta al desarrollador.
2. Delega en `reviewer` la revisión del alcance de la historia.
3. Registra cada hallazgo accionable como tarea nueva al final de `tasks.md`, en una sección `## Fase N: Revisión (punto de control USx)` con IDs consecutivos al mayor existente. No reescribas ni renumeres tareas existentes. Los hallazgos que requieren una decisión de alcance se preguntan antes de registrarlos.
4. Delega esas tareas con las mismas reglas y márcalas al terminar.

### Cierre de la feature (antes del «Completion Report» del comando base)

1. Delega en `reviewer` la **revisión final** de toda la feature (incluye la mutación informativa, si está configurada) y trata sus hallazgos como en el punto de control.
2. Delega en `spec-auditor` la auditoría de convergencia.
   - `converged` → termina.
   - `tasks_appended` → ejecuta esas tareas con las reglas anteriores y vuelve a delegar la auditoría.
   - Detente y pregunta al desarrollador si una vuelta repite hallazgos de la anterior (sin progreso) o si un hallazgo exige una decisión suya.

### Traza en el chat (siempre activa)

Escribe en el chat una línea por evento, con exactamente estos formatos, para que el desarrollador pueda seguir el flujo:

```text
🎬 [ORQUESTADOR] Inicio · feature <carpeta> · <n> tareas pendientes
📋 [FASE] <n>: <nombre> · bloques: <subagente>(<IDs>), …
➡️ [DELEGA → <subagente>] <IDs> · <resumen> · motivo: <por qué este subagente>
⬅️ [RESPUESTA ← <subagente>] <ESTADO> · <resumen en una línea>
🧭 [DECISIÓN] <qué decides y por qué>
✅ [TAREAS] Marcadas [X]: <IDs>
🔍 [PUNTO DE CONTROL] <historia> · cobertura <x %> · <n> hallazgos
❓ [PREGUNTA] <pregunta al desarrollador>
🔁 [CONVERGENCIA] Vuelta <n> · <converged | tareas añadidas: IDs>
🏁 [ORQUESTADOR] Fin · <resultado> · tareas <hechas>/<total>
```

En el informe final, añade una tabla con cada subagente, las tareas que ejecutó y los archivos que tocó.

Cuando termines de leer esta sección, continúa con el resto de las instrucciones de este comando aplicando estas reglas.



## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Pre-Execution Checks

**Check for extension hooks (before implementation)**:
- Check if `.specify/extensions.yml` exists in the project root.
- If it exists, read it and look for entries under the `hooks.before_implement` key
- If the YAML cannot be parsed or is invalid, do not skip silently: tell the user that `.specify/extensions.yml` could not be read (include the parser error) and that no hooks were checked, including any mandatory (`optional: false`) hooks registered there, then continue normally
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Pre-Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```
  - **Mandatory hook** (`optional: false`):
    ```
    ## Extension Hooks

    **Automatic Pre-Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}

    Wait for the result of the hook command before proceeding to the Outline.
    ```
    After emitting the block above you MUST actually invoke the hook and wait for it to finish before continuing. Run it the same way you would run the command yourself in this agent/session (the invocation may differ from the literal `{command}` id shown above, e.g. a skills-mode agent runs it as `/skill:speckit-...` or `$speckit-...`). Emitting the block alone does not run the hook.
- If no hooks are registered or `.specify/extensions.yml` does not exist, skip silently

## Outline

1. Run `{SCRIPT}` from repo root and parse FEATURE_DIR and AVAILABLE_DOCS list. All paths must be absolute. For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

2. **Check checklists status** (if FEATURE_DIR/checklists/ exists):
   - Treat checklist markers as a read-only gate: scan checkbox state, report status, and ask before proceeding when needed; do NOT modify checklist files or markers
   - `checklists/requirements.md` is the built-in spec-quality checklist maintained by `__SPECKIT_COMMAND_SPECIFY__` and `__SPECKIT_COMMAND_CLARIFY__`; custom checklists generated by `__SPECKIT_COMMAND_CHECKLIST__` are reviewer-owned requirements-quality review artifacts
   - For custom checklists, `[x]` means the reviewer determined the requirements-quality criterion is satisfied; it does NOT mean implementation work is complete
   - Scan all checklist files in the checklists/ directory
   - For each checklist, count:
     - Total items: All lines matching `- [ ]` or `- [X]` or `- [x]`
     - Checked items: Lines matching `- [X]` or `- [x]`
     - Unchecked items: Lines matching `- [ ]`
   - Create a status table:

     ```text
     | Checklist | Total | Checked | Unchecked | Status |
     |-----------|-------|---------|-----------|--------|
     | ux.md     | 12    | 12      | 0         | ✓ PASS |
     | test.md   | 8     | 5       | 3         | ✗ FAIL |
     | security.md | 6   | 6       | 0         | ✓ PASS |
     ```

   - Calculate overall status:
     - **PASS**: All checklists have 0 unchecked items
     - **FAIL**: One or more checklists have unchecked items

   - **If any checklist has unchecked items**:
     - Display the table with unchecked item counts
     - **STOP** and ask: "Some checklists have unchecked items. Do you want to proceed with implementation anyway? (yes/no)"
     - Wait for user response before continuing
     - If user says "no" or "wait" or "stop", halt execution
     - If user says "yes" or "proceed" or "continue", proceed to step 3

   - **If all checklists are checked**:
     - Display the table showing all checklists passed
     - Automatically proceed to step 3

3. Load and analyze the implementation context:
   - **REQUIRED**: Read tasks.md for the complete task list and execution plan
   - **REQUIRED**: Read plan.md for tech stack, architecture, and file structure
   - **IF EXISTS**: Read data-model.md for entities and relationships
   - **IF EXISTS**: Read contracts/ for API specifications and test requirements
   - **IF EXISTS**: Read research.md for technical decisions and constraints
   - **IF EXISTS**: Read /memory/constitution.md for governance constraints
   - **IF EXISTS**: Read quickstart.md for integration scenarios

4. **Project Setup Verification**:
   - **REQUIRED**: Create/verify ignore files based on actual project setup:

   **Detection & Creation Logic**:
   - Check if the following command succeeds to determine if the repository is a git repo (create/verify .gitignore if so):

     ```sh
     git rev-parse --git-dir 2>/dev/null
     ```

   - Check if Dockerfile* exists or Docker in plan.md → create/verify .dockerignore
   - Check if .eslintrc* exists → create/verify .eslintignore
   - Check if eslint.config.* exists → ensure the config's `ignores` entries cover required patterns
   - Check if .prettierrc* exists → create/verify .prettierignore
   - Check if .npmrc or package.json exists → create/verify .npmignore (if publishing)
   - Check if terraform files (*.tf) exist → create/verify .terraformignore
   - Check if .helmignore needed (helm charts present) → create/verify .helmignore

   **If ignore file already exists**: Verify it contains essential patterns, append missing critical patterns only
   **If ignore file missing**: Create with full pattern set for detected technology

   **Common Patterns by Technology** (from plan.md tech stack):
   - **Node.js/JavaScript/TypeScript**: `node_modules/`, `dist/`, `build/`, `*.log`, `.env*`
   - **Python**: `__pycache__/`, `*.pyc`, `.venv/`, `venv/`, `dist/`, `*.egg-info/`
   - **Java**: `target/`, `*.class`, `*.jar`, `.gradle/`, `build/`
   - **C#/.NET**: `bin/`, `obj/`, `*.user`, `*.suo`, `packages/`
   - **Go**: `*.exe`, `*.test`, `vendor/`, `*.out`
   - **Ruby**: `.bundle/`, `log/`, `tmp/`, `*.gem`, `vendor/bundle/`
   - **PHP**: `vendor/`, `*.log`, `*.cache`, `*.env`
   - **Rust**: `target/`, `debug/`, `release/`, `*.rs.bk`, `*.rlib`, `*.prof*`, `.idea/`, `*.log`, `.env*`
   - **Kotlin**: `build/`, `out/`, `.gradle/`, `.idea/`, `*.class`, `*.jar`, `*.iml`, `*.log`, `.env*`
   - **C++**: `build/`, `bin/`, `obj/`, `out/`, `*.o`, `*.so`, `*.a`, `*.exe`, `*.dll`, `.idea/`, `*.log`, `.env*`
   - **C**: `build/`, `bin/`, `obj/`, `out/`, `*.o`, `*.a`, `*.so`, `*.exe`, `*.dll`, `autom4te.cache/`, `config.status`, `config.log`, `.idea/`, `*.log`, `.env*`
   - **Swift**: `.build/`, `DerivedData/`, `*.swiftpm/`, `Packages/`
   - **R**: `.Rproj.user/`, `.Rhistory`, `.RData`, `.Ruserdata`, `*.Rproj`, `packrat/`, `renv/`
   - **Universal**: `.DS_Store`, `Thumbs.db`, `*.tmp`, `*.swp`, `.vscode/`, `.idea/`

   **Tool-Specific Patterns**:
   - **Docker**: `node_modules/`, `.git/`, `Dockerfile*`, `.dockerignore`, `*.log*`, `.env*`, `coverage/`
   - **ESLint**: `node_modules/`, `dist/`, `build/`, `coverage/`, `*.min.js`
   - **Prettier**: `node_modules/`, `dist/`, `build/`, `coverage/`, `package-lock.json`, `yarn.lock`, `pnpm-lock.yaml`
   - **Terraform**: `.terraform/`, `*.tfstate*`, `*.tfvars`, `.terraform.lock.hcl`
   - **Kubernetes/k8s**: `*.secret.yaml`, `secrets/`, `.kube/`, `kubeconfig*`, `*.key`, `*.crt`

5. Parse tasks.md structure and extract:
   - **Task phases**: Setup, Tests, Core, Integration, Polish
   - **Task dependencies**: Sequential vs parallel execution rules
   - **Task details**: ID, description, file paths, parallel markers [P]
   - **Execution flow**: Order and dependency requirements

6. Execute implementation following the task plan:
   - **Phase-by-phase execution**: Complete each phase before moving to the next
   - **Respect dependencies**: Run sequential tasks in order, parallel tasks [P] can run together
   - **Follow TDD approach**: Execute test tasks before their corresponding implementation tasks
   - **File-based coordination**: Tasks affecting the same files must run sequentially
   - **Validation checkpoints**: Verify each phase completion before proceeding

7. Implementation execution rules:
   - **Setup first**: Initialize project structure, dependencies, configuration
   - **Tests before code**: If you need to write tests for contracts, entities, and integration scenarios
   - **Core development**: Implement models, services, CLI commands, endpoints
   - **Integration work**: Database connections, middleware, logging, external services
   - **Polish and validation**: Unit tests, performance optimization, documentation

8. Progress tracking and error handling:
   - Report progress after each completed task
   - Halt execution if any non-parallel task fails
   - For parallel tasks [P], continue with successful tasks, report failed ones
   - Provide clear error messages with context for debugging
   - Suggest next steps if implementation cannot proceed
   - **IMPORTANT** For completed tasks, make sure to mark the task off as [X] in the tasks file.

9. Completion validation:
   - Verify all required tasks are completed
   - Check that implemented features match the original specification
   - Validate that tests pass and coverage meets requirements
   - Confirm the implementation follows the technical plan

Note: This command assumes a complete task breakdown exists in tasks.md. If tasks are incomplete or missing, suggest running `__SPECKIT_COMMAND_TASKS__` first to regenerate the task list.

## Mandatory Post-Execution Hooks

**You MUST complete this section before reporting completion to the user.**

Check if `.specify/extensions.yml` exists in the project root.
- If it does not exist, or no hooks are registered under `hooks.after_implement`, skip to the Completion Report.
- If it exists, read it and look for entries under the `hooks.after_implement` key.
- If the YAML cannot be parsed or is invalid, do not skip silently: tell the user that `.specify/extensions.yml` could not be read (include the parser error) and that no hooks were checked, including any mandatory (`optional: false`) hooks registered there, then continue to the Completion Report.
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Mandatory hook** (`optional: false`) — **You MUST emit `EXECUTE_COMMAND:` for each mandatory hook**:
    ```
    ## Extension Hooks

    **Automatic Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}
    ```
    After emitting the block above you MUST actually invoke the hook and wait for it to finish before continuing. Run it the same way you would run the command yourself in this agent/session (the invocation may differ from the literal `{command}` id shown above, e.g. a skills-mode agent runs it as `/skill:speckit-...` or `$speckit-...`). Emitting the block alone does not run the hook.
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```

## Completion Report

Report final status with summary of completed work.

## Done When

- [ ] All tasks in tasks.md completed and marked `[X]`
- [ ] Implementation validated against specification, plan, and test coverage
- [ ] Extension hooks dispatched or skipped according to the rules in Mandatory Post-Execution Hooks above
- [ ] Completion reported to user with summary of completed work
