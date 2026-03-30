import { z } from 'zod';

export const locationStep1Schema = z.object({
  locationName: z.string().min(1, 'El nombre de la ubicacion es obligatorio').max(200),
  address: z.string().min(1, 'La direccion es obligatoria').max(300),
  zipCode: z
    .string()
    .regex(/^\d{5}$/, 'El codigo postal debe ser de 5 digitos')
    .optional()
    .or(z.literal('')),
  state: z.string().optional(),
  municipality: z.string().optional(),
  neighborhood: z.string().optional(),
  city: z.string().optional(),
  catZone: z.string().optional(),
  constructionType: z
    .enum(['Tipo 1 - Macizo', 'Tipo 2 - Mixto', 'Tipo 3 - Ligero', 'Tipo 4 - Metalico'])
    .optional(),
  level: z.preprocess(
    (val) => (typeof val === 'number' && isNaN(val) ? undefined : val),
    z.number().int().min(0).optional(),
  ),
  constructionYear: z.preprocess(
    (val) => (typeof val === 'number' && isNaN(val) ? undefined : val),
    z.number().int().min(1800).max(2026).optional(),
  ),
});

export const guaranteeItemSchema = z.object({
  guaranteeKey: z.string(),
  insuredAmount: z.number().min(0, 'La suma asegurada debe ser mayor o igual a 0'),
});

export const locationStep2Schema = z.object({
  businessLine: z
    .object({
      code: z.string(),
      description: z.string(),
      fireKey: z.string().min(1),
      riskLevel: z.string(),
    })
    .optional(),
  guarantees: z.array(guaranteeItemSchema).optional(),
});

export const locationFormSchema = locationStep1Schema.merge(locationStep2Schema);

export type LocationStep1Values = z.infer<typeof locationStep1Schema>;
export type LocationStep2Values = z.infer<typeof locationStep2Schema>;
export type LocationFormValues = z.infer<typeof locationFormSchema>;
