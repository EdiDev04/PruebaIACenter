# spec: SPEC-003 + SPEC-004
# feature: folio-creation + general-info-management
# date: 2026-03-29
# author: qa-agent
# quality-gate: PASSED

# Propuesta de Automatización — SPEC-003 + SPEC-004

## Resumen Ejecutivo

| Métrica                            | Valor                                              |
|------------------------------------|----------------------------------------------------|
| Flujos candidatos analizados       | 12                                                 |
| Flujos P1 (automatizar de inmediato) | 5                                                |
| Flujos P2 (próximo sprint)         | 4                                                  |
| Flujos a posponer                  | 3                                                  |
| Frameworks recomendados            | Playwright (E2E), xUnit + Moq (backend), Vitest + Testing Library (frontend) |
| Costo estimado de implementación   | 2 sprints                                          |

> **Criterio de ROI**: `ROI = (costo_manual_min × frecuencia_mensual) / costo_implementación_horas`
> Valores de frecuencia basados en el ciclo de desarrollo del reto (mínimo 1 ejecución/PR + 2 ejecuciones de regresión/mes).

---

## Matriz de Priorización

| ID Flujo | Nombre del Flujo                                   | HU Origen    | Repetitivo     | Estable | Alto Impacto | Costo Manual | ROI   | Prioridad |
|----------|----------------------------------------------------|--------------|----------------|---------|--------------|--------------|-------|-----------|
| FLUJO-01 | Ciclo completo: crear folio → guardar datos generales | HU-003-01 + HU-004-05 | ✅ Alta (cada PR) | ✅ Sí | ✅ Alto | ✅ Alto (20 min) | 4/4 | **P1** |
| FLUJO-02 | Idempotencia: mismo `Idempotency-Key` no duplica  | HU-003-01    | ✅ Alta         | ✅ Sí  | ✅ Alto        | ✅ Alto (15 min) | 4/4 | **P1** |
| FLUJO-03 | Conflicto de versión 409 → UI muestra alerta       | HU-004-05    | ✅ Alta         | ✅ Sí  | ✅ Alto        | ✅ Alto (12 min) | 4/4 | **P1** |
| FLUJO-04 | core-mock indisponible → 503 + no persiste datos   | HU-003-04    | ✅ Alta         | ✅ Sí  | ✅ Alto        | ✅ Alto (18 min) | 4/4 | **P1** |
| FLUJO-05 | Agente inexistente en core-mock → 422              | HU-004-03    | ✅ Alta         | ✅ Sí  | ✅ Alto        | ✅ Alto (10 min) | 4/4 | **P1** |
| FLUJO-06 | Abrir folio existente → redirige al wizard correcto | HU-003-02 + HU-003-03 | ✅ Alta | ✅ Sí | ⚠️ Medio | ✅ Alto (8 min) | 3/4 | **P2** |
| FLUJO-07 | Guardar solo campos obligatorios (email/phone null) | HU-004-01    | ✅ Alta         | ✅ Sí  | ⚠️ Medio       | ⚠️ Medio (6 min) | 2/4 | **P2** |
| FLUJO-08 | Selector de suscriptor → autocompletado de oficina | HU-004-02    | ✅ Alta         | ✅ Sí  | ⚠️ Medio       | ✅ Alto (8 min) | 3/4 | **P2** |
| FLUJO-09 | BusinessType inválido → 400 con mensaje correcto   | HU-004-04    | ✅ Alta         | ✅ Sí  | ⚠️ Medio       | ⚠️ Medio (5 min) | 2/4 | **P2** |
| FLUJO-10 | Formato de folio inválido en GET → 400             | HU-003-02    | ⚠️ Media        | ✅ Sí  | ⚠️ Medio       | ❌ Bajo (3 min) | 1/4 | Posponer |
| FLUJO-11 | Selección de clasificación de riesgo (dropdown)    | HU-004-04    | ⚠️ Media        | ❌ No  | ⚠️ Medio       | ❌ Bajo (4 min) | 1/4 | Posponer |
| FLUJO-12 | Header `Idempotency-Key` ausente → 400            | HU-003-01    | ⚠️ Media        | ✅ Sí  | ❌ Bajo        | ❌ Bajo (2 min) | 1/4 | Posponer |

---

## Framework Recomendado

### 1. Backend (API + Use Cases) → `xUnit + Moq + FluentAssertions`
**Justificación:**
- Stack tecnológico del proyecto es .NET 8 con xUnit ya configurado en `Cotizador.Tests/`.
- Moq permite mockear `ICoreOhsClient` e `IQuoteRepository` para aislar cada Use Case.
- FluentAssertions produce mensajes de error legibles en falla.
- Cobertura objetivo: FLUJO-02, FLUJO-04, FLUJO-05 (unit), FLUJO-01, FLUJO-03 (integración).

**Casos de uso cubiertos**: `CreateFolioUseCase`, `GetQuoteSummaryUseCase`, `GetGeneralInfoUseCase`, `UpdateGeneralInfoUseCase`.

### 2. Frontend (componentes y slices) → `Vitest + Testing Library`
**Justificación:**
- Stack frontend es React + Vite con Vitest ya configurado (`cotizador-webapp/src/__tests__/setup.ts`).
- Testing Library permite probar componentes desde la perspectiva del usuario (sin exponer implementación).
- Cobertura objetivo: FLUJO-08 (subscriber autocomplete), FLUJO-03 (409 UI alert), FLUJO-06 (redirect logic).

**Slices a cubrir**: `quoteWizardSlice`, `generalInfoSlice`, `folioCreationSlice`.

### 3. E2E (flujos completos navegador) → `Playwright`
**Justificación:**
- FLUJO-01 (ciclo completo) requiere probar la integración backend + frontend + redirección de wizard en un navegador real.
- Playwright soporta multi-browser, es CI-first y tiene soporte nativo TypeScript (compatible con el stack frontend).
- `cotizador-automatization/` ya existe como carpeta de automatización — Playwright se configura ahí.

**Flujos E2E prioritarios**: FLUJO-01, FLUJO-03 (409 en UI), FLUJO-06 (apertura de folio + redirect).

### 4. Carga y resiliencia → `k6`
**Justificación:**
- FLUJO-04 (resiliencia core-mock) y la idempotencia bajo carga (FLUJO-02 concurrente) requieren simular múltiples VUs enviando el mismo `Idempotency-Key`.
- k6 se integra con CI/CD y permite definir umbrales (e.g., `http_req_failed < 1%`).
- Solo si se definen SLAs formales; de lo contrario, el test de integración unitario es suficiente para el reto.

---

## Hoja de Ruta

### Sprint 1 — P1 (Automatizar de inmediato)

| Flujo     | Framework       | Tipo       | Estimación | Responsable sugerido       |
|-----------|-----------------|------------|------------|---------------------------|
| FLUJO-01  | Playwright      | E2E        | 1.5 días   | Test Engineer Frontend     |
| FLUJO-02  | xUnit + Moq     | Unit       | 0.5 días   | Test Engineer Backend      |
| FLUJO-03  | Vitest + xUnit  | Unit + E2E | 1.0 día    | Test Engineer Frontend/BE  |
| FLUJO-04  | xUnit + Moq     | Unit       | 0.5 días   | Test Engineer Backend      |
| FLUJO-05  | xUnit + Moq     | Unit       | 0.5 días   | Test Engineer Backend      |

**Total Sprint 1**: ~4 días

### Sprint 2 — P2

| Flujo     | Framework            | Tipo       | Estimación | Responsable sugerido      |
|-----------|----------------------|------------|------------|--------------------------|
| FLUJO-06  | Playwright           | E2E        | 1.0 día    | Test Engineer Frontend    |
| FLUJO-07  | xUnit + Moq          | Unit       | 0.5 días   | Test Engineer Backend     |
| FLUJO-08  | Vitest + Testing Lib | Unit       | 0.5 días   | Test Engineer Frontend    |
| FLUJO-09  | xUnit + Moq          | Unit       | 0.5 días   | Test Engineer Backend     |

**Total Sprint 2**: ~2.5 días

### Posponer (Backlog)

| Flujo     | Razón para posponer                                                   |
|-----------|----------------------------------------------------------------------|
| FLUJO-10  | Bajo costo manual (3 min), cubierto implícitamente por FLUJO-01       |
| FLUJO-11  | Feature de UI inestable (diseño pendiente de aprobación)              |
| FLUJO-12  | Bajo impacto, cubierto por el test de validación de FLUJO-01          |

---

## Justificación frente a Criterios del Reto

| Criterio del Reto                             | Flujo que lo cubre           | Prioridad |
|-----------------------------------------------|------------------------------|-----------|
| Trazabilidad del ciclo de cotización          | FLUJO-01 (E2E completo)      | P1        |
| Consistencia de APIs + manejo de errores      | FLUJO-04, FLUJO-05           | P1        |
| Concurrencia y versionado optimista           | FLUJO-02, FLUJO-03           | P1        |
| Validación de inputs en frontend y backend    | FLUJO-07, FLUJO-09           | P2        |
| UX: redirección correcta al wizard            | FLUJO-06                     | P2        |

---

## Definition of Ready (DoR) de Automatización

- [ ] El escenario Gherkin correspondiente está aprobado en `docs/output/qa/`
- [ ] El caso fue ejecutado manualmente sin bugs bloqueantes
- [ ] Los datos de prueba (fixtures) están identificados y disponibles en `cotizador-core-mock/src/fixtures/`
- [ ] El ambiente local está estable: `cotizador-backend`, `cotizador-core-mock` y `cotizador-webapp` levantan sin errores
- [ ] El riesgo asociado está clasificado como ALTO (R-001..R-008) o tiene aprobación explícita para riesgo MEDIO

## Definition of Done (DoD) de Automatización

- [ ] El código de automatización fue revisado por un par del equipo
- [ ] El test está integrado al pipeline CI (GitHub Actions o equivalente)
- [ ] La trazabilidad con la HU está documentada con tags (`@HU-003-01`, `@HU-004-05`, etc.)
- [ ] El test es determinista: pasa en 3 ejecuciones consecutivas en ambiente limpio
- [ ] Los datos de prueba no dependen de estado previo (cada test es independiente / `AfterEach` limpia MongoDB)
