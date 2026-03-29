import { z } from 'zod';

export const folioSearchSchema = z.object({
  folioNumber: z
    .string()
    .min(1, 'El número de folio es obligatorio')
    .regex(/^DAN-\d{4}-\d{5}$/, 'Formato inválido. Use DAN-YYYY-NNNNN (ej: DAN-2026-00001)'),
});

export type FolioSearchFormData = z.infer<typeof folioSearchSchema>;
