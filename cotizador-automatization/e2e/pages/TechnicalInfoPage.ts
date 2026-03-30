import { type Page, expect } from '@playwright/test';

export class TechnicalInfoPage {
  constructor(private readonly page: Page) {}

  async waitForReady() {
    await expect(this.page.getByRole('heading', { name: 'Opciones de Cobertura' })).toBeVisible();
    await this.page.waitForLoadState('networkidle');
    // Espera a que react-hook-form haya llamado a reset() con los datos del backend.
    // CoverageOptionsForm usa el patrón hasReset: llama a reset() durante render,
    // DESPUÉS de que los requests HTTP terminan. Si hacemos click antes de que ese
    // ciclo de render termine, enabledGuarantees vale [] y Zod bloquea el submit.
    // Un checkbox checked en el DOM certifica que el store del formulario ya está poblado.
    await expect(
      this.page.getByRole('checkbox', { name: /Incendio Edificios/i }),
    ).toBeChecked({ timeout: 10_000 });
  }

  async saveAndContinue() {
    const [response] = await Promise.all([
      this.page.waitForResponse(
        (resp) =>
          resp.url().includes('/v1/quotes') &&
          resp.url().includes('/coverage-options') &&
          resp.request().method() === 'PUT',
        { timeout: 20_000 },
      ),
      this.page.getByRole('button', { name: /Guardar y continuar/i }).click(),
    ]);

    if (!response.ok()) {
      const body = await response.text().catch(() => '(no body)');
      throw new Error(`PUT /coverage-options falló — status ${response.status()}: ${body}`);
    }

    // staleTime:0 en useQuoteStateQuery hace que ResultsPage sirva datos del caché
    // (con la versión anterior) y dispare un GET /state en background al montar.
    // Registrar el listener ANTES de que waitForURL resuelva garantiza que lo capturamos
    // antes de que el componente monte y dispare el request.
    const stateRefetchPromise = this.page.waitForResponse(
      (resp) =>
        resp.url().includes('/v1/quotes') &&
        resp.url().includes('/state') &&
        resp.request().method() === 'GET',
      { timeout: 15_000 },
    );

    await this.page.waitForURL(/\/quotes\/.+\/results/, { timeout: 10_000 });
    await stateRefetchPromise;

    // Un tick de rAF para que el scheduler de React (MessageChannel) haya
    // procesado la actualización y re-renderizado CalculateButton con la versión fresca.
    await this.page.evaluate(() => new Promise<void>((r) => requestAnimationFrame(r)));
  }
}
