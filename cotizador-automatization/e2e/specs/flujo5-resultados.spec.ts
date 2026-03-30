import { test, expect } from '@playwright/test';
import { FolioHomePage } from '../pages/FolioHomePage';
import { GeneralInfoPage } from '../pages/GeneralInfoPage';
import { LocationsPage } from '../pages/LocationsPage';
import { TechnicalInfoPage } from '../pages/TechnicalInfoPage';
import { ResultsPage } from '../pages/ResultsPage';
import { VALID_GENERAL_INFO, VALID_LOCATION } from '../fixtures/test-data';

/**
 * Flujo 5 — Resultado de cálculo visible
 *
 * Dado un folio calculado (creado durante el test),
 * verifica que la página de resultados renderiza:
 *  - Prima Neta Total
 *  - Prima Comercial (sin IVA)
 *  - Prima Comercial Total
 *  - Tabla de desglose por ubicación
 */
test.describe('Flujo 5 — Resultados: prima neta, comercial y desglose por ubicación', () => {
  /**
   * Helper: crea un folio completo y lo lleva hasta la pantalla de resultados
   * con el cálculo ejecutado. Retorna el folioNumber para referencia.
   */
  async function setupCalculatedFolio(page: import('@playwright/test').Page): Promise<string> {
    const homePage = new FolioHomePage(page);
    const generalInfoPage = new GeneralInfoPage(page);
    const locationsPage = new LocationsPage(page);
    const technicalInfoPage = new TechnicalInfoPage(page);

    await homePage.goto();
    const folioNumber = await homePage.createNewFolio();

    await generalInfoPage.proceedFromCreatedPage();
    await generalInfoPage.fillAllData(VALID_GENERAL_INFO);
    await generalInfoPage.saveAndContinue();

    await locationsPage.waitForReady();
    await locationsPage.addCompleteLocation(VALID_LOCATION);
    await locationsPage.continueToTechnicalInfo();

    await technicalInfoPage.waitForReady();
    await technicalInfoPage.saveAndContinue();

    return folioNumber;
  }

  test(
    'prima neta, prima comercial y desglose por ubicación son visibles tras calcular',
    async ({ page }) => {
      const resultsPage = new ResultsPage(page);

      // ── Setup: folio con ubicación calculable, en pantalla de resultados ──
      const folioNumber = await setupCalculatedFolio(page);
      await resultsPage.waitForReady();

      // Verificar URL correcta
      await expect(page).toHaveURL(`/quotes/${folioNumber}/results`);

      // ── Ejecutar cálculo ─────────────────────────────────────────────────
      await resultsPage.calculate();

      // ── Sección 1: Prima Neta Total ──────────────────────────────────────
      const netCard = resultsPage.getNetPremiumCard();
      await expect(netCard).toBeVisible();
      await expect(netCard.getByText('Prima Neta Total')).toBeVisible();
      await expect(netCard.getByText('Solo riesgo puro')).toBeVisible();

      // ── Sección 2: Prima Comercial (sin IVA) ─────────────────────────────
      const commercialBeforeTaxCard = resultsPage.getCommercialPremiumBeforeTaxCard();
      await expect(commercialBeforeTaxCard).toBeVisible();
      await expect(commercialBeforeTaxCard.getByText('Prima Comercial (sin IVA)')).toBeVisible();
      await expect(commercialBeforeTaxCard.getByText('Incluye gastos de expedición')).toBeVisible();

      // ── Sección 3: Prima Comercial Total ─────────────────────────────────
      const commercialCard = resultsPage.getCommercialPremiumCard();
      await expect(commercialCard).toBeVisible();
      await expect(commercialCard.getByText('Prima Comercial Total')).toBeVisible();
      await expect(commercialCard.getByText('IVA incluido')).toBeVisible();
      await expect(commercialCard.getByText('Precio final')).toBeVisible();

      // Verificar que los montos son valores monetarios formateados
      await resultsPage.assertPremiumAmountsArePositive();

      // ── Tabla de desglose por ubicación ──────────────────────────────────
      await resultsPage.assertLocationBreakdownVisible();

      const breakdownTable = resultsPage.getLocationBreakdownTable();
      await expect(breakdownTable).toBeVisible();

      // Verificar encabezados de columna de la tabla
      await expect(breakdownTable.getByRole('columnheader', { name: 'Ubicación' })).toBeVisible();
      await expect(breakdownTable.getByRole('columnheader', { name: 'Prima Neta' })).toBeVisible();
      await expect(breakdownTable.getByRole('columnheader', { name: 'Estado' })).toBeVisible();

      // Verificar que la ubicación agregada aparece en el desglose
      await expect(breakdownTable.getByText(VALID_LOCATION.locationName)).toBeVisible();
    },
  );

  test(
    'pantalla de resultados muestra estado vacío con botón de cálculo antes de calcular',
    async ({ page }) => {
      const resultsPage = new ResultsPage(page);

      await setupCalculatedFolio(page);
      await resultsPage.waitForReady();

      // Antes de calcular: estado vacío con instrucción al usuario
      await expect(
        page.getByText('Ejecute el cálculo para ver los resultados de su cotización'),
      ).toBeVisible();

      // Botón de cálculo visible y habilitado
      const calcButton = page.getByRole('button', { name: 'Calcular cotización' });
      await expect(calcButton).toBeVisible();
      await expect(calcButton).toBeEnabled();

      // El resumen financiero NO debe estar visible aún
      await expect(resultsPage.getFinancialSummary()).not.toBeVisible();
    },
  );
});
