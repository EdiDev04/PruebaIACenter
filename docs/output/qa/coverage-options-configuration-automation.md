# Propuesta de Automatización con ROI — SPEC-007: Configuración de Opciones de Cobertura

> **Fuente:** `coverage-options-configuration.spec.md` v1.0 + `coverage-options-configuration-risks.md`
> **Generado:** 2026-03-29
> **Ciclo ASDD:** Lite — Fase 3 (Unit Tests) diferida. Automatización cubre solo capas API (Postman/Newman) y E2E (Playwright).
> **⚠️ Nota:** Para activar la suite de tests unitarios, ejecutar `/asdd-orchestrate` con flujo completo en la siguiente iteración.

---

## Resumen Ejecutivo

| Métrica | Valor |
|---|---|
| Flujos candidatos evaluados | 10 |
| **P1 — Automatizar ya** | **5** |
| **P2 — Automatizar próximo sprint** | **3** |
| **P3 — Posponer** | **2** |
| Framework API recomendado | Postman + Newman (CI) |
| Framework E2E recomendado | **Playwright** |
| Tests unitarios | ⏸ Diferidos — activar Fase 3 del orchestrator completo |
| Sprints estimados de implementación | 1.5 sprints |

---

## Criterios de evaluación (metodología ASDD)

Cada flujo se evalúa con los 4 criterios de automatización:

| Criterio | Descripción |
|---|---|
| ✅ **Repetitivo** | Se ejecuta cada release o sprint |
| ✅ **Estable** | No cambia con frecuencia (> 1 sprint) |
| ✅ **Alto Impacto** | Falla en producción con consecuencias importantes |
| ✅ **Costo Manual Alto** | Ejecutarlo manualmente es costoso o propenso a error |

---

## Matriz de priorización ROI

| ID | Flujo | Repetitivo | Estable | Alto Impacto | Costo Manual | ROI | Prioridad | Framework |
|---|---|---|---|---|---|---|---|---|
| F-01 | GET coverage-options — folio con config y folio con defaults | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** | Postman/Newman |
| F-02 | PUT coverage-options — actualización exitosa + versionado (409) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** | Postman/Newman |
| F-03 | GET catalogs/guarantees — proxy exitoso (14 garantías) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** | Postman/Newman |
| F-04 | E2E — ciclo completo: carga → edición → guardado exitoso + toast | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** | Playwright |
| F-05 | E2E — warning de deshabilitación de garantía con ubicaciones afectadas | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** | Playwright |
| F-06 | GET catalogs/guarantees — core-ohs no disponible (503) | ⚠️ Media | ✅ Sí | ✅ Alta | ✅ Alto | 3/4 | **P2** | Postman/Newman |
| F-07 | E2E — conflict 409 → banner ámbar → recarga del formulario | ⚠️ Media | ✅ Sí | ✅ Alta | ⚠️ Medio | 3/4 | **P2** | Playwright |
| F-08 | PUT — validaciones de entrada (vacío, key inválida, rango) | ✅ Alta | ✅ Sí | ⚠️ Media | ✅ Alto | 3/4 | **P2** | Postman/Newman |
| F-09 | E2E — catálogo no disponible → banner rojo → reintentar | ❌ Baja | ✅ Sí | ✅ Alta | ❌ Bajo | 2/4 | **P3** | Playwright |
| F-10 | E2E — "Seleccionar todas" por sección de garantías | ❌ Baja | ✅ Sí | ❌ Baja | ❌ Bajo | 1/4 | **P3** | Posponer |

---

## Selección de frameworks

### Backend API: Postman + Newman

**Justificación:**
- El stack backend es .NET 8. No existe stack Python ni Node en la capa de API del cotizador.
- Postman collections están siendo usadas en specs anteriores del proyecto (SPEC-002 a SPEC-006).
- Newman permite integrar las colecciones directamente al pipeline CI existente (GitHub Actions / Azure DevOps).
- Para el reto, la ejecución de Postman/Newman basta para demostrar "consistencia de APIs y manejo de errores" como criterio de evaluación.
- **No se recomienda k6 en esta feature** ya que la spec no define SLAs explícitos. Ver sección de performance.

**Estructura de colecciones Postman:**
```
cotizador-automatization/
└── postman/
    └── SPEC-007-coverage-options.postman_collection.json
        ├── Folder: GET coverage-options
        │   ├── API-01 — folio con config
        │   ├── API-02 — folio con defaults
        │   ├── API-03 — folio inexistente (404)
        │   ├── API-04 — folio inválido (400)
        │   └── API-05 — sin credenciales (401)
        ├── Folder: PUT coverage-options
        │   ├── API-06 — actualización exitosa
        │   ├── API-07 — conflict 409
        │   ├── API-08 — enabledGuarantees vacío
        │   ├── API-09 — key inválida
        │   ├── API-10 — deducible fuera de rango
        │   ├── API-11 — coaseguro negativo
        │   └── API-12 — sin credenciales
        └── Folder: GET catalogs/guarantees
            ├── API-13 — proxy exitoso (14 garantías)
            └── API-14 — core-ohs no disponible (503)
```

**Comando de ejecución CI:**
```bash
newman run postman/SPEC-007-coverage-options.postman_collection.json \
  --environment postman/envs/dev.postman_environment.json \
  --reporters cli,junit \
  --reporter-junit-export reports/spec-007-results.xml
```

---

### Frontend E2E: Playwright

**Por qué Playwright y no Cypress para este wizard:**

| Criterio | Playwright | Cypress |
|---|---|---|
| Soporte multi-browser | ✅ Chromium, Firefox, WebKit | ⚠️ Solo Chromium + Firefox (no Safari) |
| Tests E2E con iframes | ✅ Nativo | ⚠️ Limitado |
| Intercepción de red (route.fulfill) | ✅ Nativa — crítica para simular 409/503 | ⚠️ `cy.intercept` más verboso |
| Integración con TanStack Query cache | ✅ Sin problemas | ✅ Sin problemas |
| Stack del proyecto | ✅ TS nativo — mismo stack que `cotizador-webapp` | ✅ Soporta TS |
| CI-first (sin servidor de video) | ✅ Headless por defecto | ⚠️ Requiere Cypress Cloud para CI robusto |
| Simulación de concurrencia (versión 409) | ✅ `page.route()` intercepta y modifica response | ⚠️ Más complejo con `cy.intercept` |

**Decisión:** Playwright es la opción correcta para este wizard porque la simulación de errores de red (503 de core-ohs y 409 de versionado) se hace con `page.route()` de forma limpia y es part del stack CI-first del proyecto.

**Estructura de specs Playwright:**
```
cotizador-automatization/e2e/specs/
└── coverage-options/
    ├── coverage-options-load.spec.ts        # E2E-01 — carga inicial
    ├── coverage-options-warning.spec.ts     # E2E-02, E2E-03 — warning de deshabilitación
    ├── coverage-options-save.spec.ts        # E2E-04 — guardado exitoso + toast
    ├── coverage-options-conflict.spec.ts    # E2E-05 — 409 + banner ámbar + recarga
    └── coverage-options-catalog-error.spec.ts # E2E-06 — 503 + banner rojo + reintentar
```

**Ejemplo de intercepción para simular 409 (Playwright):**
```typescript
// coverage-options-conflict.spec.ts
test('E2E-05 — Conflicto de versión muestra banner ámbar', async ({ page }) => {
  await page.route('**/v1/quotes/DAN-2026-00001/coverage-options', (route) => {
    if (route.request().method() === 'PUT') {
      route.fulfill({
        status: 409,
        contentType: 'application/json',
        body: JSON.stringify({
          type: 'versionConflict',
          message: 'El folio fue modificado por otro proceso. Recargue para continuar',
          field: null,
        }),
      });
    } else {
      route.continue();
    }
  });

  await page.goto('/quotes/DAN-2026-00001/technical-info');
  await page.getByRole('button', { name: 'Guardar y Continuar' }).click();
  await expect(page.getByRole('alert')).toContainText('modificado por otro proceso');
  await expect(page.getByRole('button', { name: 'Recargar' })).toBeVisible();
});
```

---

## Hoja de Ruta de Automatización

### Sprint 1 — P1 (automatizar ya)

| Flujo | Escenarios | Estimación | Framework |
|---|---|---|---|
| F-01: GET coverage-options (happy + defaults) | API-01, API-02 | 2 h | Postman/Newman |
| F-02: PUT coverage-options (éxito + 409) | API-06, API-07 | 3 h | Postman/Newman |
| F-03: GET catalogs/guarantees (proxy exitoso) | API-13 | 1 h | Postman/Newman |
| F-04: E2E ciclo completo carga → guardado → toast | E2E-01, E2E-04 | 4 h | Playwright |
| F-05: E2E warning deshabilitación de garantía | E2E-02, E2E-03 | 3 h | Playwright |

**Estimación Sprint 1:** 13 horas · 2 ingenieros · ~1 sprint

### Sprint 2 — P2

| Flujo | Escenarios | Estimación | Framework |
|---|---|---|---|
| F-06: GET catalogs/guarantees — 503 | API-14 | 2 h | Postman/Newman |
| F-07: E2E conflict 409 → banner → recarga | E2E-05 | 3 h | Playwright |
| F-08: PUT — validaciones de entrada | API-08 a API-12 | 3 h | Postman/Newman |

**Estimación Sprint 2:** 8 horas · 1 ingeniero · ~0.5 sprint

### Fase 3 ASDD Completo — Tests Unitarios (diferidos)

> ⚠️ **Acción requerida:** Ejecutar `/asdd-orchestrate` con flujo completo para activar el Test Engineer Backend + Test Engineer Frontend y generar esta suite.

Al activar la Fase 3, los tests unitarios que deben generarse **primero** (en orden de prioridad por riesgo):

| Prioridad | Componente | Framework | Riesgo cubierto |
|---|---|---|---|
| 1 | `UpdateCoverageOptionsUseCase` — éxito, 409, 400 | xUnit + Moq | R-02 (versionado optimista) |
| 2 | `CoverageOptions` value object — constructor con defaults (14 garantías, `0`, `0`) | xUnit | R-07 (defaults) |
| 3 | `UpdateCoverageOptionsRequestValidator` — enabledGuarantees vacío, key inválida, deducible/coaseguro fuera de rango | xUnit + FluentValidation | R-08 (validaciones) |
| 4 | `GetCoverageOptionsUseCase` — folio con config, folio sin config (defaults), folio inexistente | xUnit + Moq | R-07 (defaults) |
| 5 | `coverageOptionsApiSchema.transform()` — conversión %→decimal bidireccional | Vitest | R-05 (conversión) |
| 6 | `useSaveCoverageOptions` hook — mutación exitosa, error 409, error 400 | Vitest + Testing Library | R-03 (warning) |
| 7 | `useDisableGuaranteeWarning` hook — count de ubicaciones afectadas, cache miss | Vitest + Testing Library | R-03 (warning) |

---

## Consideraciones de performance

La spec SPEC-007 **no define SLAs explícitos** para los endpoints de opciones de cobertura. Por lo tanto:

- **k6 no se incorpora en este ciclo.** No existe umbral de tiempo de respuesta o TPS definido contra el cual ejecutar una prueba de carga.
- **Recomendación para la siguiente iteración:** Si el reto o los requisitos no funcionales establecen un SLA (ej. `GET /v1/catalogs/guarantees` debe responder en < 500 ms P95 bajo 50 usuarios concurrentes), activar el skill `/performance-analyzer` para generar el plan k6.
- El único caso de performance implícito es la carga del catálogo de garantías (proxy a core-ohs): si core-ohs tiene latencia alta, el step 3 del wizard tarda en renderizar. Mitigación: `staleTime: 30min` en TanStack Query (ya definido en la spec) evita llamadas repetidas.

---

## DoR de Automatización — Checklist antes de implementar cada script

- [ ] El escenario fue ejecutado manualmente con éxito al menos una vez (sin bugs críticos abiertos)
- [ ] Los datos de prueba sintéticos están identificados y disponibles (ver tabla en gherkin)
- [ ] El ambiente de desarrollo/QA está estable y responde correctamente
- [ ] Los endpoints están accesibles (backend desplegado o mock activo)
- [ ] Aprobación del equipo confirmada

## DoD de Automatización — Checklist para dar por completado cada script

- [ ] Código del script revisado por al menos un par (o validado con Copilot)
- [ ] Los datos de prueba están desacoplados del código del script (variables de entorno / fixtures externos)
- [ ] El script está integrado al pipeline CI (GitHub Actions / Azure DevOps)
- [ ] El script tiene trazabilidad con el escenario Gherkin correspondiente (comentario con ID: API-06, E2E-04, etc.)
- [ ] El script fue entregado al equipo y está documentado en el README de `cotizador-automatization/`

---

## Relación con criterios de evaluación del reto

| Criterio del reto | Flujos que lo demuestran | Prioridad |
|---|---|---|
| Trazabilidad del cálculo | F-01, F-02: GET/PUT coverage-options persiste correctamente las opciones que influyen en el cálculo | P1 |
| Consistencia de APIs y manejo de errores | F-02 (409), F-06 (503), F-08 (400) | P1 / P2 |
| Resiliencia ante fallos de terceros | F-06 (core-ohs no disponible), F-07 y F-09 (UI resiliente) | P2 / P3 |
| Calidad de código | R-01 (0% unit tests diferidos) — activar Fase 3 del orchestrator | Siguiente iteración |
