import { describe, it, expect } from 'vitest';
import { locationStep1Schema, locationFormSchema } from '@/entities/location';

/** Valid base object — override individual fields to test each rule in isolation */
const validBase = {
  locationName: 'Bodega Norte',
  address: 'Av. Industria 340',
};

describe('locationStep1Schema', () => {
  describe('locationName', () => {
    it('rejects empty locationName with the correct error message', () => {
      // Arrange & Act
      const result = locationStep1Schema.safeParse({ ...validBase, locationName: '' });

      // Assert
      expect(result.success).toBe(false);
      const err = result.error?.issues.find((i) => i.path.includes('locationName'));
      expect(err?.message).toBe('El nombre de la ubicacion es obligatorio');
    });

    it('accepts a valid locationName', () => {
      // Arrange & Act
      const result = locationStep1Schema.safeParse({ ...validBase, locationName: 'Sucursal Centro' });

      // Assert
      expect(result.success).toBe(true);
    });
  });

  describe('zipCode', () => {
    it('rejects a zip code with 3 digits', () => {
      // Arrange & Act
      const result = locationStep1Schema.safeParse({ ...validBase, zipCode: '123' });

      // Assert
      expect(result.success).toBe(false);
      const err = result.error?.issues.find((i) => i.path.includes('zipCode'));
      expect(err).toBeDefined();
    });

    it('rejects a zip code with 4 digits', () => {
      // Arrange & Act
      const result = locationStep1Schema.safeParse({ ...validBase, zipCode: '0660' });

      // Assert
      expect(result.success).toBe(false);
    });

    it('accepts a valid 5-digit zip code', () => {
      // Arrange & Act
      const result = locationStep1Schema.safeParse({ ...validBase, zipCode: '06600' });

      // Assert
      expect(result.success).toBe(true);
    });

    it('accepts empty string as zip code (field is optional)', () => {
      // Arrange & Act
      const result = locationStep1Schema.safeParse({ ...validBase, zipCode: '' });

      // Assert
      expect(result.success).toBe(true);
    });

    it('accepts undefined zip code (field is optional)', () => {
      // Arrange & Act
      const result = locationStep1Schema.safeParse({ ...validBase });

      // Assert
      expect(result.success).toBe(true);
    });
  });

  describe('constructionYear', () => {
    it('rejects constructionYear below minimum (1799)', () => {
      // Arrange & Act
      const result = locationStep1Schema.safeParse({ ...validBase, constructionYear: 1799 });

      // Assert
      expect(result.success).toBe(false);
      const err = result.error?.issues.find((i) => i.path.includes('constructionYear'));
      expect(err).toBeDefined();
    });

    it('rejects constructionYear above maximum (2027)', () => {
      // Arrange & Act
      const result = locationStep1Schema.safeParse({ ...validBase, constructionYear: 2027 });

      // Assert
      expect(result.success).toBe(false);
      const err = result.error?.issues.find((i) => i.path.includes('constructionYear'));
      expect(err).toBeDefined();
    });

    it('accepts constructionYear at lower boundary (1800)', () => {
      // Arrange & Act
      const result = locationStep1Schema.safeParse({ ...validBase, constructionYear: 1800 });

      // Assert
      expect(result.success).toBe(true);
    });

    it('accepts constructionYear at upper boundary (2026)', () => {
      // Arrange & Act
      const result = locationStep1Schema.safeParse({ ...validBase, constructionYear: 2026 });

      // Assert
      expect(result.success).toBe(true);
    });
  });
});

describe('locationFormSchema (full schema)', () => {
  it('parses successfully with all valid fields', () => {
    // Arrange
    const completeInput = {
      locationName: 'Bodega Norte',
      address: 'Av. Industria 340',
      zipCode: '06600',
      state: 'Ciudad de México',
      municipality: 'Cuauhtémoc',
      neighborhood: 'Doctores',
      city: 'Ciudad de México',
      catZone: 'A',
      constructionType: 'Tipo 1 - Macizo' as const,
      level: 2,
      constructionYear: 1998,
      businessLine: { description: 'Storage warehouse', fireKey: 'B-03' },
      guarantees: [{ guaranteeKey: 'building_fire', insuredAmount: 5000000 }],
    };

    // Act
    const result = locationFormSchema.safeParse(completeInput);

    // Assert
    expect(result.success).toBe(true);
  });

  it('rejects insuredAmount below 0', () => {
    // Arrange & Act
    const result = locationFormSchema.safeParse({
      ...validBase,
      guarantees: [{ guaranteeKey: 'building_fire', insuredAmount: -1 }],
    });

    // Assert
    expect(result.success).toBe(false);
    const err = result.error?.issues.find((i) => i.path.includes('insuredAmount'));
    expect(err).toBeDefined();
  });

  it('accepts insuredAmount of 0 (valid for non-required guarantees)', () => {
    // Arrange & Act
    const result = locationFormSchema.safeParse({
      ...validBase,
      guarantees: [{ guaranteeKey: 'glass', insuredAmount: 0 }],
    });

    // Assert
    expect(result.success).toBe(true);
  });
});
