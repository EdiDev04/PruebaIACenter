import { type Page, expect } from '@playwright/test';
import type { LocationData } from '../fixtures/test-data';

export class LocationsPage {
  constructor(private readonly page: Page) {}

  async waitForReady() {
    await expect(this.page.getByRole('heading', { name: 'Ubicaciones de riesgo' })).toBeVisible();
  }

  async openAddLocationForm() {
    await this.page.getByRole('button', { name: /Agregar ubicación/i }).click();
    await expect(this.page.getByRole('dialog', { name: 'Nueva ubicación' })).toBeVisible();
  }

  async fillStep1(data: LocationData) {
    // Nombre y dirección
    await this.page.getByLabel(/Nombre de la ubicación/i).fill(data.locationName);
    await this.page.getByLabel(/Dirección/i).fill(data.address);

    // Código postal — getByRole evita el strict mode violation con el div aria-label que también contiene "código postal"
    const zipInput = this.page.getByRole('textbox', { name: 'Código postal' });
    await zipInput.fill(data.zipCode);
    await expect(this.page.getByText(/Resuelto/i)).toBeVisible({ timeout: 10_000 });

    // Tipo constructivo (radio) — input visualmente oculto, clic en el label visible
    await this.page
      .locator('fieldset', { hasText: /Tipo constructivo/i })
      .locator('label', { hasText: new RegExp(data.constructionType, 'i') })
      .click();

    // Nivel y año de construcción
    await this.page.getByLabel(/^Nivel/i).fill(data.level);
    await this.page.getByLabel(/Año de construcción/i).fill(data.constructionYear);
  }

  async goToStep2() {
    await this.page.getByRole('button', { name: /Siguiente.*Coberturas/i }).click();
    await expect(this.page.getByText(/Paso 2 de 2/i)).toBeVisible();
  }

  async selectBusinessLine(description: string) {
    const businessLineInput = this.page.getByPlaceholder(/Buscar giro/i);
    await businessLineInput.fill(description.split(' ')[0]);
    const option = this.page.getByRole('option', { name: new RegExp(description, 'i') });
    await expect(option).toBeVisible({ timeout: 8_000 });
    await option.click();
  }

  async fillInsuredAmounts(insuredAmounts: Array<{ ariaLabel: string; amount: string }>) {
    // El grupo "Coberturas base" está abierto por defecto (defaultOpen=true)
    // Los inputs de suma asegurada usan aria-label="Suma asegurada para <nombre garantía>"
    for (const { ariaLabel, amount } of insuredAmounts) {
      const input = this.page.getByRole('spinbutton', { name: ariaLabel });
      await input.fill(amount);
      await input.blur();
    }
  }

  async saveLocation() {
    // Espera a que la red esté en reposo antes de guardar.
    // LocationForm tiene un guard `if (!locationsData) return` que cancela el submit
    // silenciosamente si el GET /locations aún no resolvió cuando se hace click.
    await this.page.waitForLoadState('networkidle');

    const [response] = await Promise.all([
      this.page.waitForResponse(
        (resp) =>
          resp.url().includes('/v1/quotes') &&
          resp.url().includes('/locations') &&
          resp.request().method() === 'PUT',
        { timeout: 20_000 },
      ),
      this.page.getByRole('button', { name: 'Guardar ubicación' }).click(),
    ]);

    if (!response.ok()) {
      const body = await response.text().catch(() => '(no body)');
      throw new Error(`PUT /locations falló — status ${response.status()}: ${body}`);
    }

    await expect(this.page.getByRole('dialog', { name: /ubicación/i })).not.toBeVisible({
      timeout: 10_000,
    });
    await expect(this.page.getByText(/Ubicación agregada correctamente/i)).toBeVisible();
  }

  async addCompleteLocation(data: LocationData) {
    await this.openAddLocationForm();
    await this.fillStep1(data);
    await this.goToStep2();
    await this.selectBusinessLine(data.businessLineText);
    if (data.insuredAmounts?.length) {
      await this.fillInsuredAmounts(data.insuredAmounts);
    }
    await this.saveLocation();
  }

  async continueToTechnicalInfo() {
    const continueBtn = this.page.getByRole('button', { name: /Continuar →/i });
    await expect(continueBtn).toBeEnabled({ timeout: 8_000 });
    await continueBtn.click();
    await this.page.waitForURL(/\/quotes\/.+\/technical-info/);
  }

  getLocationCount() {
    return this.page.locator('[aria-label*="ubicacion"], article').count();
  }
}
