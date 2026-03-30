# Propuesta de Automatización — SPEC-010: Visualización de Resultados y Alertas

> **Feature:** results-display  
> **Spec:** SPEC-010  
> **Framework recomendado:** Playwright (E2E visual) + Vitest (unitarios — ya implementados)  
> **Generado:** 2026-03-30

---

## Contexto de decisión

SPEC-010 es una pantalla 100% frontend con lógica de presentación de datos financieros, interacción visual (acordeón, alertas, estados condicionales) y flujos de usuario críticos (calcular / recalcular). La automatización E2E con Playwright es idónea porque:

- Los riesgos más altos (R-01 y R-02) son de coherencia visual de datos financieros — no detectables solo con unitarios.
- El acordeón y el panel de alertas requieren simulación real de interacción de usuario.
- El flujo de recálculo involucra invalidación de caché y re-render → necesita ciclo completo DOM.

La suite de 34 tests unitarios (SPEC-010 ASDD Lite) ya cubre la capa de componentes aislados. Esta propuesta se enfoca en los gaps que los unitarios no cubren.

---

## Flujos Priorizados para Automatización E2E

| Prioridad | ID Flujo | Flujo | Frecuencia de riesgo | Costo manual (min) | ROI estimado | Estado actual |
|-----------|----------|-------|---------------------|-------------------|--------------|---------------|
| 🔴 1 | F-01 | Ciclo completo: cálculo → visualización de resultados → 3 tarjetas correctas | Alta | 12 min | **Alto** | Sin E2E |
| 🔴 2 | F-02 | Coherencia financiera: suma desglose == total tarjeta prima neta | Alta | 8 min | **Alto** | Sin E2E |
| 🟠 3 | F-03 | Panel de alertas: ubicación incompleta + campos en español + link editar | Media | 6 min | **Alto** | Sin E2E |
| 🟠 4 | F-04 | Acordeón: expandir / colapsar cobertura por ubicación | Media | 5 min | **Medio** | Sin E2E |
| 🟡 5 | F-05 | Estado no calculado (ready=true): mensaje invitación + botón calcular | Media | 4 min | **Medio** | Parcial unitarios |
| 🟡 6 | F-06 | Estado no calculado (ready=false): mensaje + sin botón | Baja | 3 min | **Medio** | Parcial unitarios |
| 🟡 7 | F-07 | Recálculo: doble clic bloqueado + actualización de resultados | Media | 8 min | **Alto** | Sin E2E |
| 🟢 8 | F-08 | Responsive: visualización en viewport 375px sin overflow horizontal | Baja | 10 min | **Bajo** | Sin E2E |

---

## Justificación ROI por Flujo

### F-01 — Ciclo completo de cálculo y visualización *(Prioridad 1)*

**Por qué automatizar:**  
Es el flujo de valor principal del cotizador. El criterio de evaluación del reto exige "trazabilidad del cálculo". Un error en la presentación de resultados (ej: tarjeta no renderiza, prima muestra `NaN`) destruye la demo.

**Costo manual:** 12 min por ejecución × estimado 15 ejecuciones/sprint = **3 horas/sprint**  
**Costo de automatización:** ~2h desarrollo inicial  
**Break-even:** < 1 sprint  

```typescript
// Playwright — F-01: Ciclo completo
test('Visualización de resultados tras cálculo exitoso', async ({ page }) => {
  await page.goto('/quotes/DAN-2026-00010/terms-and-conditions');
  // Mock: GET /state retorna quoteStatus: 'calculated' con calculationResult
  await expect(page.getByText('Prima Neta Total')).toBeVisible();
  await expect(page.getByTestId('card-net-premium')).toContainText('$125.430,50');
  await expect(page.getByTestId('card-commercial-before-tax')).toContainText('$145.499,38');
  await expect(page.getByTestId('card-commercial-premium')).toContainText('$173.244,26');
});
```

---

### F-02 — Coherencia financiera: desglose == total *(Prioridad 2)*

**Por qué automatizar:**  
Riesgo R-02 catalogado como **Alto**. Detecta discrepancias entre el valor de consolidación del backend y lo que el componente `LocationBreakdown` renderiza. No detectable con tests unitarios de componente aislado.

**Costo manual:** 8 min por ejecución × 15/sprint = **2 horas/sprint**  
**Valor adicional:** Detecta regresos en el cálculo de consolidación (SPEC-009) que se manifiestan en la UI.

```typescript
// Playwright — F-02: Coherencia financiera
test('Suma de primas por ubicación coincide con prima neta total', async ({ page }) => {
  // Extraer texto de cada fila de ubicación y sumar
  const rows = page.getByTestId('location-row');
  let sum = 0;
  for (const row of await rows.all()) {
    const priceText = await row.getByTestId('location-net-premium').textContent();
    sum += parseFloat(priceText.replace(/[^0-9,]/g, '').replace(',', '.'));
  }
  const totalText = await page.getByTestId('card-net-premium').textContent();
  const total = parseFloat(totalText.replace(/[^0-9,]/g, '').replace(',', '.'));
  expect(sum).toBeCloseTo(total, 2);
});
```

---

### F-03 — Panel de alertas con campos en español *(Prioridad 3)*

**Por qué automatizar:**  
El criterio de evaluación del reto incluye "experiencia de usuario". Una alerta con `zipCode` en lugar de `Código Postal` es un defecto visible en la revisión. El riesgo R-04 es Medio pero de alto impacto en la evaluación.

```typescript
// Playwright — F-03: Panel de alertas
test('Ubicación incompleta muestra campos faltantes en español', async ({ page }) => {
  // Mock: alertLocations[0].missingFields = ['zipCode', 'businessLine.fireKey']
  await expect(page.getByTestId('alert-panel')).toBeVisible();
  await expect(page.getByTestId('alert-panel')).toContainText('Almacén Sur');
  await expect(page.getByTestId('alert-panel')).toContainText('Código Postal');
  await expect(page.getByTestId('alert-panel')).toContainText('Clave de incendio');
  await expect(page.getByTestId('alert-panel')).not.toContainText('zipCode');
  await expect(page.getByTestId('alert-panel')).not.toContainText('fireKey');
  
  // Verificar navegación
  await page.getByRole('link', { name: 'Editar ubicación' }).click();
  await expect(page).toHaveURL('/quotes/DAN-2026-00010/locations');
});
```

---

### F-04 — Acordeón de coberturas *(Prioridad 4)*

**Por qué automatizar:**  
La interacción de expansión/colapso es la funcionalidad diferenciadora de SPEC-010 (HU-010-03). Un acordeón roto bloquea al usuario de verificar las tasas aplicadas por los asesores.

```typescript
// Playwright — F-04: Acordeón
test('Expandir y colapsar acordeón de coberturas por ubicación', async ({ page }) => {
  const locationRow = page.getByTestId('location-row-bodega-central');
  
  // Cerrado por defecto
  await expect(page.getByTestId('coverage-table-bodega-central')).not.toBeVisible();
  
  // Expandir
  await locationRow.click();
  await expect(page.getByTestId('coverage-table-bodega-central')).toBeVisible();
  await expect(page.getByTestId('coverage-table-bodega-central').getByRole('row')).toHaveCount(5); // 4 coberturas + header
  
  // Colapsar
  await locationRow.click();
  await expect(page.getByTestId('coverage-table-bodega-central')).not.toBeVisible();
});
```

---

### F-07 — Botón Recalcular: doble clic bloqueado *(Prioridad 7)*

**Por qué automatizar:**  
Riesgo R-08 (envío duplicado). En el reto se evalúa "consistencia de APIs y manejo de errores". Enviar cálculos duplicados podría generar estados inconsistentes.

```typescript
// Playwright — F-07: Anti envío duplicado
test('Botón Recalcular deshabilitado durante ejecución', async ({ page }) => {
  // Mock: POST /calculate con delay de 2s
  await page.route('**/calculate', async route => {
    await new Promise(r => setTimeout(r, 2000));
    await route.fulfill({ json: { /* resultado */ } });
  });
  
  const btn = page.getByRole('button', { name: 'Recalcular' });
  await btn.click();
  await expect(btn).toBeDisabled();
  await page.waitForTimeout(2100);
  await expect(btn).toBeEnabled();
});
```

---

## Configuración sugerida — Playwright

```typescript
// playwright.config.ts
import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: 'cotizador-automatization/e2e',
  use: {
    baseURL: 'http://localhost:5173',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { browserName: 'chromium' } },
    { name: 'mobile', use: { viewport: { width: 375, height: 667 } } },
  ],
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    cwd: 'cotizador-webapp',
    reuseExistingServer: !process.env.CI,
  },
});
```

**Estrategia de mocks:** usar `page.route()` de Playwright para interceptar `GET /v1/quotes/*/state` y `POST /v1/quotes/*/calculate`. No depender del backend real en los tests E2E de SPEC-010.

---

## Resumen de inversión y ROI

| Métrica | Valor |
|---------|-------|
| Flujos priorizados a automatizar | 7 (F-01 a F-07) |
| Esfuerzo de desarrollo estimado | 8–10 horas |
| Esfuerzo de ejecución manual equivalente (por sprint) | ~56 min |
| Break-even (sprints) | ~9 sprints |
| Valor diferencial | Detección temprana R-01 y R-02 (datos financieros incorrectos) antes de demo del reto |

**Recomendación de priorización para el reto:**  
Implementar F-01, F-02 y F-03 como mínimo antes de la entrega. Estos 3 flujos cubren los criterios de evaluación de "trazabilidad del cálculo" y "experiencia de usuario" con el menor esfuerzo (estimado 4–5h) y el mayor ROI.
