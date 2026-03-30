export interface GeneralInfoData {
  insuredName: string;
  taxId: string;
  subscriberOptionText: string;
  agentCode: string;
  businessType: 'Comercial' | 'Industrial' | 'Residencial';
  riskClassification: string;
}

export interface LocationData {
  locationName: string;
  address: string;
  zipCode: string;
  constructionType: string;
  level: string;
  constructionYear: string;
  businessLineText: string;
  /** Suma asegurada para cada garantía pre-seleccionada que requiere monto (aria-label del input) */
  insuredAmounts?: Array<{ ariaLabel: string; amount: string }>;
}

export const VALID_GENERAL_INFO: GeneralInfoData = {
  insuredName: 'Grupo Industrial SA de CV',
  taxId: 'GIN850101AAA',
  subscriberOptionText: 'María González López (SUB-001)',
  agentCode: '001',
  businessType: 'Comercial',
  riskClassification: 'Riesgo estándar',
};

export const VALID_LOCATION: LocationData = {
  locationName: 'Bodega Central CDMX',
  address: 'Av. Insurgentes Sur 1234',
  zipCode: '06600',
  constructionType: 'Tipo 1 – Macizo',
  level: '1',
  constructionYear: '2005',
  businessLineText: 'Bodega de almacenamiento',
  // building_fire y contents_fire están pre-seleccionadas con insuredAmount: 0 por defecto
  insuredAmounts: [
    { ariaLabel: 'Suma asegurada para Incendio de edificio', amount: '5000000' },
    { ariaLabel: 'Suma asegurada para Incendio de contenido', amount: '2000000' },
  ],
};

export const ALTERNATE_GENERAL_INFO: GeneralInfoData = {
  insuredName: 'Comercializadora Beta SA de CV',
  taxId: 'CBT920315BBB',
  subscriberOptionText: 'Carlos Ramírez Torres (SUB-002)',
  agentCode: '002',
  businessType: 'Industrial',
  riskClassification: 'Riesgo preferente — perfil de riesgo bajo',
};
