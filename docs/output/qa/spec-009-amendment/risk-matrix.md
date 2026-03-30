# Matriz de Riesgos — SPEC-009 Amendment v1.1
## RN-009-02b: Cruce de `enabledGuarantees` con garantías de ubicación

**Referencia:** SPEC-009 v1.1 · RN-009-02b · Metodología: ASD (Alto / Medio / Bajo)  
**Generado:** 2026-03-30 · **Agente:** QA Agent

---

## Resumen

| Total | Alto (A) | Medio (S) | Bajo (D) |
|-------|----------|-----------|----------|
| 7 | 4 | 2 | 1 |

Regla ASD:
- **A (Alto)** → Testing OBLIGATORIO — bloquea el release
- **S (Medio)** → Testing RECOMENDADO — documentar si se omite
- **D (Bajo)** → Testing OPCIONAL — priorizar en el backlog

---

## Detalle de Riesgos

| ID | Regla origen | Descripción del riesgo | Factores | Nivel | Testing |
|---|---|---|---|---|---|
| AMEND-009-R-01 | RN-009-02b | Folios existentes con `CoverageOptions` null o `enabledGuarantees: []` se ven afectados por el filtro — regresión silenciosa | Código nuevo, integraciones, lógica compleja | **A** | Obligatorio |
| AMEND-009-R-02 | RN-009-02b | Inconsistencia entre `validationStatus: "calculable"` en BD y `validationStatus: "incomplete"` en response del cálculo — confusión de estado | Lógica de negocio compleja, datos persistidos | **A** | Obligatorio |
| AMEND-009-R-03 | RN-009-02b + RN-009-04 | Todas las ubicaciones degradadas a incomplete por el filtro → HTTP 422, pero el folio NO debe modificarse — riesgo de escritura parcial | Operación destructiva, atomicidad | **A** | Obligatorio |
| AMEND-009-R-04 | RN-009-02b | `enabledGuarantees: null` vs `enabledGuarantees: []` — ambos deben deshabilitar el filtro, pero son condiciones distintas en C# | Código nuevo, casos borde | **A** | Obligatorio |
| AMEND-009-R-05 | RN-009-02b | Prima comercial total calculada sobre prima neta que excluye ubicaciones degradadas — verificar propagación correcta hacia `commercialPremiumBeforeTax` y `commercialPremium` | Lógica financiera, cálculo en cascada | **S** | Recomendado |
| AMEND-009-R-06 | RN-009-02b | Una ubicación con `validationStatus: "incomplete"` en BD y además garantías fuera de enabledGuarantees: verificar que no causa doble penalización ni comportamiento inesperado | Código nuevo, interacción entre reglas | **S** | Recomendado |
| AMEND-009-R-07 | RN-009-02b | ZipCode lookup (`techLevelByZip`) solo se ejecuta para ubicaciones calculables post-filtro — si el filtro excluye una ubicación antes del lookup, no debe fallar ni generar log de error | Feature interna, impacto limitado | **D** | Opcional |

---

## Plan de Mitigación — Riesgos ALTO

### AMEND-009-R-01: Regresión en folios con CoverageOptions sin configurar

**Contexto:** Si un folio fue creado antes de SPEC-007 (o el usuario no completó el Step 3 del wizard), `CoverageOptions` es `null`. El amendment especifica que `null` → sin filtro. Riesgo: un bug en la condición `enabledGuaranteesList != null && enabledGuaranteesList.Count > 0` puede activar el filtro con una lista vacía construida erróneamente.

- **Mitigación técnica:** La condición en `CalculateQuoteUseCase.cs` (línea ~56) usa `enabledGuaranteesList != null && enabledGuaranteesList.Count > 0` antes de construir el `HashSet`. Si es `null` o `Count==0`, `enabledGuaranteeKeys` queda en `null` y el filtro no aplica.
- **Tests obligatorios:**
  - Unit test: `CoverageOptions == null` → `enabledGuaranteeKeys == null` → todas las ubicaciones calculables pasan
  - Unit test: `EnabledGuarantees == []` → `enabledGuaranteeKeys == null` → mismo resultado
  - Integration test (Postman): folio sin CoverageOptions → POST calculate → HTTP 200, todas ubicaciones calculables
- **Bloqueante para release:** ✅ Sí

---

### AMEND-009-R-02: Inconsistencia visible entre status en BD y status en response

**Contexto:** Una ubicación con `validationStatus: "calculable"` en MongoDB aparecerá como `validationStatus: "incomplete"` en el response del cálculo cuando RN-009-02b la degrada. Esto puede confundir a clientes que consulten el folio directamente (GET) vs el resultado del cálculo (POST calculate).

- **Mitigación técnica:** El `LocationPremium` del response siempre refleja el status efectivo de cálculo, no el de BD. El `GET /v1/quotes/{folio}` devuelve el status de BD (sin cambiar). Esta dualidad es intencional según RN-009-02b pero debe estar documentada.
- **Tests obligatorios:**
  - Test explícito: después de POST calculate con degradación, GET folio → `locations[x].validationStatus` sigue siendo "calculable" en BD
  - Verificar que `UpdateFinancialResultAsync` NO actualiza `validationStatus` de las locations
- **Bloqueante para release:** ✅ Sí

---

### AMEND-009-R-03: HTTP 422 sin escritura parcial cuando todas las ubicaciones son degradadas

**Contexto:** Si RN-009-02b degrada todas las ubicaciones a incomplete, se lanza `InvalidQuoteStateException` → HTTP 422. El folio debe quedar intacto (version no incrementa, netPremium no se persiste).

- **Mitigación técnica:** La excepción se lanza en el paso 7 del use case, antes de `UpdateFinancialResultAsync`. La atomicidad está garantizada por el flujo, pero un bug de orden (llamar persist antes de la verificación) rompería esto.
- **Tests obligatorios:**
  - Unit test: 0 ubicaciones calculables post-filtro → `InvalidQuoteStateException` thrown, `UpdateFinancialResultAsync` nunca invocado (verificar con Mock)
  - Integration test: POST calculate con todas garantías deshabilitadas → HTTP 422, GET folio → version sin cambio
- **Bloqueante para release:** ✅ Sí

---

### AMEND-009-R-04: `null` vs `[]` como casos borde del "sin filtro"

**Contexto:** En C#, `null` y `new List<string>()` son condiciones distintas. El código evalúa ambas con `enabledGuaranteesList != null && enabledGuaranteesList.Count > 0`. Si en algún punto del pipeline (deserialización JSON, MongoDB driver) un `[]` llega como `null` o viceversa, el comportamiento del filtro cambia inesperadamente.

- **Mitigación técnica:** Verificar que el modelo `CoverageOptions.EnabledGuarantees` usa `List<string>?` (nullable) y que el MongoDB driver serializa `[]` como array vacío, no como `null`.
- **Tests obligatorios:**
  - Unit test: `EnabledGuarantees = null` → sin filtro
  - Unit test: `EnabledGuarantees = new List<string>()` → sin filtro
  - Unit test: `EnabledGuarantees = new List<string> { "building_fire" }` → filtro activo
- **Bloqueante para release:** ✅ Sí
