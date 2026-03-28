---
name: generate-design-spec
description: Genera design specs y diseños UI usando Google Stitch MCP para una feature del cotizador. Produce mockups, reglas visuales y artefactos que el frontend-developer consume como input obligatorio.
argument-hint: "<nombre-feature> [--refine \"instrucción de refinamiento\"]"
---

# Generate Design Spec

## Instrucciones

1. Recibe el nombre de la feature como argumento: `/generate-design-spec <nombre-feature>`
2. Ejecuta el flujo completo del agente `ux-designer`:
   - Análisis Data-Driven del modelo de datos
   - Generación del design spec (`.github/design-specs/<feature>.design.md`)
   - Generación de pantallas en Google Stitch via MCP
   - Extracción de HTML/CSS de referencia
   - Validación contra principios de diseño conductual

## Carga de contexto obligatoria

Lee estos archivos ANTES de diseñar cualquier pantalla:

```
ARCHITECTURE.md                              → Stack FE, capas FSD, separación de estado
bussines-context.md                          → Dominio de seguros, entidades, flujos
.github/instructions/frontend.instructions.md → Restricciones FE, convenciones, componentes
.github/docs/lineamientos/dev-guidelines.md  → Clean Code, accesibilidad, patrones
.github/design-specs/.stitch-config.json     → Config de proyecto Stitch (si existe)
.github/specs/<feature>.spec.md              → Spec técnica (si existe)
```

## Proceso

### Fase 1 — Análisis Data-Driven

1. Identifica la **entidad de dominio** principal de la pantalla
2. Extrae campos del modelo con tipos, rangos y restricciones
3. Clasifica campos: input del usuario vs derivados (auto-calculados)
4. Ejecuta **cluster analysis**: agrupa campos por cohesión funcional
5. Ejecuta **Data → UI mapping**: asigna componente UI óptimo a cada campo

Reglas de mapping:

| Tipo de dato | Opciones | Componente |
|---|---|---|
| Texto libre | N/A | `TextInput` |
| Catálogo pequeño (<7) | Mutuamente excluyentes | `RadioGroup` |
| Catálogo pequeño (<7) | Multi-selección | `CheckboxGroup` |
| Catálogo grande (>7) | Selección única | `ComboBox` con búsqueda |
| Número con rango acotado | Min/max conocidos | `NumberInput` con stepper |
| Booleano | N/A | `Toggle` o `Checkbox` |
| Derivado (auto-calculado) | Solo lectura | `ReadOnlyField` con chip "auto" |

### Fase 2 — Design System en Stitch

1. Verifica si existe proyecto Stitch → `mcp_stitch_list_projects`
2. Si no existe → crea proyecto con `mcp_stitch_create_project`
3. Crea o verifica design system con `mcp_stitch_create_design_system`
4. Persiste config en `.github/design-specs/.stitch-config.json`

### Fase 3 — Design Spec

Genera `.github/design-specs/<feature>.design.md` con **6 secciones obligatorias**:

1. **Data → UI mapping** — Tabla: campo → componente → tipo → razón
2. **Behavioral annotations** — Progressive disclosure, smart defaults, agrupación cognitiva
3. **Screen flow + hierarchy** — Wireframe ASCII, jerarquía F-pattern, tabla de interacciones
4. **Component inventory** — Tabla: componente → capa FSD → props → estado. Zod schemas por step
5. **Validation + feedback UX** — Matriz: campo → trigger → feedback positivo → feedback error
6. **Stitch prompts** — Prompts optimizados para `mcp_stitch_generate_screen_from_text`

### Fase 4 — Generación en Stitch (via MCP)

1. Genera pantallas con `mcp_stitch_generate_screen_from_text` (modelo `GEMINI_3_FLASH`)
2. Aplica design system con `mcp_stitch_apply_design_system`
3. Evalúa y refina con `mcp_stitch_edit_screens` (máximo 3 iteraciones por pantalla)
4. Genera variantes opcionales con `mcp_stitch_generate_variants`
5. Versión final con modelo `GEMINI_3_1_PRO`
6. Extrae HTML/CSS con `mcp_stitch_get_screen` → guarda en `screens/<feature>/`

### Fase 5 — Entrega

Actualiza `.stitch-config.json` con todos los screenIds finales (status: "final").

## Output esperado

```
.github/design-specs/
├── .stitch-config.json                 → Project, screen IDs y design system ID
├── <feature>.design.md                 → Design spec completo (6 secciones)
└── screens/
    └── <feature>/
        └── *.html                      → HTML/CSS de referencia generado por Stitch
```

## Reglas de diseño conductual (invariantes)

- **Max 6 campos visibles** simultáneamente — usar progressive disclosure
- **Incompleto ≠ Error** — usar ámbar (warning), NO rojo (danger)
- **Smart defaults** pre-seleccionados con badge "recomendado"
- **Agrupación cognitiva** en categorías (Miller's Law: max 4 grupos)
- **Validación inline** al perder foco — NO solo al submit
- **Campos derivados** muestran chip "auto" + tooltip con origen del dato
- **Transparencia total** en desglose de prima
- **WCAG AA** obligatorio — contraste mínimo 4.5:1

## Estrategia de modelos Stitch

| Momento | Modelo | Razón |
|---|---|---|
| Exploración inicial | `GEMINI_3_FLASH` | Rápido, permite iterar |
| Variantes | `GEMINI_3_FLASH` | Explorar opciones |
| Versión final | `GEMINI_3_1_PRO` | Mayor calidad visual |

## Uso

```
# Generar design spec para una pantalla
/generate-design-spec ubicaciones

# Generar design spec para todo el cotizador
/generate-design-spec cotizador-completo

# Regenerar con refinamientos
/generate-design-spec ubicaciones --refine "Agregar estado de carga en el formulario"
```

## Restricciones

- SÓLO crear archivos en `.github/design-specs/`
- NUNCA modificar código fuente React en `cotizador-webapp/src/`
- NUNCA modificar specs técnicas en `.github/specs/`
- NUNCA generar componentes React — eso lo hace `frontend-developer`
- NUNCA usar rojo para estados "incompletos"
- NUNCA usar `GEMINI_3_1_PRO` en iteraciones exploratorias
- NUNCA diseñar sin haber hecho el análisis Data-Driven (Fase 1)
