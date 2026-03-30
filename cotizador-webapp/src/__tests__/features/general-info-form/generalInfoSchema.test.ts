import { describe, it, expect } from 'vitest';
import { generalInfoFormSchema } from '@/features/general-info-form/model/generalInfoSchema';

/** Valid base object — override individual fields to test each rule in isolation */
const validBase = {
  name: 'Empresa de Prueba S.A.',
  taxId: 'GIN850101AAA',
  email: '',
  phone: '',
  subscriberCode: 'SUB-001',
  officeName: 'Oficina Central',
  agentCode: 'AGT-001',
  businessType: 'commercial',
  riskClassification: 'ClaseA',
};

describe('generalInfoFormSchema', () => {
  describe('taxId (RFC)', () => {
    it('accepts a valid RFC (GIN850101AAA)', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, taxId: 'GIN850101AAA' });
      expect(result.success).toBe(true);
    });

    it('rejects an invalid RFC (123456)', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, taxId: '123456' });
      expect(result.success).toBe(false);
      const rfcError = result.error?.issues.find((i) => i.path.includes('taxId'));
      expect(rfcError?.message).toMatch(/RFC/i);
    });

    it('rejects an empty RFC', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, taxId: '' });
      expect(result.success).toBe(false);
      const rfcError = result.error?.issues.find((i) => i.path.includes('taxId'));
      expect(rfcError).toBeDefined();
    });
  });

  describe('email', () => {
    it('accepts empty string (email is optional)', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, email: '' });
      expect(result.success).toBe(true);
    });

    it('accepts a valid email address', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, email: 'usuario@empresa.com' });
      expect(result.success).toBe(true);
    });

    it('rejects an invalid email format', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, email: 'not-an-email' });
      expect(result.success).toBe(false);
      const emailError = result.error?.issues.find((i) => i.path.includes('email'));
      expect(emailError?.message).toMatch(/correo electrónico/i);
    });
  });

  describe('agentCode', () => {
    it('accepts AGT-001 format', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, agentCode: 'AGT-001' });
      expect(result.success).toBe(true);
    });

    it('accepts AGT-999 format', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, agentCode: 'AGT-999' });
      expect(result.success).toBe(true);
    });

    it('rejects INVALID agent code format', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, agentCode: 'INVALID' });
      expect(result.success).toBe(false);
      const err = result.error?.issues.find((i) => i.path.includes('agentCode'));
      expect(err?.message).toMatch(/agente/i);
    });

    it('rejects an agent code missing the prefix', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, agentCode: '001' });
      expect(result.success).toBe(false);
    });
  });

  describe('subscriberCode', () => {
    it('accepts SUB-001 format', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, subscriberCode: 'SUB-001' });
      expect(result.success).toBe(true);
    });

    it('rejects INVALID subscriber code format', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, subscriberCode: 'INVALID' });
      expect(result.success).toBe(false);
      const err = result.error?.issues.find((i) => i.path.includes('subscriberCode'));
      expect(err).toBeDefined();
    });
  });

  describe('businessType', () => {
    it.each(['commercial', 'industrial', 'residential'])(
      'accepts "%s" as a valid businessType',
      (businessType) => {
        const result = generalInfoFormSchema.safeParse({ ...validBase, businessType });
        expect(result.success).toBe(true);
      }
    );

    it('rejects an unknown businessType', () => {
      const result = generalInfoFormSchema.safeParse({ ...validBase, businessType: 'NUEVO' });
      expect(result.success).toBe(false);
      const err = result.error?.issues.find((i) => i.path.includes('businessType'));
      expect(err?.message).toMatch(/tipo de negocio/i);
    });
  });
});
