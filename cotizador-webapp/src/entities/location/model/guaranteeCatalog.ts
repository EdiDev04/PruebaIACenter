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
      { guaranteeKey: 'glass', label: 'Cristales', requiresInsuredAmount: false },
      { guaranteeKey: 'illuminated_signs', label: 'Anuncios luminosos', requiresInsuredAmount: false },
    ],
  },
  {
    groupKey: 'cat',
    label: 'Catastrofes naturales',
    items: [
      { guaranteeKey: 'cat_tev', label: 'Terremoto y erupcion volcanica', requiresInsuredAmount: true },
      { guaranteeKey: 'cat_hm', label: 'Huracan y marea de tormenta', requiresInsuredAmount: true },
      { guaranteeKey: 'cat_hi', label: 'Inundacion', requiresInsuredAmount: true },
      { guaranteeKey: 'cat_other', label: 'Otras catastrofes', requiresInsuredAmount: true },
    ],
  },
  {
    groupKey: 'complementary',
    label: 'Complementarias',
    items: [
      { guaranteeKey: 'theft', label: 'Robo con violencia', requiresInsuredAmount: true },
      { guaranteeKey: 'machinery_breakdown', label: 'Maquinaria y equipo', requiresInsuredAmount: true },
      { guaranteeKey: 'electronic_equipment', label: 'Equipo electronico', requiresInsuredAmount: true },
    ],
  },
  {
    groupKey: 'special',
    label: 'Especiales',
    items: [
      { guaranteeKey: 'business_interruption', label: 'Interrupcion de negocio', requiresInsuredAmount: true },
      { guaranteeKey: 'civil_liability', label: 'Responsabilidad civil', requiresInsuredAmount: true },
      { guaranteeKey: 'cash', label: 'Dinero y valores', requiresInsuredAmount: true },
    ],
  },
];

export const DEFAULT_SELECTED_GUARANTEES = ['building_fire', 'contents_fire'];
