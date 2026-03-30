import { z } from 'zod';

export const coverageOptionsFormSchema = z.object({
  enabledGuarantees: z
    .array(z.string())
    .min(1, 'Debe habilitar al menos una cobertura'),
  deductiblePercentage: z
    .number()
    .min(0, 'El deducible no puede ser negativo')
    .max(100, 'El deducible no puede superar 100%'),
  coinsurancePercentage: z
    .number()
    .min(0, 'El coaseguro no puede ser negativo')
    .max(100, 'El coaseguro no puede superar 100%'),
});

export type CoverageOptionsFormValues = z.infer<typeof coverageOptionsFormSchema>;
