# ADR-008: Diseño del endpoint de estado del folio (SPEC-008)

**Estado:** Accepted  
**Fecha:** 2026-03-30  
**SPEC de origen:** SPEC-008 `quote-state-progress`  
**Decisiones documentadas:** 2

---

## Decisión 1: `calculationResult` incluido en el mismo endpoint de estado

### Contexto

El frontend necesita, al entrar al Step 4 del wizard (términos y condiciones), mostrar el resultado financiero si el folio ya fue calculado. Las opciones eran:

1. Endpoint separado `GET /v1/quotes/{folio}/calculation-result` que el FE consulta solo en Step 4.
2. Incluir `calculationResult` (nullable) dentro de `GET /v1/quotes/{folio}/state`, que ya se consulta en cada página del wizard.

### Decisión

Se eligió la **opción 2**: `calculationResult` forma parte del `QuoteStateDto` con tipo nullable.

- Si `quoteStatus != "calculated"` → el campo es `null`, sin overhead de datos.
- Si `quoteStatus == "calculated"` → el campo está poblado; el FE no hace una segunda llamada.

### Consecuencias

- **Positivo:** elimina un round-trip HTTP en Step 4. Un solo endpoint es la fuente de verdad de todo el estado del folio.
- **Positivo:** la invalidación de caché es simple — un solo query key `['quote-state', folio]` cubre todo.
- **Negativo / limitación futura:** si `calculationResult` crece en tamaño (e.g., con SPEC-009 agregando factores detallados por cobertura), el response de estado se hace más pesado en folios calculados. En ese escenario, considerar separar el endpoint y paginar `premiumsByLocation`.
- **Supuesto aprobado:** SUP-008-02 en la spec.

---

## Decisión 2: Progreso derivado de datos persistidos, no del step del wizard

### Contexto

El widget `ProgressBar` necesita saber qué secciones del folio están completas. Las opciones eran:

1. Derivar el progreso del campo `metadata.lastWizardStep` (ya existente en el documento).
2. Derivar el progreso de la presencia de datos reales en cada sección del `PropertyQuote`.

### Decisión

Se eligió la **opción 2**: el progreso se calcula en el Use Case evaluando datos concretos:

| Sección | Condición para `true` |
|---|---|
| `generalInfo` | `!string.IsNullOrWhiteSpace(InsuredData.Name)` |
| `layoutConfiguration` | Siempre `true` (los defaults existen desde la creación del folio, SPEC-005) |
| `locations` | `Locations.Count > 0` |
| `coverageOptions` | `CoverageOptions.EnabledGuarantees.Count > 0` |

### Consecuencias

- **Positivo:** el estado es consistente con actualizaciones vía API directa o agentes automatizados — no depende de que el usuario haya navegado por el wizard.
- **Positivo:** si un usuario vacía una sección, el progreso retrocede correctamente sin lógica adicional.
- **Limitación conocida:** si en el futuro se necesita trackear "el usuario visitó este paso sin guardar datos", se requiere agregar un flag independiente (e.g., `visitedSteps: string[]`) al documento. El campo `lastWizardStep` sigue disponible como auxiliar de UX pero no como fuente de verdad del progreso.
- **Supuesto aprobado:** SUP-008-01 en la spec.
