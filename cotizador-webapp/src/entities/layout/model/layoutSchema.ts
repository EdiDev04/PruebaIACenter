import { z } from 'zod';
import { VALID_COLUMNS } from './types';

const DISPLAY_MODES = ['grid', 'list'] as const;

export const layoutConfigurationSchema = z.object({
  displayMode: z.enum(DISPLAY_MODES, {
    errorMap: () => ({
      message: 'Modo de visualización inválido. Valores permitidos: grid, list',
    }),
  }),
  visibleColumns: z
    .array(z.enum(VALID_COLUMNS))
    .min(1, 'Debe seleccionar al menos una columna visible'),
  version: z.number().int().positive(),
});

export const updateLayoutRequestSchema = z.object({
  displayMode: z.enum(DISPLAY_MODES, {
    errorMap: () => ({
      message: 'Modo de visualización inválido. Valores permitidos: grid, list',
    }),
  }),
  visibleColumns: z
    .array(z.enum(VALID_COLUMNS))
    .min(1, 'Debe seleccionar al menos una columna visible'),
  version: z.number().int().positive(),
});

export type LayoutConfiguration = z.infer<typeof layoutConfigurationSchema>;
export type UpdateLayoutRequestSchema = z.infer<typeof updateLayoutRequestSchema>;
