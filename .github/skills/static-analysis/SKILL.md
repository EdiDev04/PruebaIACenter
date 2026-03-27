---
name: static-analysis
description: Ejecuta análisis estático de código con SonarQube MCP sobre los archivos generados. Parsea issues por severidad y aplica un quality gate. Si hay BLOCKER o CRITICAL, notifica al Orchestrator para bloquear el avance a Fase 3.
argument-hint: "<nombre-feature> [backend|frontend|ambos]"
---

# Skill: static-analysis [Quality Gate]

Ejecuta análisis de código estático con SonarQube MCP sobre los archivos producidos en Fase 2 (Backend ∥ Frontend). Determina si el código puede avanzar a Fase 3 (Tests).

## Definition of Done — verificar al completar

- [ ] `mcp_sonarqube_analyze_code_snippet` ejecutado sobre todos los archivos modificados en la fase
- [ ] Issues clasificados por severidad: BLOCKER, CRITICAL, MAJOR, MINOR, INFO
- [ ] Reporte generado en `docs/output/qa/<feature>-static-analysis.md`
- [ ] Gate evaluado: BLOCKER o CRITICAL → flujo bloqueado y notificado al Orchestrator
- [ ] Análisis automático re-habilitado al finalizar

## Tools SonarQube requeridas

| Tool | Propósito | Aplica a |
|------|----------|----------|
| `mcp_sonarqube_search_my_sonarqube_projects` | Resolver el project key — OBLIGATORIO antes del análisis | Todos |
| `sonarqube_analyze_file` (IDE) | Disparar análisis Roslyn sobre un archivo `.cs` — **herramienta principal para C#** | `.cs` |
| `get_errors` | Leer issues publicados en el panel PROBLEMS de VS Code tras el análisis | `.cs` |
| `sonarqube_list_potential_security_issues` | Consultar vulnerabilidades y hotspots de seguridad (OWASP, CWE) por archivo | `.cs` |
| `mcp_sonarqube_analyze_code_snippet` | Analizar snippets de código — **NO soporta C#** | `.ts`, `.js`, `.html`, `.py` |
| `mcp_sonarqube_get_project_quality_gate_status` | Verificar el estado del quality gate del último scan completo en el servidor | Todos |
| `mcp_sonarqube_get_duplications` | Detectar bloques de código duplicado | `.ts`, `.js` |
| `mcp_sonarqube_get_file_coverage_details` | Obtener detalles de cobertura por archivo | Todos |

## Prerequisito — Lee antes de ejecutar

```
.github/specs/<feature>.spec.md              (para contexto del feature)
.github/instructions/sonarqube_mcp.instructions.md  (reglas de uso del MCP)
lista de archivos creados/modificados en Fase 2
```

## Proceso paso a paso

### Paso 1 — Preparación

1. Resolver el project key usando `mcp_sonarqube_search_my_sonarqube_projects` — nunca asumir el key
2. Recopilar la lista completa de archivos creados/modificados en Fase 2

### Paso 2 — Detección de lenguaje (OBLIGATORIO antes de analizar)

3. **Antes de llamar a cualquier herramienta de análisis**, inspeccionar la extensión de cada archivo modificado y asignar el identificador de lenguaje correcto para SonarQube:

| Extensión | Lenguaje | Valor `language` | Snippet soportado |
|-----------|----------|-----------------|-------------------|
| `.cs` | C# | `"cs"` | ❌ No soportado — usar quality gate |
| `.ts` | TypeScript | `"ts"` | ✅ |
| `.js` | JavaScript | `"js"` | ✅ |
| `.py` | Python | `"py"` | ✅ |
| `.java` | Java | `"java"` | ✅ |
| `.html` | HTML | `"html"` | ✅ |
| `.scss` / `.css` | CSS | `"css"` | ✅ |

> ⚠️ **NUNCA asumir el lenguaje ni usar `"java"` por defecto para archivos `.cs`**. Son lenguajes con reglas completamente distintas. Un `language` incorrecto hace que el motor no aplique las reglas adecuadas y retorna 0 issues de forma engañosa.
>
> ⚠️ **C# no está soportado en `mcp_sonarqube_analyze_code_snippet`** — el servidor MCP no incluye el analizador Roslyn para snippets. Para archivos `.cs`, usar el **Procedimiento C#** descrito en la sección siguiente.

---

## Procedimiento verificado para archivos C# (`.cs`)

> Procedimiento constatado en la práctica como alternativa a `analyze_code_snippet` para el stack .NET de este proyecto.

### Paso C#-1 — Disparar análisis con SonarQube for IDE

Para cada archivo `.cs` a analizar, invocar:

```
sonarqube_analyze_file(filePath: "<ruta-absoluta-del-archivo>.cs")
```

Esto hace que SonarQube for IDE ejecute el analizador Roslyn localmente sobre el archivo y publique los resultados en el panel **PROBLEMS** de VS Code. Es equivalente a la detección automática que ocurre al abrir el archivo en el editor con el plugin activo.

### Paso C#-2 — Leer issues del panel PROBLEMS

Invocar `get_errors` sobre el mismo archivo:

```
get_errors(filePaths: ["<ruta-absoluta-del-archivo>.cs"])
```

Los resultados incluyen:
- **Code Smells**: campos privados no usados, constantes no referenciadas, complejidad ciclomática alta
- **Bugs**: null references, lógica incorrecta, disposables no cerrados
- **Vulnerabilities**: detectadas por el analizador Roslyn con reglas SonarQube

Mapeo de mensajes comunes a severidades:

| Mensaje de `get_errors` | Severidad |
|-------------------------|-----------|
| `Remove the unused private field` | MAJOR |
| `Remove this unread private field` | MAJOR |
| `Remove the unused private constant` | MAJOR |
| `Possible null reference assignment` | CRITICAL |
| `SQL injection` / `command injection` | BLOCKER |
| `Hardcoded credentials` | BLOCKER |

### Paso C#-3 — Verificar vulnerabilidades y hotspots de seguridad

Invocar `sonarqube_list_potential_security_issues` sobre el mismo archivo:

```
sonarqube_list_potential_security_issues(filePath: "<ruta-absoluta-del-archivo>.cs")
```

Cubre:
- OWASP Top 10 (injection, broken auth, SSRF, etc.)
- Clasificaciones CWE
- Security Hotspots que requieren revisión humana

### Paso C#-4 — Repetir para cada archivo `.cs` del scope

Ejecutar los pasos C#-1, C#-2 y C#-3 por cada archivo `.cs` modificado en Fase 2. No saltarse ninguno aunque el archivo parezca trivial.

---

### Paso 3 — Análisis

4. Por cada archivo, aplicar el procedimiento según su extensión:

   - **`.cs`** → Ejecutar **Procedimiento C#** (pasos C#-1, C#-2 y C#-3) definido anteriormente en este SKILL.
   - **`.ts` / `.js` / `.html`** → Ejecutar `mcp_sonarqube_analyze_code_snippet` con el `language` correcto. Complementar con `mcp_sonarqube_get_duplications` y `mcp_sonarqube_get_file_coverage_details`.

| Scope | Archivos a incluir |
|-------|-----------------|
| `backend` | Todos los `.cs` del backend modificados en Fase 2 |
| `frontend` | Todos los `.ts`, `.html`, `.scss` del frontend modificados en Fase 2 |
| `ambos` | Unión de ambos conjuntos anteriores |

> Si el scope no se especifica, usar `ambos` por defecto.

### Paso 5 — Parseo de resultados

5. Clasificar todos los issues retornados por severidad:

```
BLOCKER  → Bug o vulnerabilidad que impide el funcionamiento. BLOQUEA el gate.
CRITICAL → Bug grave o security hotspot confirmado. BLOQUEA el gate.
MAJOR    → Code smell significativo o bug menor. No bloquea, pero se reporta.
MINOR    → Sugerencia de mejora. Informativo.
INFO     → Nota de estilo. Informativo.
```

6. Opcionalmente verificar `mcp_sonarqube_get_project_quality_gate_status` para contrastar con el quality gate del proyecto.

### Paso 6 — Generar reporte

7. Crear `docs/output/qa/<feature>-static-analysis.md` con la siguiente estructura:

```markdown
# Análisis Estático — [nombre-feature]

## Resumen ejecutivo
- **Fecha**: [fecha]
- **Scope**: [backend | frontend | ambos]
- **Project Key**: [key resuelto]
- **Archivos analizados**: [N]
- **Gate**: ✅ PASS | ❌ FAIL

## Conteo de issues
| Severidad | Total |
|-----------|-------|
| BLOCKER   | X     |
| CRITICAL  | X     |
| MAJOR     | X     |
| MINOR     | X     |
| INFO      | X     |

## Issues BLOCKER y CRITICAL — Acción requerida

| # | Archivo | Línea | Regla | Mensaje | Severidad |
|---|---------|-------|-------|---------|-----------|
| 1 | path/to/file | L42 | ruleKey | descripción | BLOCKER |

## Issues MAJOR — Revisión recomendada

| # | Archivo | Línea | Regla | Mensaje |
|---|---------|-------|-------|---------|
| 1 | path/to/file | L10 | ruleKey | descripción |

## Decisión del Gate

> **[PASS / FAIL]** — [razón resumida en una línea]
```

### Paso 5 — Aplicar gate

8. Evaluar el resultado:

```
SI (BLOCKER > 0 OR CRITICAL > 0):
  → Gate: FAIL
  → Notificar al Orchestrator:
    "⛔ static-analysis FAIL en [scope] de [feature].
     Issues bloqueantes: [N BLOCKER, N CRITICAL].
     Ver docs/output/qa/<feature>-static-analysis.md.
     NO proceder a Fase 3 hasta resolver los issues."

SI (BLOCKER = 0 AND CRITICAL = 0):
  → Gate: PASS
  → Notificar al Orchestrator:
    "✅ static-analysis PASS en [scope] de [feature].
     [N MAJOR issues para revisión no bloqueante].
     Puede proceder a Fase 3."
```

## Output

| Archivo | Siempre |
|---------|---------|
| `docs/output/qa/<feature>-static-analysis.md` | ✅ Siempre, independiente del gate |

## Restricciones

- Nunca asumir el project key — siempre usar `mcp_sonarqube_search_my_sonarqube_projects`
- No intentar verificar issues corregidos después de una corrección — el servidor SonarQube no refleja los cambios de inmediato
- No modificar ningún archivo de código fuente — solo leer y reportar
- Solo crear archivos en `docs/output/qa/`
- El gate FAIL es definitivo: el Orchestrator decide si continuar, no esta skill
