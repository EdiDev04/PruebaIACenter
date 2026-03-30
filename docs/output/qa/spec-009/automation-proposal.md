# Propuesta de Automatización — SPEC-009: Motor de Cálculo de Primas

> **Feature:** `premium-calculation-engine`  
> **Spec:** SPEC-009 | **Estado:** IN_PROGRESS  
> **Generado:** 2026-03-30 | **Agente:** QA Agent  
> **Criterio de priorización:** ROI = suma de ✅ en los 4 criterios (Repetitivo + Estable + Alto Impacto + Costo Manual)

---

## Resumen Ejecutivo

| Categoría | Cantidad |
|---|---|
| Flujos candidatos evaluados | 9 |
| **P1 — Automatizar ya (ROI 4/4)** | 4 |
| **P2 — Automatizar este sprint (ROI 3/4)** | 3 |
| **P3 — Posponer (ROI ≤ 2/4)** | 2 |
| Framework recomendado (E2E) | **Playwright (TypeScript)** |
| Framework recomendado (API) | **xUnit + WebApplicationFactory (.NET)** |
| Esfuerzo estimado P1+P2 | 2 sprints |

**Justificación para el reto:** Los flujos P1 cubren directamente los criterios de evaluación del reto:
- *"Trazabilidad del cálculo"* → FLUJO-001, FLUJO-002
- *"Consistencia de APIs y manejo de errores"* → FLUJO-003, FLUJO-004
- *"Integridad de datos en operaciones atómicas"* → FLUJO-003

---

## Selección de Framework

### Backend — Tests de API e Integración

**Framework:** xUnit + `WebApplicationFactory<Program>` + Bogus (datos sintéticos) + MongoDB test container

| Criterio | Evaluación |
|---|---|
| Stack del proyecto | ✅ ASP.NET Core 8 — xUnit ya presente en `Cotizador.Tests/` |
| Curva de aprendizaje | ✅ El equipo ya tiene tests xUnit en el proyecto |
| Integración CI/CD | ✅ Compatible con `dotnet test` en pipeline existente |
| Costo de mantenimiento | ✅ Bajo — mismo lenguaje que el backend |

### Frontend / E2E — Flujos de usuario completos

**Framework:** Playwright (TypeScript)

| Criterio | Evaluación |
|---|---|
| Stack del proyecto | ✅ TypeScript ya usado en `cotizador-webapp/` y `cotizador-automatization/` |
| Multi-browser | ✅ Chrome + Firefox sin configuración adicional |
| CI-first | ✅ `@playwright/test` ejecuta headless por defecto |
| Directorio target | `cotizador-automatization/e2e/specs/calculate-quote/` |

**No Cypress** — Playwright ya está adoptado en `cotizador-automatization/` según el mapa del workspace.

---

## Matriz de Priorización ROI

| ID | Flujo | Repetitivo | Estable | Alto Impacto | Costo Manual | ROI | Prioridad |
|---|---|---|---|---|---|---|---|
| FLUJO-001 | Cálculo exitoso — 2 calculables + 1 incompleta | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** |
| FLUJO-002 | Verificación de fórmulas financieras (netPremium → beforeTax → withTax) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** |
| FLUJO-003 | Versionado optimista — 409 en conflicto | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** |
| FLUJO-004 | core-ohs caído — 503 sin persistencia | ✅ Media | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** |
| FLUJO-005 | 0 ubicaciones calculables — 422 | ✅ Media | ✅ Sí | ⚠️ Media | ✅ Alto | 3/4 | **P2** |
| FLUJO-006 | Persistencia atómica — insuredData/locations no modificados | ✅ Alta | ✅ Sí | ✅ Alta | ❌ Bajo* | 3/4 | **P2** |
| FLUJO-007 | Desglose por cobertura (coveragePremiums[]) | ✅ Media | ✅ Sí | ⚠️ Media | ✅ Alto | 3/4 | **P2** |
| FLUJO-008 | Validación de entrada — folio inválido, version ausente | ✅ Alta | ✅ Sí | ❌ Baja | ❌ Bajo | 2/4 | **P3** |
| FLUJO-009 | Acceso sin autenticación — 401 | ✅ Alta | ✅ Sí | ❌ Baja | ❌ Bajo | 2/4 | **P3** |

*FLUJO-006 tiene costo manual bajo porque existe un test unitario del `$set`, pero el riesgo de producción es alto → P2 por impacto.

---

## Hoja de Ruta por Sprint

### Sprint 1 — P1 (Automatizar ya)

#### FLUJO-001: Cálculo exitoso completo
- **Tipo:** API test (xUnit + WebApplicationFactory) + E2E (Playwright)
- **Escenario:** POST .../calculate con folio `DAN-2026-00001`, version=5 → HTTP 200, premiumsByLocation con 3 items, 2 calculables y 1 incomplete, netPremium > 0
- **Datos:** Fixtures del fixture `calculationParameters.json` y `fireTariffs.json` del core-mock
- **Estimación:** 3 días (API: 1.5d + E2E: 1.5d)
- **Referencia reto:** Criterio *"trazabilidad del cálculo"*

#### FLUJO-002: Verificación aritmética de fórmulas financieras
- **Tipo:** Unitario (xUnit — PremiumCalculator)
- **Escenario:** Input con valores fijos → verificar cálculos exactos al centavo:
  - `netPremium = 125430.50`
  - `commercialPremiumBeforeTax = 125430.50 × 1.20 = 150516.60`
  - `commercialPremium = 150516.60 × 1.16 = 174599.26`
- **Estimación:** 1 día
- **Referencia reto:** Criterio *"trazabilidad del cálculo"*

#### FLUJO-003: Versionado optimista — 409
- **Tipo:** API test (xUnit)
- **Escenario A:** POST con version=3 cuando versión real=5 → HTTP 409, body `versionConflict`, folio sin cambios
- **Escenario B (concurrencia):** Dos requests simultáneos con version=5 → uno recibe 200, otro recibe 409
- **Estimación:** 2 días (incluye setup de concurrencia)
- **Referencia reto:** Criterio *"consistencia de APIs y manejo de errores"*

#### FLUJO-004: core-ohs caído — 503 sin persistencia
- **Tipo:** API test (xUnit con mock de ICoreOhsClient)
- **Escenario:** core-ohs simula timeout → HTTP 503, cuerpo `coreOhsUnavailable`, folio no modificado (versión sin cambio)
- **Estimación:** 1.5 días
- **Referencia reto:** Criterio *"consistencia de APIs y manejo de errores"* + resiliencia

---

### Sprint 2 — P2 (Automatizar este sprint)

#### FLUJO-005: 0 ubicaciones calculables — 422
- **Tipo:** API test (xUnit)
- **Escenario:** Folio con todas ubicaciones `incomplete` → HTTP 422, body `invalidQuoteState`, folio NO persiste resultado
- **Estimación:** 1 día

#### FLUJO-006: Persistencia atómica sin sobrescritura
- **Tipo:** Integración (xUnit + MongoDB test container)
- **Escenario:** Snapshot pre-cálculo de `insuredData`/`locations`/`coverageOptions` → ejecutar cálculo → comparar snapshot post-cálculo (deben ser idénticos)
- **Estimación:** 2 días (incluye setup de MongoDB TestContainer)

#### FLUJO-007: Desglose por cobertura en response
- **Tipo:** API test (xUnit) + E2E (Playwright — verificar UI de desglose en SPEC-010)
- **Escenario:** Response contiene `coveragePremiums[]` con `guaranteeKey`, `insuredAmount`, `rate`, `premium`; `premium == insuredAmount × rate` al centavo
- **Estimación:** 1.5 días

---

### P3 — Posponer (candidatos tras estabilización)

| Flujo | Razón para posponer |
|---|---|
| FLUJO-008: Validación de entrada | Lógica simple, bajo impacto financiero — cubre FluentValidation que ya tiene tests |
| FLUJO-009: Auth 401 | Framework de auth ya cubierto a nivel transversal; bajo riesgo específico de este feature |

---

## Definition of Ready (DoR) — antes de automatizar

- [ ] El escenario fue ejecutado manualmente con éxito al menos una vez (sin bugs críticos abiertos)
- [ ] Los datos de prueba están definidos y disponibles (fixtures en `cotizador-core-mock/src/fixtures/`)
- [ ] El ambiente de test está estable (core-mock corriendo, MongoDB disponible)
- [ ] La spec SPEC-009 está en estado `IMPLEMENTED` o `IN_PROGRESS` con el endpoint funcional
- [ ] El equipo aprobó la estrategia de datos sintéticos (sin datos de producción)

## Definition of Done (DoD) — para cerrar automatización

- [ ] El test pasa en pipeline CI (`dotnet test` / `npx playwright test`)
- [ ] El código del test fue revisado por un par del equipo
- [ ] El test falla correctamente cuando se introduce un bug intencional (mutation testing básico)
- [ ] No hay dependencias entre tests (cada test es idempotente y puede ejecutarse en cualquier orden)
- [ ] El test está integrado al pipeline de GitHub Actions
- [ ] El resultado se reporta en el canal de calidad del equipo

---

## Impacto en Criterios de Evaluación del Reto

| Criterio del Reto | Flujos que lo cubren | Cobertura |
|---|---|---|
| Trazabilidad del cálculo (prima neta → comercial) | FLUJO-001, FLUJO-002 | ✅ Alta |
| Consistencia de APIs y manejo de errores | FLUJO-003, FLUJO-004, FLUJO-005 | ✅ Alta |
| Integridad de datos (persistencia atómica) | FLUJO-003, FLUJO-006 | ✅ Alta |
| Manejo de estados parciales (incompletas sin bloqueo) | FLUJO-001, FLUJO-007 | ✅ Alta |
| Resiliencia ante dependencias externas | FLUJO-004 | ✅ Alta |

---

## Estimación de Esfuerzo

| Sprint | Flujos | Días estimados |
|---|---|---|
| Sprint 1 (P1) | FLUJO-001, 002, 003, 004 | 7.5 días |
| Sprint 2 (P2) | FLUJO-005, 006, 007 | 4.5 días |
| **Total P1+P2** | **7 flujos** | **12 días (~2.4 sprints de 5 días)** |
