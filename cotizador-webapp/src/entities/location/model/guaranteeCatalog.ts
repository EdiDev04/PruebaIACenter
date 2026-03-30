export interface GuaranteeCatalogItem {
  guaranteeKey: string;
  label: string;
  requiresInsuredAmount: boolean;
  recommended?: boolean;
}

export interface GuaranteeGroup {
  groupKey: string;
  label: string;
  items: GuaranteeCatalogItem[];
}

export const GUARANTEE_GROUPS: GuaranteeGroup[] = [
  {
    groupKey: 'base',
    label: 'Coberturas base',
    items: [
      { guaranteeKey: 'building_fire', label: 'Incendio de edificio', requiresInsuredAmount: true, recommended: true },
      { guaranteeKey: 'contents_fire', label: 'Incendio de contenido', requiresInsuredAmount: true, recommended: true },
      { guaranteeKey: 'coverage_extension', label: 'Extensión de cobertura', requiresInsuredAmount: false },
      { guaranteeKey: 'glass', label: 'Cristales', requiresInsuredAmount: false },
      { guaranteeKey: 'illuminated_signs', label: 'Anuncios luminosos', requiresInsuredAmount: false },
    ],
  },
  {
    groupKey: 'cat',
    label: 'Catástrofes naturales',
    items: [
      { guaranteeKey: 'cat_tev', label: 'Terremoto y erupción volcánica', requiresInsuredAmount: true },
      { guaranteeKey: 'cat_fhm', label: 'Huracán y marea de tormenta', requiresInsuredAmount: true },
    ],
  },
  {
    groupKey: 'complementary',
    label: 'Complementarias',
    items: [
      { guaranteeKey: 'theft', label: 'Robo con violencia', requiresInsuredAmount: true },
      { guaranteeKey: 'electronic_equipment', label: 'Equipo electrónico', requiresInsuredAmount: true },
      { guaranteeKey: 'debris_removal', label: 'Remoción de escombros', requiresInsuredAmount: false },
      { guaranteeKey: 'extraordinary_expenses', label: 'Gastos extraordinarios', requiresInsuredAmount: false },
    ],
  },
  {
    groupKey: 'special',
    label: 'Especiales',
    items: [
      { guaranteeKey: 'business_interruption', label: 'Interrupción de negocio', requiresInsuredAmount: true },
      { guaranteeKey: 'rent_loss', label: 'Pérdida de rentas', requiresInsuredAmount: true },
      { guaranteeKey: 'cash_and_securities', label: 'Dinero y valores', requiresInsuredAmount: true },
    ],
  },
];

export const DEFAULT_SELECTED_GUARANTEES = ['building_fire', 'contents_fire'];
