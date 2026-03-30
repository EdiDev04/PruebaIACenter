# Propuesta de Automatización — SPEC-009 Amendment v1.1
## RN-009-02b: Cruce de `enabledGuarantees` con garantías de ubicación

**Referencia:** SPEC-009 v1.1 · RN-009-02b  
**Generado:** 2026-03-30 · **Agente:** QA Agent

---

## Resumen Ejecutivo

| Candidatos | P1 (automatizar ya) | P2 (próximo sprint) | Posponer |
|-----------|--------------------|--------------------|---------|
| 6 | 4 | 2 | 0 |

**Frameworks recomendados:**
- **API / Regresión:** Postman + Newman — el proyecto ya expone OpenAPI y el equipo usa Postman para pruebas manuales. Integración directa con CI/CD (GitHub Actions).
- **E2E (wizard):** Playwright — stack TypeScript, multi-browser, CI-first, consistente con el stack del webapp.

> El amendment es acotado (1 regla de negocio en 1 endpoint). El foco de automatización es la API, no la UI; los escenarios E2E son complementarios.

---

## Matriz de Priorización (ROI)

| ID | Flujo | Repetitivo | Estable | Alto Impacto | Costo Manual | ROI | Prioridad |
|----|-------|-----------|---------|-------------|-------------|-----|-----------|
| AUTO-009-A-01 | Garantía habilitada → calculable (GH-01) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | P1 |
| AUTO-009-A-02 | Garantía deshabilitada → incomplete (GH-02) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | P1 |
| AUTO-009-A-03 | Todas deshabilitadas → HTTP 422 (GH-03) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | P1 |
| AUTO-009-A-04 | EnabledGuarantees vacío/null → sin filtro (GH-04/04b) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | P1 — antiregresión crítico |
| AUTO-009-A-05 | Mix 2+1 con resultado mixto (GH-05) | ⚠️ Media | ✅ Sí | ✅ Alta | ✅ Alto | 3/4 | P2 |
| AUTO-009-A-06 | E2E wizard: deshabilitar garantía → siguiente cálculo refleja cambio | ⚠️ Media | ⚠️ Sí* | ✅ Alta | ✅ Alto | 3/4 | P2 |

> *AUTO-009-A-06 depende de la estabilidad del wizard UI (Step 3 y Step 4). Si el UI está en cambio activo, posponer hasta que se estabilice.

---

## Hoja de Ruta

### Sprint actual — P1 (Postman/Newman)

#### AUTO-009-A-01: Happy path — garantía habilitada calcula
- **Framework:** Postman collection + Newman
- **Escenario cubierto:** AMEND-009-GH-01
- **Riesgo cubierto:** AMEND-009-R-02 (status response vs BD)
- **Estimación:** 2h (setup colección + assertions)
- **ROI:** Detecta regresión en el camino feliz en < 30s por ejecución CI

#### AUTO-009-A-02: Garantía deshabilitada degrada ubicación
- **Framework:** Postman collection + Newman
- **Escenario cubierto:** AMEND-009-GH-02
- **Riesgo cubierto:** AMEND-009-R-02, AMEND-009-R-05
- **Estimación:** 2h
- **ROI:** Valida la regla central de RN-009-02b en cada pipeline

#### AUTO-009-A-03: Todas deshabilitadas → HTTP 422 sin escritura
- **Framework:** Postman collection + Newman + script de verificación GET post-422
- **Escenario cubierto:** AMEND-009-GH-03
- **Riesgo cubierto:** AMEND-009-R-03 (atomicidad)
- **Estimación:** 3h (incluye verificación de integridad del folio vía GET)
- **ROI:** Previene corrupción de folios en producción — costo manual de verificar estado post-error es alto

#### AUTO-009-A-04: EnabledGuarantees null/vacío → sin filtro (antiregresión)
- **Framework:** Postman collection + Newman (2 requests: null y [])
- **Escenario cubierto:** AMEND-009-GH-04, AMEND-009-GH-04b
- **Riesgo cubierto:** AMEND-009-R-01, AMEND-009-R-04
- **Estimación:** 2h
- **ROI:** Protege folios de etapas tempranas del wizard (Steps 1-2 sin Step 3 completado). Regresión silenciosa de alto costo si llega a producción.

---

### Próximo sprint — P2

#### AUTO-009-A-05: Resultado mixto 2 calculables + 1 degradada
- **Framework:** Postman collection + Newman
- **Escenario cubierto:** AMEND-009-GH-05
- **Riesgo cubierto:** AMEND-009-R-05 (propagación de prima comercial)
- **Estimación:** 3h (assertions detalladas por ubicación + verificación de totales)
- **Condición de entrada:** AUTO-009-A-01 a A-04 en CI verde

#### AUTO-009-A-06: E2E wizard — deshabilitar garantía y recalcular
- **Framework:** Playwright (TypeScript)
- **Flujo cubierto:** Step 3 (deshabilitar garantía en coverage options) → Step 4 (ejecutar cálculo) → verificar que la ubicación aparece como incomplete en el resultado
- **Riesgo cubierto:** AMEND-009-R-02 (experiencia de usuario ante la inconsistencia de estado)
- **Estimación:** 5h (incluye page objects para Step 3 y Step 4)
- **Condición de entrada:** UI de Step 3 y Step 4 estable (sin cambios activos en sprint)

---

## DoR de Automatización

- [ ] Escenario ejecutado manualmente con éxito (sin bugs críticos abiertos)
- [ ] Ambiente de QA estable con datos de prueba seed disponibles
- [ ] Folio de prueba `DAN-2026-00100` con estados conocidos cargado en seed
- [ ] core-mock ejecutándose en ambiente QA (endpoints de tarifas y CP disponibles)
- [ ] Aprobación del equipo para incluir en pipeline CI

## DoD de Automatización

- [ ] Cada test tiene assertions sobre status HTTP, body y (donde aplica) estado del folio post-request
- [ ] Tests integrados en GitHub Actions pipeline (job `qa-regression`)
- [ ] Ejecución < 2 min para el bloque P1 completo (Newman)
- [ ] Código revisado por pares
- [ ] Resultados de Newman publicados como artifact en el pipeline
