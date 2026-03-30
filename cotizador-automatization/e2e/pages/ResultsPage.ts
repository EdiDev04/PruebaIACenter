import { type Page, expect } from '@playwright/test';

export class ResultsPage {
  constructor(private readonly page: Page) {}

  async waitForReady() {
    // saveAndContinue() ya esperó GET /state + rAF antes de resolver,
    // por lo que la versión fresca está en el store de React Query y el
    // botón ya tiene el onClick con la versión correcta.
    await expect(
      this.page.getByRole('button', { name: 'Calcular cotización' }),
    ).toBeVisible({ timeout: 10_000 });
  }

  async calculate() {
    const calcButton = this.page.getByRole('button', { name: 'Calcular cotización' });
    await expect(calcButton).toBeVisible({ timeout: 10_000 });

    const [response] = await Promise.all([
      this.page.waitForResponse(
        (resp) =>
          resp.url().includes('/v1/quotes') &&
          resp.url().includes('/calculate') &&
          resp.request().method() === 'POST',
        { timeout: 30_000 },
      ),
      calcButton.click(),
    ]);

    if (!response.ok()) {
      const body = await response.text().catch(() => '(no body)');
      throw new Error(`POST /calculate falló — status ${response.status()}: ${body}`);
    }

    await expect(
      this.page.getByRole('region', { name: 'Resumen financiero' }),
    ).toBeVisible({ timeout: 15_000 });
  }

  getFinancialSummary() {
    return this.page.getByRole('region', { name: 'Resumen financiero' });
  }

  getNetPremiumCard() {
    return this.page.getByRole('region', { name: /Prima Neta Total/i });
  }

  getCommercialPremiumCard() {
    return this.page.getByRole('region', { name: /Prima Comercial Total/i });
  }

  getCommercialPremiumBeforeTaxCard() {
    return this.page.getByRole('region', { name: /Prima Comercial sin IVA/i });
  }

  getLocationBreakdownTable() {
    return this.page.getByRole('table', { name: /Desglose por ubicación/i });
  }

  async assertFinancialSummaryVisible() {
    await expect(this.getFinancialSummary()).toBeVisible();
    await expect(this.getNetPremiumCard()).toBeVisible();
    await expect(this.getCommercialPremiumBeforeTaxCard()).toBeVisible();
    await expect(this.getCommercialPremiumCard()).toBeVisible();
  }

  async assertLocationBreakdownVisible() {
    await expect(this.getLocationBreakdownTable()).toBeVisible();
  }

  async assertPremiumAmountsArePositive() {
    // Formato es-CO: "$ 8.750,00" — punto como separador de miles, coma como decimal.
    const netCard = this.getNetPremiumCard();
    const amountText = await netCard.textContent();
    expect(amountText).toMatch(/\$\s?[\d.]+,\d{2}/);
  }
}
