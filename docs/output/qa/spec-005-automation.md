# Propuesta de Automatización con ROI — SPEC-005: Configuración del Layout de Ubicaciones

> **Feature:** `location-layout-configuration`
> **Generado:** 2026-03-29
> **Derivado de:** `.github/specs/location-layout-configuration.spec.md` + `docs/output/qa/spec-005-gherkin.md`
> **Stack:** Backend .NET 8 · Frontend React + Vite · DB MongoDB · E2E Playwright

---

## Resumen ejecutivo

| Métrica | Valor |
|---------|-------|
| Flujos candidatos evaluados | 14 |
| P1 — Automatizar en Sprint 1 | 6 |
| P2 — Automatizar en Sprint 2 | 5 |
| P3 / Posponer | 3 |
| Frameworks recomendados | xUnit + Moq (BE) · Vitest + Testing Library (FE) · Playwright (E2E) |
| Ahorro estimado por ciclo | ~95 min de ejecución manual evitados por build |

**Criterio de evaluación del reto:** Los flujos P1 priorizan los criterios de evaluación del reto: "consistencia de contratos API", "versionado optimista" y "actualización parcial sin pérdida de datos".

---

## Matriz de priorización ROI

| ID | Flujo | Repetitivo | Estable | Alto impacto | Costo manual | ROI | Prioridad | Framework |
|----|-------|:----------:|:-------:|:------------:|:------------:|:---:|:---------:|-----------|
| F-001 | PUT layout — versionado optimista (409) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ 12 min | 4/4 | **P1** | xUnit + Moq |
| F-002 | PUT layout — actualización parcial (no toca otras secciones) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ 15 min | 4/4 | **P1** | xUnit + integración MongoDB |
| F-003 | GET layout — defaults cuando no hay configuración | ✅ Alta | ✅ Sí | ✅ Alta | ✅ 8 min | 4/4 | **P1** | xUnit + Moq |
| F-004 | PUT layout — validación displayMode (enum estricto) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ 10 min | 4/4 | **P1** | xUnit (validator tests) |
| F-005 | PUT layout — visibleColumns vacío bloqueado BE+FE | ✅ Alta | ✅ Sí | ✅ Alta | ✅ 10 min | 4/4 | **P1** | xUnit (BE) + Vitest (FE) |
| F-006 | metadata.lastWizardStep actualizado a 2 en MongoDB | ✅ Alta | ✅ Sí | ✅ Alta | ✅ 12 min | 4/4 | **P1** | xUnit + integración MongoDB |
| F-007 | Ciclo completo E2E: cargar layout → editar → guardar → reload | ✅ Alta | ⚠️ Media | ✅ Alta | ✅ 15 min | 3/4 | **P2** | Playwright |
| F-008 | Toggle grid/list — preview inmediato y sin persistir sort | ✅ Alta | ✅ Sí | ⚠️ Media | ✅ 8 min | 3/4 | **P2** | Vitest + Testing Library |
| F-009 | GET layout con folio inexistente → 404 | ✅ Alta | ✅ Sí | ⚠️ Media | ✅ 5 min | 3/4 | **P2** | xUnit |
| F-010 | Alerta ámbar 409 en UI con botón Recargar | ✅ Alta | ⚠️ Media | ✅ Alta | ✅ 10 min | 3/4 | **P2** | Playwright / Vitest |
| F-011 | Restaurar predeterminados — reset al estado default | ⚠️ Media | ✅ Sí | ⚠️ Media | ✅ 6 min | 2/4 | **P2** | Vitest + Testing Library |
| F-012 | Autenticación — 401 sin header Authorization | ✅ Alta | ✅ Sí | ✅ Alta | ❌ 3 min | 2/4 | **P3** | xUnit (ya cubierto por middleware global) |
| F-013 | Drift de contrato FE↔BE — types TS vs. schema C# | ⚠️ Media | ✅ Sí | ✅ Alta | ❌ 2 min | 2/4 | **P3** | Revisión estática (TypeScript + OpenAPI) |
| F-014 | Violaciones FSD — importaciones cruzadas entre layers | ⚠️ Media | ✅ Sí | ⚠️ Media | ❌ 2 min | 1/4 | **Posponer** | ESLint FSD plugin |

---

## Hoja de ruta de automatización

### Sprint 1 — P1 (Riesgos Alto · Bloqueantes de release)

> **Objetivo:** Cubrir los 7 riesgos clasificados como Alto antes de que el código llegue a la rama principal. Estas pruebas deben ejecutarse en el pipeline CI en cada push.

#### F-001 — Versionado optimista (409)
- **Framework:** xUnit + Moq
- **Clase de test:** `UpdateLayoutUseCaseTests`
- **Descripción:** Mock de `IQuoteRepository.UpdateLayoutAsync` retorna `ModifiedCount == 0` → verificar que se lanza `VersionConflictException` → integration test verifica HTTP 409 con body exacto
- **Estimación:** 0.5 días
- **Valor para el reto:** Criterio "consistencia de APIs y manejo de errores"

```csharp
// Ejemplo de esqueleto — xUnit + Moq
[Fact]
public async Task Execute_WhenVersionMismatch_ThrowsVersionConflictException()
{
    // Arrange
    _mockRepo.Setup(r => r.UpdateLayoutAsync(It.IsAny<string>(), It.IsAny<LayoutConfiguration>(), 2))
             .ReturnsAsync(0); // ModifiedCount == 0
    // Act + Assert
    await Assert.ThrowsAsync<VersionConflictException>(
        () => _useCase.ExecuteAsync("DAN-2026-00001", new UpdateLayoutRequest("grid", ["index"], 2))
    );
}
```

#### F-002 — Actualización parcial (no toca otras secciones)
- **Framework:** xUnit + integración MongoDB (Testcontainers o InMemory)
- **Clase de test:** `UpdateLayoutIntegrationTests`
- **Descripción:** Insertar folio completo → PUT layout → leer documento completo → verificar que `insuredData`, `locations`, `coverageOptions` son idénticos al estado previo
- **Estimación:** 1 día (incluye setup de contenedor MongoDB en CI)
- **Valor para el reto:** Criterio "actualización parcial sin pérdida de datos"

#### F-003 — GET defaults cuando no hay configuración
- **Framework:** xUnit + Moq
- **Clase de test:** `GetLayoutUseCaseTests`
- **Descripción:** Mock retorna folio con `LayoutConfiguration == null` → verificar response con `displayMode:"grid"` y 5 columnas exactas
- **Estimación:** 0.5 días

#### F-004 — Validación displayMode enum estricto
- **Framework:** xUnit (FluentValidation Tests)
- **Clase de test:** `UpdateLayoutRequestValidatorTests`
- **Descripción:** Parametrizar con `[InlineData]` para todos los valores válidos e inválidos (ver tabla §Datos de prueba en Gherkin)
- **Estimación:** 0.5 días

#### F-005 — visibleColumns vacío bloqueado en BE y FE
- **Framework:** xUnit (BE) + Vitest + Testing Library (FE)
- **Clases de test:** `UpdateLayoutRequestValidatorTests` + `LayoutConfigPanel.test.tsx`
- **Descripción BE:** Validador rechaza `[]` → 400 con `field:"visibleColumns"`
- **Descripción FE:** Con 1 columna seleccionada, el checkbox está `disabled` y el botón "Guardar" está `disabled`
- **Estimación:** 0.5 días

#### F-006 — metadata.lastWizardStep actualizado a 2
- **Framework:** xUnit + integración MongoDB
- **Clase de test:** `UpdateLayoutIntegrationTests`
- **Descripción:** Tras PUT exitoso, consultar documento MongoDB y verificar `metadata.lastWizardStep === 2`
- **Estimación:** 0.25 días (se puede incluir en el test de F-002)

---

### Sprint 2 — P2 (Cobertura funcional completa)

> **Objetivo:** Cubrir flujos funcionales de usuario y UI. Ejecutar en CI pero no blocantes de merge.

#### F-007 — Ciclo completo E2E
- **Framework:** Playwright (TypeScript)
- **Archivo sugerido:** `cotizador-automatization/e2e/specs/layout-configuration.spec.ts`
- **Descripción:** Crear folio (o usar fixture) → navegar a `/quotes/DAN-2026-00001/locations` → abrir panel de layout → cambiar a modo lista → seleccionar 3 columnas → Guardar → recargar página → verificar que persiste
- **Estimación:** 1.5 días (incluye setup de fixtures y autenticación en Playwright)
- **Valor para el reto:** Criterio "trazabilidad del flujo de configuración"

```typescript
// Esqueleto Playwright
test('ciclo completo: editar y persistir layout', async ({ page }) => {
  await page.goto('/quotes/DAN-2026-00001/locations');
  await page.click('[data-testid="layout-config-panel-toggle"]');
  await page.click('[data-testid="mode-list"]');
  await page.click('[data-testid="col-address"]'); // deseleccionar
  await page.click('[data-testid="save-layout"]');
  await expect(page.locator('[data-testid="layout-mode"]')).toHaveText('Vista de lista');
  await page.reload();
  await expect(page.locator('[data-testid="layout-mode"]')).toHaveText('Vista de lista');
});
```

#### F-008 — Toggle grid/list + sort no persiste
- **Framework:** Vitest + Testing Library
- **Archivo:** `cotizador-webapp/src/__tests__/widgets/LayoutConfigPanel.test.tsx`
- **Descripción:** Renderizar `LayoutConfigPanel` con mock de `useLayoutQuery` → cambiar modo → verificar que el formulario tiene el nuevo modo pero no se llama a `useSaveLayout` hasta Guardar; verificar que body de PUT no incluye `sortBy`
- **Estimación:** 0.5 días

#### F-009 — GET 404 con folio inexistente
- **Framework:** xUnit + integración
- **Descripción:** HTTP GET sobre folio no registrado → 404 con body contractual exacto
- **Estimación:** 0.25 días (reusar setup de F-002)

#### F-010 — Alerta ámbar 409 en UI
- **Framework:** Playwright (o Vitest con MSW para interceptar)
- **Descripción:** Interceptar PUT con MSW → simular 409 → verificar que aparece alerta ámbar → clic "Recargar" → verificar GET re-ejecutado con versión actualizada
- **Estimación:** 0.75 días

#### F-011 — Restaurar predeterminados
- **Framework:** Vitest + Testing Library
- **Descripción:** Simular layout personalizado → clic "Restaurar predeterminados" → verificar que el formulario tiene los valores default antes de guardar
- **Estimación:** 0.25 días

---

### Sprint 3 / Posponer

| Flujo | Razón para posponer | Acción recomendada |
|-------|--------------------|--------------------|
| F-012 — Autenticación 401 | Cubierto por middleware global — testear en la capa de middleware, no en cada endpoint | Verificar en suite de seguridad existente |
| F-013 — Drift FE↔BE | Más eficiente con OpenAPI generado + validación de tipos TypeScript en build | Configurar `openapi-typescript` en el pipeline de build |
| F-014 — Violaciones FSD | ESLint con `eslint-plugin-boundaries` detecta esto en lint, no requiere test separado | Agregar al `.eslintrc` existente |

---

## Análisis de costo-beneficio por flujo

| Flujo | Costo manual por ciclo | Frecuencia/sprint | Costo total sprint | Esfuerzo automatizar | ROI en sprints |
|-------|----------------------|--------------------|-------------------|---------------------|----------------|
| F-001 (versionado 409) | 12 min | 8 ejecuciones | 96 min | 0.5 días | 1 sprint |
| F-002 (actualización parcial) | 15 min | 8 ejecuciones | 120 min | 1 día | 1.5 sprints |
| F-003 (defaults GET) | 8 min | 8 ejecuciones | 64 min | 0.5 días | 1 sprint |
| F-006 (lastWizardStep) | 12 min | 8 ejecuciones | 96 min | 0.25 días | 0.5 sprints |
| F-007 (E2E ciclo completo) | 15 min | 4 ejecuciones | 60 min | 1.5 días | 3 sprints |
| F-010 (alerta 409 UI) | 10 min | 4 ejecuciones | 40 min | 0.75 días | 4 sprints |

---

## Definition of Ready (DoR) para automatización

- [ ] El escenario Gherkin correspondiente fue ejecutado manualmente con éxito (sin defectos críticos abiertos)
- [ ] Los datos de prueba (fixtures) están disponibles en `cotizador-core-mock/src/fixtures/` o como documentos seed MongoDB
- [ ] El ambiente de CI tiene acceso a MongoDB (Testcontainers o instancia dedicada de test)
- [ ] El folio de referencia `DAN-2026-00001` existe o puede crearse como prerequisito del setup de tests
- [ ] SPEC-002 implementada (repositorio `UpdateLayoutAsync` disponible)

## Definition of Done (DoD) para automatización

- [ ] El test falla en rojo antes de la implementación (TDD)
- [ ] El test pasa en verde después de la implementación
- [ ] El test se ejecuta en el pipeline CI/CD sin intervención manual
- [ ] Código del test revisado por un miembro del equipo
- [ ] El test está etiquetado correctamente (`@smoke`, `@critico`, `@happy-path`, `@error-path`)
- [ ] Sin timeouts arbitrarios — usar aserciones con espera condicional (Playwright `waitFor`, xUnit async)

---

## Resumen de esfuerzo total

| Sprint | Flujos | Esfuerzo estimado |
|--------|--------|------------------|
| Sprint 1 — P1 | F-001, F-002, F-003, F-004, F-005, F-006 | 3 días |
| Sprint 2 — P2 | F-007, F-008, F-009, F-010, F-011 | 3.25 días |
| Sprint 3 (posponer) | F-012, F-013, F-014 | Inline con otras actividades |
| **Total** | **14 flujos** | **~6.25 días** |
