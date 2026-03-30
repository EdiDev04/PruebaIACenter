import { describe, it, expect } from 'vitest';
import { layoutConfigurationSchema, updateLayoutRequestSchema } from '@/entities/layout/model/layoutSchema';

/** Valid base object — override individual fields to test each rule in isolation */
const validBase = {
  displayMode: 'grid',
  visibleColumns: ['index', 'locationName'],
  version: 1,
};

describe('layoutConfigurationSchema', () => {
  describe('displayMode', () => {
    it('accepts "grid" as displayMode', () => {
      const result = layoutConfigurationSchema.safeParse({ ...validBase, displayMode: 'grid' });
      expect(result.success).toBe(true);
    });

    it('accepts "list" as displayMode', () => {
      const result = layoutConfigurationSchema.safeParse({ ...validBase, displayMode: 'list' });
      expect(result.success).toBe(true);
    });

    it('rejects an invalid displayMode value', () => {
      const result = layoutConfigurationSchema.safeParse({ ...validBase, displayMode: 'table' });
      expect(result.success).toBe(false);
      const err = result.error?.issues.find((i) => i.path.includes('displayMode'));
      expect(err?.message).toMatch(/inválido/i);
    });
  });

  describe('visibleColumns', () => {
    it('accepts an array of valid column keys', () => {
      const result = layoutConfigurationSchema.safeParse({
        ...validBase,
        visibleColumns: ['index', 'zipCode', 'businessLine'],
      });
      expect(result.success).toBe(true);
    });

    it('rejects an empty array', () => {
      const result = layoutConfigurationSchema.safeParse({ ...validBase, visibleColumns: [] });
      expect(result.success).toBe(false);
      const err = result.error?.issues.find((i) => i.path.includes('visibleColumns'));
      expect(err?.message).toMatch(/al menos una columna/i);
    });

    it('rejects a column key not included in VALID_COLUMNS', () => {
      const result = layoutConfigurationSchema.safeParse({
        ...validBase,
        visibleColumns: ['index', 'invalidColumn'],
      });
      expect(result.success).toBe(false);
    });
  });

  describe('version', () => {
    it('accepts version greater than 0', () => {
      const result = layoutConfigurationSchema.safeParse({ ...validBase, version: 5 });
      expect(result.success).toBe(true);
    });

    it('rejects version equal to 0', () => {
      const result = layoutConfigurationSchema.safeParse({ ...validBase, version: 0 });
      expect(result.success).toBe(false);
    });

    it('rejects a negative version', () => {
      const result = layoutConfigurationSchema.safeParse({ ...validBase, version: -1 });
      expect(result.success).toBe(false);
    });
  });
});

describe('updateLayoutRequestSchema', () => {
  const validRequest = {
    displayMode: 'list',
    visibleColumns: ['locationName', 'zipCode'],
    version: 2,
  };

  it('accepts a valid payload with list displayMode', () => {
    const result = updateLayoutRequestSchema.safeParse(validRequest);
    expect(result.success).toBe(true);
  });

  it('rejects an unrecognized displayMode', () => {
    const result = updateLayoutRequestSchema.safeParse({ ...validRequest, displayMode: 'compact' });
    expect(result.success).toBe(false);
    const err = result.error?.issues.find((i) => i.path.includes('displayMode'));
    expect(err).toBeDefined();
  });

  it('rejects empty visibleColumns array', () => {
    const result = updateLayoutRequestSchema.safeParse({ ...validRequest, visibleColumns: [] });
    expect(result.success).toBe(false);
  });

  it('requires version to be a positive integer greater than 0', () => {
    const result = updateLayoutRequestSchema.safeParse({ ...validRequest, version: 0 });
    expect(result.success).toBe(false);
  });

  it('rejects a non-integer version', () => {
    const result = updateLayoutRequestSchema.safeParse({ ...validRequest, version: 1.5 });
    expect(result.success).toBe(false);
  });
});
