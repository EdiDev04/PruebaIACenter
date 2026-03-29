import { z } from 'zod';

const rfcRegex = /^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$/i;
const agentCodeRegex = /^AGT-\d{3}$/;
const subscriberCodeRegex = /^SUB-\d{3}$/;

export const insuredDataSchema = z.object({
  name: z
    .string()
    .min(1, 'El nombre del asegurado es obligatorio')
    .max(200, 'El nombre no puede exceder 200 caracteres'),
  taxId: z
    .string()
    .min(1, 'El RFC es obligatorio')
    .regex(rfcRegex, 'El RFC no tiene formato válido (ej: GIN850101AAA)'),
  email: z
    .string()
    .email('El correo electrónico no tiene formato válido')
    .optional()
    .or(z.literal('')),
  phone: z.string().optional().or(z.literal('')),
});

export const conductionDataSchema = z.object({
  subscriberCode: z
    .string()
    .min(1, 'El suscriptor es obligatorio')
    .regex(subscriberCodeRegex, 'Código de suscriptor inválido'),
  officeName: z.string().min(1, 'La oficina es obligatoria'),
  agentCode: z
    .string()
    .min(1, 'El código de agente es obligatorio')
    .regex(agentCodeRegex, 'Formato de agente inválido (ej: AGT-001)'),
});

export const businessClassSchema = z.object({
  businessType: z.enum(['commercial', 'industrial', 'residential'], {
    errorMap: () => ({ message: 'Seleccione un tipo de negocio' }),
  }),
  riskClassification: z.string().min(1, 'La clasificación de riesgo es obligatoria'),
});

export const generalInfoFormSchema = insuredDataSchema
  .merge(conductionDataSchema)
  .merge(businessClassSchema);

export type GeneralInfoFormData = z.infer<typeof generalInfoFormSchema>;
export type GeneralInfoPayload = GeneralInfoFormData & { version: number };
