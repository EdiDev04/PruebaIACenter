# Propuesta de Automatización con ROI — Estado y Progreso de la Cotización (SPEC-008)

> **Feature:** `quote-state-progress`
> **Endpoint cubierto:** `GET /v1/quotes/{folio}/state`
> **Spec origen:** `.github/specs/quote-state-progress.spec.md`
> **Contexto:** Stack TypeScript (frontend Vite + React), .NET 8 (backend), orchestrator-lite activo.
> **Generado:** 2026-03-30 | **Agente:** qa-agent

---

## Resumen Ejecutivo

| Métrica | Valor |
|---------|-------|
| Flujos candidatos evaluados | 8 |
| P1 (automatizar ya) | 3 |
| P2 (próximo sprint) | 2 |
| P3 (backlog) | 2 |
| Posponer | 1 |
| Framework recomendado E2E | **Playwright** (TypeScript, CLI-first) |
| Framework recomendado contratos | **Zod schema validation** (integrado al frontend) |
| Framework tests unitarios | **xUnit + Moq + FluentAssertions** (.NET) |
| Costo estimado de implementación P1+P2 | **2 sprints** |

---

## Los 4 Criterios de Automatización (Regla ROI)

```
✅ REPETITIVO   — Se ejecuta en cada release, cada PR o diariamente
✅ ESTABLE      — No cambia con frecuencia (> 1 sprint sin cambios significativos)
✅ ALTO IMPACTO — Su falla en producción tiene consecuencias importantes para el negocio
✅ COSTO ALTO   — Ejecutarlo manualmente toma tiempo significativo o es propenso a error humano
```

**Criterio de evaluación del reto aplicado:**
> Los flujos priorizados en P1 son los que cubren directamente los criterios de evaluación del reto: **"trazabilidad del progreso del folio"**, **"consistencia de APIs"** y **"manejo de errores y estados"**.

---

## Matriz de Priorización ROI

| ID | Flujo | Repetitivo | Estable | Alto Impacto | Costo Manual | ROI | Prioridad |
|----|-------|-----------|---------|--------------|--------------|-----|-----------|
| FLUJO-001 | E2E wizard completo con state endpoint | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto (15 min) | 4/4 | **P1** |
| FLUJO-002 | Contrato schema `QuoteStateDto` (Zod) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto (8 min/drift) | 4/4 | **P1** |
| FLUJO-003 | Tests unitarios `GetQuoteStateUseCase` (9 casos §8.3) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto (20 min) | 4/4 | **P1** (diferido) |
| FLUJO-004 | Error paths API (400, 401, 404, 500) via supertest/.http | ✅ Alta | ✅ Sí | ⚠️ Media | ✅ Alto (10 min) | 3/4 | **P2** |
| FLUJO-005 | Test UX: `isLoading`/`isError` en `WizardLayout` | ✅ Alta | ✅ Sí | ⚠️ Media | ⚠️ Medio (5 min) | 3/4 | **P2** |
| FLUJO-006 | E2E doble pestaña — caché desactualizada | ❌ Baja | ✅ Sí | ⚠️ Media | ⚠️ Medio (8 min) | 2/4 | **P3** |
| FLUJO-007 | Test de performance `staleTime: 0` bajo navegación | ❌ Baja | ✅ Sí | ❌ Baja | ❌ Bajo (3 min) | 1/4 | **P3** |
| FLUJO-008 | Test `layoutConfiguration` path en `ProgressBar` | ❌ Baja | ❌ No (estética) | ❌ Baja | ❌ Bajo (2 min) | 1/4 | Posponer |

---

## Hoja de Ruta por Sprint

---

### Sprint 1 — P1: Cobertura crítica inmediata

#### FLUJO-001 — E2E Playwright: Flujo wizard completo con state endpoint

**Framework:** Playwright (TypeScript)
**Justificación:** Cubre el criterio de evaluación del reto "trazabilidad del progreso del folio" — el wizard completo consume `GET /state` en cada página. Es el flujo de mayor impacto del feature y el más costoso de ejecutar manualmente (15 minutos por ciclo completo de regresión).

**Escenarios a automatizar:**

```typescript
// cotizador-automatization/e2e/specs/quote-state-progress.spec.ts

test.describe('Estado y Progreso de la Cotización', () => {

  test('HU-008-01 — Folio draft muestra progreso inicial con defaults', async ({ page }) => {
    // Fixture: DAN-2026-00001 (draft)
    await page.goto('/quotes/DAN-2026-00001/general-info');
    await expect(page.locator('[data-testid="progress-generalInfo"]')).not.toHaveClass(/completed/);
    await expect(page.locator('[data-testid="progress-layoutConfiguration"]')).toHaveClass(/completed/);
    await expect(page.locator('[data-testid="progress-locations"]')).not.toHaveClass(/completed/);
    await expect(page.locator('[data-testid="progress-coverageOptions"]')).not.toHaveClass(/completed/);
  });

  test('HU-008-01 — Folio in_progress muestra datos generales completos', async ({ page }) => {
    // Fixture: DAN-2026-00002 (in_progress)
    await page.goto('/quotes/DAN-2026-00002/locations');
    await expect(page.locator('[data-testid="progress-generalInfo"]')).toHaveClass(/completed/);
    await expect(page.locator('[data-testid="location-alerts"]')).toBeVisible();
    await expect(page.locator('[data-testid="alert-location-name"]')).toContainText('Local sin CP');
  });

  test('HU-008-03 — Alerta de ubicación incompleta permite navegar sin bloquear', async ({ page }) => {
    await page.goto('/quotes/DAN-2026-00002/general-info');
    const alert = page.locator('[data-testid="location-alert-0"]');
    await expect(alert).toBeVisible();
    await alert.click();
    await expect(page).toHaveURL(/\/quotes\/DAN-2026-00002\/locations\/2/);
  });

  test('HU-008-05 — Folio calculado muestra resultado financiero en wizard', async ({ page }) => {
    // Fixture: DAN-2026-00003 (calculated)
    await page.goto('/quotes/DAN-2026-00003/terms-and-conditions');
    await expect(page.locator('[data-testid="net-premium"]')).toContainText('125,000.50');
    await expect(page.locator('[data-testid="commercial-premium"]')).toContainText('174,000.70');
  });

  test('@error-path — Sin auth redirige a login o muestra 401', async ({ page }) => {
    await page.context().clearCookies();
    const response = await page.request.get('/v1/quotes/DAN-2026-00001/state');
    expect(response.status()).toBe(401);
  });
});
```

**Estimación:** 3-4 días de implementación  
**Ahorro:** 15 min × N releases/sprint → ROI positivo desde el sprint 2

---

#### FLUJO-002 — Validación de Contrato Schema (Zod)

**Framework:** Zod (TypeScript, integrado en el código de producción del FE)
**Justificación:** Cubre el criterio de evaluación del reto "consistencia de APIs". El drift silencioso entre `QuoteStateDto` (BE) y los tipos TypeScript (FE) es clasificado como R-004 (riesgo Alto). Con Zod, cada request al endpoint valida el shape automáticamente.

**Implementación recomendada:**

```typescript
// cotizador-webapp/src/entities/quote-state/model/types.ts

import { z } from 'zod';

export const LocationAlertSchema = z.object({
  index: z.number(),
  locationName: z.string(),
  missingFields: z.array(z.string()),
});

export const ProgressSchema = z.object({
  generalInfo: z.boolean(),
  layoutConfiguration: z.boolean(),
  locations: z.boolean(),
  coverageOptions: z.boolean(),
});

export const LocationsStateSchema = z.object({
  total: z.number().nonnegative(),
  calculable: z.number().nonnegative(),
  incomplete: z.number().nonnegative(),
  alerts: z.array(LocationAlertSchema),
});

export const CalculationResultSchema = z.object({
  netPremium: z.number(),
  commercialPremiumBeforeTax: z.number(),   // 0 hasta SPEC-009
  commercialPremium: z.number(),
  premiumsByLocation: z.array(z.object({
    locationIndex: z.number(),
    locationName: z.string(),
    netPremium: z.number(),
    validationStatus: z.enum(['calculable', 'incomplete']),
    coveragePremiums: z.array(z.object({
      guaranteeKey: z.string(),
      insuredAmount: z.number(),
      rate: z.number(),
      premium: z.number(),
    })),
  })),
});

export const QuoteStateSchema = z.object({
  folioNumber: z.string().regex(/^DAN-\d{4}-\d{5}$/),
  quoteStatus: z.enum(['draft', 'in_progress', 'calculated']),
  version: z.number().positive(),
  progress: ProgressSchema,
  locations: LocationsStateSchema,
  readyForCalculation: z.boolean(),
  calculationResult: CalculationResultSchema.nullable(),
});

export type QuoteStateDto = z.infer<typeof QuoteStateSchema>;
```

**Test de contrato (xUnit + HTTP Client):**

```typescript
// cotizador-automatization/e2e/specs/quote-state-contract.spec.ts

test('Contrato — GET /state retorna shape QuoteStateDto válido', async ({ request }) => {
  const response = await request.get('/v1/quotes/DAN-2026-00002/state', {
    headers: { 'Authorization': 'Basic dXNlcjpwYXNz' }
  });
  expect(response.status()).toBe(200);
  const body = await response.json();
  const parsed = QuoteStateSchema.safeParse(body.data);
  expect(parsed.success, `Zod errors: ${JSON.stringify(parsed.error?.issues)}`).toBe(true);
});
```

**Estimación:** 2 días de implementación  
**Ahorro:** Cada drift de contrato detectado en CI en vez de en producción → ROI inmediato

---

#### FLUJO-003 — Tests Unitarios `GetQuoteStateUseCase` (9 casos §8.3) — **DIFERIDO**

**Framework:** xUnit + Moq + FluentAssertions (.NET)
**Estado:** ⏸️ **DIFERIDO** — orchestrator-lite activo (0% cobertura actual aceptado).

> ⚠️ **Activar orchestrator completo para SPEC-008** antes de implementar estos tests.

**9 casos de prueba a implementar (§8.3 de la spec):**

```csharp
// cotizador-backend/src/Cotizador.Tests/Application/GetQuoteStateUseCaseTests.cs

[Fact]
public async Task Caso1_FolioDraft_RetornaProgressConTodosEnFalseExceptoLayout()
{
    // Arrange: folio sin datos (Name="", Locations=[], EnabledGuarantees=[])
    // Act: ExecuteAsync("DAN-2026-00001")
    // Assert:
    result.Progress.GeneralInfo.Should().BeFalse();
    result.Progress.LayoutConfiguration.Should().BeTrue();  // RN-008-03
    result.Progress.Locations.Should().BeFalse();
    result.Progress.CoverageOptions.Should().BeFalse();
    result.CalculationResult.Should().BeNull();
    result.ReadyForCalculation.Should().BeFalse();
}

[Fact]
public async Task Caso2_FolioConDatosGenerales_RetornaGeneralInfoTrue()
{
    // Arrange: folio con InsuredData.Name = "Aseguradora S.A."
    // Assert: result.Progress.GeneralInfo.Should().BeTrue();
}

[Fact]
public async Task Caso3_FolioConUbicaciones_RetornaLocationsYConteoCorrector()
{
    // Arrange: 2 ubicaciones (1 calculable, 1 incomplete)
    // Assert:
    result.Progress.Locations.Should().BeTrue();
    result.Locations.Total.Should().Be(2);
    result.Locations.Calculable.Should().Be(1);
    result.Locations.Incomplete.Should().Be(1);
}

[Fact]
public async Task Caso4_FolioConGuarantiasHabilitadas_RetornaCoverageOptionsTrue()
{
    // Arrange: EnabledGuarantees = ["building_fire", "cat_tev"]
    // Assert: result.Progress.CoverageOptions.Should().BeTrue();
}

[Fact]
public async Task Caso5_FolioConGuarantiasVacias_RetornaCoverageOptionsFalse()
{
    // Arrange: EnabledGuarantees = []
    // Assert: result.Progress.CoverageOptions.Should().BeFalse();
}

[Fact]
public async Task Caso6_ConUnaCalculableYUnaIncompleta_ReadyForCalculationTrueConAlertasCorrectas()
{
    // Arrange: 1 calculable + 1 incomplete con blockingAlerts ["zipCode"]
    // Assert:
    result.ReadyForCalculation.Should().BeTrue();
    result.Locations.Alerts.Should().HaveCount(1);
    result.Locations.Alerts[0].MissingFields.Should().Contain("zipCode");
}

[Fact]
public async Task Caso7_SinUbicacionesCalculables_ReadyForCalculationFalse()
{
    // Arrange: Locations = [] o todas incomplete
    // Assert: result.ReadyForCalculation.Should().BeFalse();
}

[Fact]
public async Task Caso8_FolioCalculado_RetornaCalculationResultConDatosFinancieros()
{
    // Arrange: quoteStatus = "calculated", netPremium = 125000.50m
    // Assert:
    result.CalculationResult.Should().NotBeNull();
    result.CalculationResult!.NetPremium.Should().Be(125000.50m);
    result.QuoteStatus.Should().Be("calculated");
}

[Fact]
public async Task Caso9_FolioInexistente_LanzaFolioNotFoundException()
{
    // Arrange: repository returns null
    // Act + Assert:
    await sut.Invoking(x => x.ExecuteAsync("DAN-2026-99999"))
             .Should().ThrowAsync<FolioNotFoundException>();
}
```

**Estimación:** 2 días de implementación (al activar orchestrator)  
**Ahorro:** 20 min de testing manual por bug regression en derivación de progreso

---

### Sprint 2 — P2: Cobertura de rutas de error y resiliencia UX

#### FLUJO-004 — Error Paths API (400, 401, 404, 500)

**Framework:** Playwright `request` API o `.http` files + dotnet test  
**Escenarios:** folio malformado → 400, sin auth → 401, folio inexistente → 404, BD caída → 500

```typescript
// cotizador-automatization/e2e/specs/quote-state-errors.spec.ts

test.describe('Error Paths — GET /state', () => {
  test('400 — folio con formato inválido', async ({ request }) => {
    const r = await request.get('/v1/quotes/INVALIDO-001/state', auth);
    expect(r.status()).toBe(400);
    const body = await r.json();
    expect(body.type).toBe('validationError');
    expect(body.message).toBe('Formato de folio inválido. Use DAN-YYYY-NNNNN');
    expect(body.field).toBe('folio');
  });

  test('401 — sin header Authorization', async ({ request }) => {
    const r = await request.get('/v1/quotes/DAN-2026-00001/state');
    expect(r.status()).toBe(401);
    expect((await r.json()).type).toBe('unauthorized');
  });

  test('404 — folio bien formado pero inexistente', async ({ request }) => {
    const r = await request.get('/v1/quotes/DAN-2026-99999/state', auth);
    expect(r.status()).toBe(404);
    expect((await r.json()).message).toContain('DAN-2026-99999');
  });
});
```

**Estimación:** 1.5 días  
**Ahorro:** 10 min × N builds → ROI positivo desde sprint 3

---

#### FLUJO-005 — Test UX: Manejo de `isLoading`/`isError` en `WizardLayout`

**Framework:** Vitest + Testing Library (React)  
**Vinculado a:** R-002 (MAY-001 — UX silencioso)

```typescript
// cotizador-webapp/src/__tests__/app/WizardLayout.test.tsx

it('muestra skeleton cuando isLoading es true', async () => {
  server.use(http.get('*/state', () => HttpResponse.json({}, { status: 200 }), { once: false }));
  // Simular loading delay
  render(<WizardLayout />, { wrapper: TestProviders });
  expect(screen.getByTestId('progress-bar-skeleton')).toBeInTheDocument();
});

it('muestra mensaje de error cuando la query falla con 503', async () => {
  server.use(http.get('*/state', () => HttpResponse.json({}, { status: 503 })));
  render(<WizardLayout />, { wrapper: TestProviders });
  await screen.findByText(/No se pudo cargar el estado de la cotización/i);
  expect(screen.queryByTestId('progress-bar')).not.toBeInTheDocument();
});
```

**Estimación:** 1 día  
**Ahorro:** Detección temprana de regresiones en UX sin testing manual de escenarios de fallo

---

### Backlog — P3

#### FLUJO-006 — E2E Doble Pestaña (R-006)

**Escenario:** Abrir folio en 2 pestañas, editar en una, verificar que la otra no muestra datos obsoletos sin recargar.  
**Estimación:** 2 días (requiere coordinación de fixtures y browser contexts)  
**Prioridad:** Baja — impacto en UX pero sin consecuencias en datos (versionado optimista protege escrituras)

#### FLUJO-007 — Performance `staleTime: 0`

**Escenario:** Navegar entre 4 páginas del wizard 3 veces consecutivas, contar las requests a `GET /state`.  
Sin SLAs definidos, este test generaría métricas de referencia pero no puede fallar por umbral. Posponer hasta que se definan SLAs.

---

### Posponer

#### FLUJO-008 — Path de `layoutConfiguration` en `ProgressBar` (MIN-001)

Inestable por definición: depende de si existe una ruta `/locations/layout` en el router. Posponer hasta que el router esté finalizado y estabilizado.

---

## Framework — Selección y Justificación

| Tipo de test | Framework | Justificación |
|---|---|---|
| E2E (UI + API integrados) | **Playwright** | Stack TypeScript nativo, CLI-first, multi-browser, integración CI/CD sencilla con GitHub Actions |
| Validación de contrato | **Zod** (integrado en producción) | Sin fricción adicional — el schema vive en el código de producción y se valida en cada request |
| Tests unitarios backend | **xUnit + Moq + FluentAssertions** | Stack .NET estándar del proyecto — ya existe `Cotizador.Tests/` |
| Tests unitarios frontend | **Vitest + Testing Library** | Configurado en `vite.config.ts` y `__tests__/setup.ts` — no requiere setup adicional |
| Performance (futuro) | **k6** | Cuando se definan SLAs — no aplica en SPEC-008 (sin SLAs en spec) |

---

## DoR de Automatización — Checklist

Para cada flujo antes de iniciar la implementación:

- [ ] Caso de prueba ejecutado manualmente con éxito (sin bugs críticos abiertos)
- [ ] Fixtures de datos identificados y disponibles (`DAN-2026-00001` a `DAN-2026-00007`)
- [ ] Ambiente de CI/CD estable (no en frozen sprint)
- [ ] MAY-001 fix aplicado (FLUJO-005 requiere `isError` manejado)
- [ ] R-004 (Zod schema) implementado antes de FLUJO-002
- [ ] Aprobación del equipo de desarrollo

## DoD de Automatización — Checklist

Para cada flujo al finalizar:

- [ ] Código revisado por pares (o por qa-agent en modo ASDD)
- [ ] Datos de prueba desacoplados del código (fixtures externos, sin hardcoding)
- [ ] Integrado al pipeline CI (GitHub Actions — `cotizador-automatization/`)
- [ ] Documentación en `cotizador-automatization/README.md` (cómo correr, qué cubre)
- [ ] Trazabilidad: cada test referencia su HU o RN de origen (via comentarios o tags)
- [ ] Entregado al equipo de desarrollo con evidencia de ejecución

---

## Justificación ROI con Criterios del Reto

| Criterio de evaluación del reto | Flujo que lo cubre | Prioridad |
|---|---|---|
| Trazabilidad del progreso del folio | FLUJO-001 (E2E wizard) | P1 |
| Consistencia de APIs y contratos | FLUJO-002 (Zod schema), FLUJO-004 (error paths) | P1, P2 |
| Manejo de errores y estados | FLUJO-004 (400/401/404/500), FLUJO-005 (isError UX) | P2 |
| Lógica de derivación de progreso | FLUJO-003 (tests unitarios — diferidos) | P1 al activar orchestrator |
| Resiliencia (MongoDB no disponible) | FLUJO-004 (500), FLUJO-005 (isError) | P2 |

> **Nota:** FLUJO-003 tiene ROI 4/4 pero está **diferido** por el modelo orchestrator-lite. Al activar el orchestrator completo, debe pasar a P1 inmediato. Su no implementación es el riesgo R-001 (Alto) aceptado conscientemente.
