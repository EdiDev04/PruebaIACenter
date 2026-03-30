import { type Page, expect } from '@playwright/test';

export class FolioHomePage {
  constructor(private readonly page: Page) {}

  async goto() {
    await this.page.goto('/');
    await expect(this.page.getByRole('heading', { name: 'Panel de Suscripción' })).toBeVisible();
  }

  async createNewFolio(): Promise<string> {
    // Inicia la escucha de red ANTES del click (evita race condition).
    // Si la API falla, el error incluye el status HTTP real en lugar de un timeout genérico.
    const [response] = await Promise.all([
      this.page.waitForResponse(
        (resp) =>
          resp.url().includes('/v1/folios') &&
          resp.request().method() === 'POST',
        { timeout: 20_000 },
      ),
      this.page.getByRole('button', { name: /Crear folio nuevo/i }).click(),
    ]);

    if (!response.ok()) {
      const body = await response.text().catch(() => '(no body)');
      throw new Error(
        `POST /v1/folios falló — status ${response.status()}: ${body}`,
      );
    }

    await this.page.waitForURL(/\/quotes\/.+\/created/, { timeout: 10_000 });
    const url = this.page.url();
    const match = url.match(/\/quotes\/([^/]+)\/created/);
    if (!match) throw new Error(`No se pudo extraer el número de folio de la URL: ${url}`);
    return match[1];
  }

  async openExistingFolio(folioNumber: string) {
    await this.page.getByLabel(/número de folio/i).fill(folioNumber);
    await this.page.getByRole('button', { name: /Buscar folio/i }).click();
    await this.page.waitForURL(`/quotes/${folioNumber}/general-info`);
  }
}
