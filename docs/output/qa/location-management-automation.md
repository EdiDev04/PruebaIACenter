# Propuesta de Automatización — SPEC-006: Gestión de Ubicaciones de Riesgo

> **Fuente:** SPEC-006 v1.0 + location-management.design.md (APPROVED) + location-management-risks.md
> **Generado:** 2026-03-29
> **Metodología:** Criterios RECI (Repetitivo · Estable · Crítico · Invertible)
> **Criterio de evaluación del reto:** trazabilidad de datos de entrada al motor de cálculo + consistencia de APIs y manejo de errores

---

## Resumen Ejecutivo

| Métrica | Valor |
|---|---|
| Flujos candidatos | 6 |
| P1 — Automatizar ya | 3 |
| P2 — Sprint siguiente | 2 |
| P3 — Backlog | 1 |
| Framework principal | Playwright (E2E) + Newman/k6 (API/Performance) |
| Ahorro semanal estimado (post-automatización) | ~7.5 horas / ciclo de release |
| Costo de implementación estimado | 1.5 sprints |

---

## Matriz de priorización ROI

| # | Flujo | Framework | Repetitivo | Estable | Alto Impacto | Costo Manual | ROI | Prioridad |
|---|---|---|---|---|---|---|---|---|
| F-01 | `PUT /locations` — flujo feliz + validaciones de error | k6 + Newman | ✅ Alta | ✅ Sí | ✅ Alta | ✅ 2h/sem | 24x | **P1** |
| F-02 | E2E wizard paso 2 completo (step 1 → step 2 → guardar → badge) | Playwright | ✅ Alta | ✅ Sí | ✅ Alta | ✅ 3h/sem | 18x | **P1** |
| F-03 | Manejo de conflicto de versión 409 (toast + formulario abierto) | Playwright | ✅ Alta | ✅ Sí | ✅ Alta | ✅ 1h/sem | 12x | **P1** |
| F-04 | `PATCH /locations/{index}` — edición de ubicación individual | Newman | ✅ Alta | ✅ Sí | ⚠️ Media | ✅ 1h/sem | 20x | **P2** |
| F-05 | Validaciones de formulario Step 1 (locationName, zipCode, año) | Playwright | ✅ Alta | ✅ Sí | ⚠️ Media | ✅ 1.5h/sem | 18x | **P2** |
| F-06 | Visual regression — badge estado (ámbar vs verde, nunca rojo) | Playwright Screenshots | ❌ Baja | ✅ Sí | ⚠️ Media | ❌ 0.5h/sem | 15x | **P3** |

---

## Detalle de flujos

### F-01 — `PUT /locations` (Newman + k6) `P1`

**Justificación:** Es la operación más crítica de SPEC-006. Un error en PUT puede generar data corruption del array completo (R-004). Se ejecuta en cada save de ubicación nueva y en cada eliminación implícita. Alto costo de detección manual en regresión.

**Criterio de reto cubierto:** consistencia de APIs — atomicidad del reemplazo del array.

| Sub-flujo | Tipo | Resultado esperado |
|---|---|---|
| PUT con 1 ubicación calculable | Happy path | HTTP 200, `validationStatus: "calculable"`, `version` increments |
| PUT con 3 ubicaciones (reemplaza 2 previas) | Atomicidad | HTTP 200, array tiene exactamente 3 elementos |
| PUT con CP `99999` (no encontrado en core-ohs) | Error path | HTTP 200, `validationStatus: "incomplete"`, `blockingAlerts` contiene CP |
| PUT sin header `Authorization` | Seguridad | HTTP 401 |
| PUT con `version` desactualizada | Concurrencia | HTTP 409, mensaje en español |
| PUT con `locationName` vacío | Validación | HTTP 400, `"El nombre de la ubicación es obligatorio"` |
| PUT con `constructionYear: 1799` | Edge case | HTTP 400, `"El año de construcción es inválido"` |
| PUT con `guarantees[].insuredAmount: -1` | Edge case | HTTP 400, `"La suma asegurada debe ser mayor o igual a 0"` |

**Costo manual estimado:** 2 horas / semana (8 sub-flujos × 15 min c/u)
**Tiempo automatizado:** ~5 min / ejecución
**ROI estimado:** 24x

---

### F-02 — E2E Wizard Paso 2 Completo (Playwright) `P1`

**Justificación:** Flujo crítico de usuario final. Cubre la integración FE↔BE completa: resolución de CP vía proxy, selección de catálogos, evaluación de `validationStatus` visualizado en badge. Error en este flujo impacta directamente la entrada de datos al motor SPEC-009.

**Criterio de reto cubierto:** trazabilidad del cálculo — los datos de ubicación que llegan al motor son correctos; consistencia de APIs — badge refleja status real del backend.

| Sub-flujo | Resultado esperado |
|---|---|
| Ingresar CP `06600` → auto-resolución | Estado, municipio, colonia, catZone aparecen como "auto" en < 500ms |
| Completar step 1 → avanzar a step 2 | Formulario navega; datos de step 1 conservados |
| Seleccionar giro + garantía → guardar | Badge verde "Calculable" en grilla |
| Ingresar CP `99999` → guardar parcial | Badge ámbar "Datos pendientes" (sin badge rojo) |
| 0 ubicaciones calculables → Continuar | Botón "Continuar →" deshabilitado con tooltip correcto |
| ≥ 1 ubicación calculable → Continuar | Botón habilitado; navega a paso 3 |

**Costo manual estimado:** 3 horas / semana (6 variantes × 30 min c/u)
**Tiempo automatizado:** ~10 min / ejecución
**ROI estimado:** 18x

---

### F-03 — Conflicto de Versión 409 (Playwright) `P1`

**Justificación:** El manejo del conflicto de versión es un riesgo de nivel Alto (R-002). Un 409 silencioso provoca pérdida de datos. El criterio de evaluación del reto incluye explícitamente "manejo de errores" en flujos concurrentes.

**Criterio de reto cubierto:** consistencia de APIs — manejo de errores y resiliencia.

| Sub-flujo | Resultado esperado |
|---|---|
| PUT con version desactualizada | Toast "El folio fue modificado. Recargue para continuar." |
| Toast visible | Formulario permanece abierto (no se cierra) |
| Datos del formulario después del 409 | Los campos conservan los valores que el agente tenía editados |
| Navegación después del 409 | No se redirige a otra página automáticamente |

**Costo manual estimado:** 1 hora / semana (escenario complejo de replicar manualmente)
**Tiempo automatizado:** ~5 min / ejecución
**ROI estimado:** 12x

---

### F-04 — `PATCH /locations/{index}` Individual (Newman) `P2`

**Justificación:** PATCH es la operación de edición granular. Un error de índice puede corromper la ubicación equivocada (R-007). Menos frecuente que PUT pero con impacto alto.

| Sub-flujo | Resultado esperado |
|---|---|
| PATCH index=2 en folio con 3 ubicaciones | Solo `locations[1]` modificado; `locations[0]` y `locations[2]` intactos |
| PATCH index=99 (inexistente) | HTTP 404, `type: "folioNotFound"` |
| PATCH con version desactualizada | HTTP 409 |
| PATCH solo `locationName` (partial update) | Solo ese campo cambia; `validationStatus` recalculado |

**Costo manual estimado:** 1 hora / semana
**Tiempo automatizado:** ~3 min / ejecución
**ROI estimado:** 20x

---

### F-05 — Validaciones de Formulario Step 1 (Playwright) `P2`

**Justificación:** Las validaciones de Step 1 son la primera línea de defensa sobre la integridad de los datos. Un error aquí permite que datos inválidos lleguen al backend.

| Campo | Caso de prueba | Resultado esperado |
|---|---|---|
| `locationName` vacío | Clic en "Siguiente" | Error inline "El nombre de la ubicación es obligatorio" |
| `locationName` con 201 chars | Guardar | Error inline "Máximo 200 caracteres" o truncado |
| `zipCode` con letras | `ABCDE` | Error "El código postal debe ser de 5 dígitos" |
| `zipCode` con 4 dígitos | `0660` | Error "El código postal debe ser de 5 dígitos" |
| `constructionYear` inválido | `1799` | Error "El año de construcción es inválido" |
| `level` negativo | `-1` | Error "El nivel debe ser un número positivo" |

**Costo manual estimado:** 1.5 horas / semana
**Tiempo automatizado:** ~5 min / ejecución
**ROI estimado:** 18x

---

### F-06 — Visual Regression de Badges (Playwright Screenshots) `P3`

**Justificación:** Los badges deben ser ámbar para incompletas (nunca rojo) y verdes para calculables. Un error visual afecta la percepción del agente sobre el estado del folio. Frecuencia baja (solo cambia si se toca la UI de badges).

| Sub-flujo | Resultado esperado |
|---|---|
| Badge `calculable` | Color #16a34a (verde), texto "Calculable" |
| Badge `incomplete` | Color #d97706 (ámbar), texto "Datos pendientes" |
| Badge `incomplete` | NUNCA color #dc2626 (rojo) |
| Badge en grilla (viewport 1440px) | Visible sin truncado |
| Badge en grilla (viewport 768px) | Visible con scroll horizontal permitido |

**Costo manual estimado:** 0.5 horas / semana
**Tiempo automatizado:** ~2 min / ejecución
**ROI estimado:** 15x

---

## Orden de implementación recomendado

```
Sprint 1 (P1 — menor costo, mayor impacto inmediato)
├── Semana 1: F-01 — API tests Newman (menor costo de setup, mayor cobertura rápida)
│             Prerequisito: ambiente de pruebas estable con cotizador-core-mock
└── Semana 2: F-03 — 409 conflict Playwright
              F-02 — E2E wizard paso 2 completo (más complejo, última semana)

Sprint 2 (P2 — después de P1 en verde)
├── Semana 1: F-04 — PATCH individual (Newman, reutiliza setup de F-01)
└── Semana 2: F-05 — Validaciones Step 1 (Playwright, reutiliza fixtures de F-02)

Backlog (P3 — solo si el equipo tiene capacidad)
└── F-06 — Visual regression badges (baja frecuencia de cambio)
```

**Razón del orden:** Los API tests (F-01, F-04) tienen menor costo de setup porque no requieren browser. Dan cobertura rápida de las reglas de negocio críticas. Los E2E (F-02, F-03, F-05) requieren más tiempo de implementación pero cubren flujos de integración FE↔BE que los API tests no pueden verificar. El visual regression (F-06) es el último porque depende de que la UI esté estabilizada.

---

## Definition of Ready (DoR) para iniciar automatización

- [ ] Caso ejecutado manualmente con éxito al menos 1 vez sin bugs críticos abiertos
- [ ] Ambiente `cotizador-core-mock` estable y accesible desde el pipeline CI
- [ ] Datos de prueba (folios, CP, giros) disponibles en fixtures
- [ ] Endpoints documentados y con contrato estable (sin breaking changes pendientes)
- [ ] Aprobación del equipo (tech lead + QA lead)

## Definition of Done (DoD) de cada automatización

- [ ] Código revisado mediante pull request
- [ ] Tests pasan en CI sin flakiness (< 1% de fallos espúrios en 10 ejecuciones)
- [ ] Integrado al pipeline CI (GitHub Actions) como gate de merge
- [ ] Tiempo de ejecución documentado y dentro del budget del pipeline
- [ ] Reporte de resultados enviado al canal de calidad del equipo
