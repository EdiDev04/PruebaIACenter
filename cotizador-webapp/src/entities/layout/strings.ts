import type { ColumnKey } from './model/types';

export const COLUMN_LABELS: Record<ColumnKey, string> = {
  index: '# (Número)',
  locationName: 'Nombre de ubicación',
  address: 'Dirección',
  zipCode: 'Código postal',
  state: 'Estado',
  municipality: 'Municipio',
  neighborhood: 'Colonia',
  city: 'Ciudad',
  constructionType: 'Tipo constructivo',
  level: 'Nivel',
  constructionYear: 'Año de construcción',
  businessLine: 'Giro comercial',
  guarantees: 'Coberturas',
  catZone: 'Zona catastrófica',
  validationStatus: 'Estado de validación',
};

export const COLUMN_GROUPS = [
  {
    label: 'Identificación',
    keys: ['index', 'locationName', 'address'] as ColumnKey[],
    defaultExpanded: true,
  },
  {
    label: 'Ubicación geográfica',
    keys: ['zipCode', 'state', 'municipality', 'neighborhood', 'city'] as ColumnKey[],
    defaultExpanded: false,
  },
  {
    label: 'Características del inmueble',
    keys: ['constructionType', 'level', 'constructionYear'] as ColumnKey[],
    defaultExpanded: false,
  },
  {
    label: 'Clasificación y estado',
    keys: ['businessLine', 'guarantees', 'catZone', 'validationStatus'] as ColumnKey[],
    defaultExpanded: true,
  },
] as const;

export const DISPLAY_MODE_LABELS = {
  grid: 'Grilla',
  list: 'Lista',
} as const;

export const PANEL_STRINGS = {
  title: 'Configurar vista',
  badgeDefault: 'Por defecto',
  badgeCustom: 'Personalizada',
  displayModeLabel: 'Modo de visualización',
  columnsLabel: 'Columnas visibles',
  loadingAria: 'Cargando configuración de vista',
  lastColumnTooltip: 'Debe haber al menos una columna visible',
} as const;
