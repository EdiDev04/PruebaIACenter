import { test, expect, type BrowserContext } from '@playwright/test';
import { FolioHomePage } from '../pages/FolioHomePage';
import { GeneralInfoPage } from '../pages/GeneralInfoPage';
import { VALID_GENERAL_INFO, ALTERNATE_GENERAL_INFO } from '../fixtures/test-data';

/**
 * Flujo 3 — Edición con versionado optimista
 *
 * Simula edición concurrente usando dos contextos de navegador aislados.
 * Cada contexto tiene su propio Redux store (estado JS independiente).
 *
 * Escenario:
 * 1. Contexto A crea el folio y guarda datos generales → versión incrementa.
 * 2. Contexto B navega al mismo folio antes de que A guarde (versión desactualizada).
 * 3. Contexto B intenta guardar → backend rechaza con conflicto de versión.
 * 4. Se verifica el modal de conflicto y el botón de recarga.
 */
test.describe('Flujo 3 — Edición concurrente y conflicto de versión', () => {
  let contextA: BrowserContext;
  let contextB: BrowserContext;

  test.beforeEach(async ({ browser }) => {
    contextA = await browser.newContext();
    contextB = await browser.newContext();
  });

  test.afterEach(async () => {
    await contextA.close();
    await contextB.close();
  });

  test(
    'segundo intento de edición con versión desactualizada muestra mensaje de conflicto',
    async () => {
      const pageA = await contextA.newPage();
      const pageB = await contextB.newPage();

      const homePageA = new FolioHomePage(pageA);
      const generalInfoA = new GeneralInfoPage(pageA);
      const generalInfoB = new GeneralInfoPage(pageB);

      // ── 1. Contexto A: crear folio ───────────────────────────────────────
      await homePageA.goto();
      const folioNumber = await homePageA.createNewFolio();
      expect(folioNumber).toMatch(/^DAN-\d{4}-\d{5}$/);

      // ── 2. Contexto A: ir a Datos Generales y cargar el formulario ───────
      await generalInfoA.proceedFromCreatedPage();

      // ── 3. Contexto B: abrir el mismo folio en Datos Generales ────────────
      // Ambos contextos cargan el folio con la misma versión inicial
      await pageB.goto(`/quotes/${folioNumber}/general-info`);
      await generalInfoB.waitForReady();

      // ── 4. Contexto A: llenar datos y guardar (versión incrementa) ────────
      await generalInfoA.fillAllData(VALID_GENERAL_INFO);
      await generalInfoA.saveAndContinue();

      // Confirmar que contexto A avanzó a ubicaciones
      await expect(pageA).toHaveURL(/\/quotes\/.+\/locations/);

      // ── 5. Contexto B: llenar datos distintos e intentar guardar ──────────
      // El store de contexto B aún tiene la versión anterior (desactualizada)
      await generalInfoB.fillAllData(ALTERNATE_GENERAL_INFO);
      await generalInfoB.saveExpectingConflict();

      // ── 6. Verificar modal de conflicto de versión ────────────────────────
      const conflictModal = generalInfoB.isVersionConflictModalVisible();
      await expect(conflictModal).toBeVisible();

      // Verificar texto de instrucción de recarga
      await expect(generalInfoB.getConflictMessage()).toBeVisible();

      // Verificar que el botón "Recargar datos" está presente
      await expect(generalInfoB.getReloadButton()).toBeVisible();

      // Verificar que el botón "Cancelar" también está presente
      await expect(generalInfoB.getCancelButton()).toBeVisible();

      // ── 7. Verificar acción de recarga ────────────────────────────────────
      await generalInfoB.getReloadButton().click();

      // El modal debe cerrarse después de recargar
      await expect(conflictModal).not.toBeVisible({ timeout: 8_000 });

      // El formulario debe seguir en la página de datos generales
      await generalInfoB.waitForReady();
    },
  );
});
