import { z } from 'zod';

export const createdFolioSchema = z.object({
  folioNumber: z.string().regex(/^DAN-\d{4}-\d{5}$/),
  quoteStatus: z.enum(['draft', 'in_progress', 'calculated', 'closed']),
  version: z.number().int().positive(),
  metadata: z.object({
    createdAt: z.string().datetime(),
    lastWizardStep: z.number().int().min(0),
  }),
});

export type CreatedFolio = z.infer<typeof createdFolioSchema>;
