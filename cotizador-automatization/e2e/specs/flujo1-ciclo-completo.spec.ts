import { test, expect } from '@playwright/test';
import { FolioHomePage } from '../pages/FolioHomePage';
import { GeneralInfoPage } from '../pages/GeneralInfoPage';
import { LocationsPage } from '../pages/LocationsPage';
import { TechnicalInfoPage } from '../pages/TechnicalInfoPage';
import { ResultsPage } from '../pages/ResultsPage';
import { VALID_GENERAL_INFO, VALID_LOCATION } from '../fixtures/test-data';

test.describe('Flujo 1 — Ciclo completo de cotización', () => {
  test(
    'crear folio → datos generales → ubicación calculable → opciones de cobertura → calcular → verificar prima',
    async ({ page }) => {
      const homePage = new FolioHomePage(page);
      const generalInfoPage = new GeneralInfoPage(page);
      const locationsPage = new LocationsPage(page);
      const technicalInfoPage = new TechnicalInfoPage(page);
      const resultsPage = new ResultsPage(page);

      // ── 1. Crear folio nuevo ─────────────────────────────────────────────
      await homePage.goto();
      const folioNumber = await homePage.createNewFolio();
      expect(folioNumber).toMatch(/^DAN-\d{4}-\d{5}$/);

      // Verificar página de confirmación
      await expect(page.getByText('Folio creado exitosamente')).toBeVisible();
      await expect(page.getByText(folioNumber)).toBeVisible();

      // ── 2. Navegar a Datos Generales ─────────────────────────────────────
      await generalInfoPage.proceedFromCreatedPage();

      // ── 3. Capturar datos generales con suscriptor y agente válidos ───────
      await generalInfoPage.fillAllData(VALID_GENERAL_INFO);

      // ── 4. Guardar y navegar a Ubicaciones ───────────────────────────────
      await generalInfoPage.saveAndContinue();
      await locationsPage.waitForReady();

      // ── 5. Agregar ubicación calculable (CP válido + giro + coberturas) ──
      await locationsPage.addCompleteLocation(VALID_LOCATION);

      // Verificar que el botón "Continuar →" está habilitado
      await expect(page.getByRole('button', { name: /Continuar →/i })).toBeEnabled();

      // ── 6. Navegar a Opciones de Cobertura ───────────────────────────────
      await locationsPage.continueToTechnicalInfo();
      await technicalInfoPage.waitForReady();

      // ── 7. Guardar opciones de cobertura y navegar a Resultados ──────────
      await technicalInfoPage.saveAndContinue();
      await resultsPage.waitForReady();

      // ── 8. Ejecutar el cálculo ───────────────────────────────────────────
      await resultsPage.calculate();

      // ── 9. Verificar prima neta, prima comercial y desglose por ubicación ─
      await resultsPage.assertFinancialSummaryVisible();
      await resultsPage.assertLocationBreakdownVisible();
      await resultsPage.assertPremiumAmountsArePositive();

      // Verificar texto de sección de desglose
      await expect(page.getByText(/Desglose por ubicación/i)).toBeVisible();

      // Verificar que la ubicación creada aparece en el desglose
      await expect(
        page.getByText(VALID_LOCATION.locationName),
      ).toBeVisible();
    },
  );
});
