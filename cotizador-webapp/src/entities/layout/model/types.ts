export type DisplayMode = 'grid' | 'list';

export const VALID_COLUMNS = [
  'index',
  'locationName',
  'address',
  'zipCode',
  'state',
  'municipality',
  'neighborhood',
  'city',
  'constructionType',
  'level',
  'constructionYear',
  'businessLine',
  'guarantees',
  'catZone',
  'validationStatus',
] as const;

export type ColumnKey = (typeof VALID_COLUMNS)[number];

export interface LayoutConfigurationDto {
  displayMode: DisplayMode;
  visibleColumns: ColumnKey[];
  version: number;
}

export interface UpdateLayoutRequest {
  displayMode: DisplayMode;
  visibleColumns: ColumnKey[];
  version: number;
}
