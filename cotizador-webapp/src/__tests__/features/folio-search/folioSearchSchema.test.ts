import { describe, it, expect } from 'vitest';
import { folioSearchSchema } from '@/features/folio-search/model/folioSearchSchema';

describe('folioSearchSchema', () => {
  it('accepts a valid folio number (DAN-2026-00001)', () => {
    const result = folioSearchSchema.safeParse({ folioNumber: 'DAN-2026-00001' });
    expect(result.success).toBe(true);
  });

  it('accepts the oldest valid year format (DAN-2000-00001)', () => {
    const result = folioSearchSchema.safeParse({ folioNumber: 'DAN-2000-00001' });
    expect(result.success).toBe(true);
  });

  it('rejects an empty string with the "obligatorio" message', () => {
    const result = folioSearchSchema.safeParse({ folioNumber: '' });
    expect(result.success).toBe(false);
    expect(result.error?.issues[0].message).toBe('El número de folio es obligatorio');
  });

  it('rejects DAN-9999-XXXXX (non-numeric NNNNN) with a format error', () => {
    const result = folioSearchSchema.safeParse({ folioNumber: 'DAN-9999-XXXXX' });
    expect(result.success).toBe(false);
    expect(result.error?.issues[0].message).toMatch(/Formato inválido/);
  });

  it('rejects a completely invalid string with a format error', () => {
    const result = folioSearchSchema.safeParse({ folioNumber: 'INVALID' });
    expect(result.success).toBe(false);
    expect(result.error?.issues[0].message).toMatch(/Formato inválido/);
  });

  it('rejects a folio missing the numeric sequence part', () => {
    const result = folioSearchSchema.safeParse({ folioNumber: 'DAN-2026' });
    expect(result.success).toBe(false);
    expect(result.error?.issues[0].message).toMatch(/Formato inválido/);
  });
});
