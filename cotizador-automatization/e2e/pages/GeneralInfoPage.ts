import { type Page, expect } from '@playwright/test';
import type { GeneralInfoData } from '../fixtures/test-data';

export class GeneralInfoPage {
  constructor(private readonly page: Page) {}

  async waitForReady() {
    await expect(this.page.getByRole('heading', { name: 'Datos Generales' })).toBeVisible();
    await expect(this.page.getByText('Cargando datos...')).not.toBeVisible({ timeout: 10_000 });
  }

  async proceedFromCreatedPage() {
    await this.page.getByRole('button', { name: /Iniciar captura.*Datos Generales/i }).click();
    await this.page.waitForURL(/\/quotes\/.+\/general-info/);
    await this.waitForReady();
  }

  async fillInsuredData(data: Pick<GeneralInfoData, 'insuredName' | 'taxId'>) {
    await this.page.getByLabel(/Nombre del asegurado/i).fill(data.insuredName);
    await this.page.getByLabel(/RFC/i).fill(data.taxId);
  }

  async selectSubscriber(optionText: string) {
    // Abre el combobox de suscriptores y selecciona la opción por texto visible
    const comboboxInput = this.page.getByPlaceholder('Seleccionar suscriptor...');
    await comboboxInput.click();
    await comboboxInput.fill(optionText.split(' ')[0]); // filtra por primer nombre

    const option = this.page.getByRole('option', { name: optionText });
    await expect(option).toBeVisible({ timeout: 8_000 });
    await option.click();
  }

  async fillAgentCode(digits: string) {
    const rawDigits = digits.replace(/\D/g, '');
    const agentField = this.page.getByLabel(/Código de agente/i);
    await agentField.clear();
    await agentField.fill(rawDigits);
    await agentField.blur();
    // El campo formatea automáticamente a AGT-XXX al perder el foco
  }

  async selectBusinessType(businessType: GeneralInfoData['businessType']) {
    // El <input type="radio"> está visualmente oculto (1×1px clipped).
    // Se hace click en el <label> visible que lo envuelve.
    await this.page
      .locator('fieldset', { hasText: /Tipo de negocio/i })
      .locator('label', { hasText: new RegExp(businessType, 'i') })
      .click();
  }

  async selectRiskClassification(description: string) {
    const select = this.page.getByLabel(/Clasificación de riesgo/i);
    await select.selectOption({ label: description });
  }

  async fillAllData(data: GeneralInfoData) {
    await this.fillInsuredData(data);
    await this.selectSubscriber(data.subscriberOptionText);
    await this.fillAgentCode(data.agentCode);
    await this.selectBusinessType(data.businessType);
    await this.selectRiskClassification(data.riskClassification);
  }

  async saveAndContinue() {
    const [response] = await Promise.all([
      this.page.waitForResponse(
        (resp) =>
          resp.url().includes('/general-info') &&
          resp.request().method() === 'PUT',
        { timeout: 20_000 },
      ),
      this.page.getByRole('button', { name: /Guardar y continuar/i }).click(),
    ]);

    if (!response.ok()) {
      const body = await response.text().catch(() => '(no body)');
      throw new Error(
        `PUT general-info falló — status ${response.status()}: ${body}`,
      );
    }

    await this.page.waitForURL(/\/quotes\/.+\/locations/, { timeout: 10_000 });
  }

  async saveExpectingConflict() {
    await this.page.getByRole('button', { name: /Guardar y continuar/i }).click();
    await expect(
      this.page.getByRole('dialog', { name: 'Conflicto de versión' }),
    ).toBeVisible({ timeout: 10_000 });
  }

  isVersionConflictModalVisible() {
    return this.page.getByRole('dialog', { name: 'Conflicto de versión' });
  }

  getConflictMessage() {
    return this.page.getByText(
      'El folio fue modificado por otro proceso. Debes recargar los datos actualizados para continuar.',
    );
  }

  getReloadButton() {
    return this.page.getByRole('button', { name: 'Recargar datos' });
  }

  getCancelButton() {
    return this.page.getByRole('button', { name: 'Cancelar' });
  }
}
