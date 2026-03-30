import { describe, it, expect } from 'vitest';
import {
  GUARANTEE_GROUPS,
  DEFAULT_SELECTED_GUARANTEES,
} from '@/entities/location/model/guaranteeCatalog';

describe('GUARANTEE_GROUPS', () => {
  it('has exactly 4 groups', () => {
    expect(GUARANTEE_GROUPS).toHaveLength(4);
  });

  it('has exactly 14 guarantee items in total across all groups', () => {
    const total = GUARANTEE_GROUPS.reduce((sum, g) => sum + g.items.length, 0);
    expect(total).toBe(14);
  });

  it('first group is "Coberturas base" with groupKey "base"', () => {
    const baseGroup = GUARANTEE_GROUPS[0];
    expect(baseGroup.groupKey).toBe('base');
    expect(baseGroup.label).toBe('Coberturas base');
  });

  it('"Coberturas base" group has exactly 4 items', () => {
    const baseGroup = GUARANTEE_GROUPS.find((g) => g.groupKey === 'base');
    expect(baseGroup?.items).toHaveLength(4);
  });

  it('"Coberturas base" group includes building_fire, contents_fire, glass and illuminated_signs', () => {
    const baseGroup = GUARANTEE_GROUPS.find((g) => g.groupKey === 'base');
    const keys = baseGroup?.items.map((i) => i.guaranteeKey);
    expect(keys).toContain('building_fire');
    expect(keys).toContain('contents_fire');
    expect(keys).toContain('glass');
    expect(keys).toContain('illuminated_signs');
  });

  it('building_fire has requiresInsuredAmount: true', () => {
    const baseGroup = GUARANTEE_GROUPS.find((g) => g.groupKey === 'base');
    const item = baseGroup?.items.find((i) => i.guaranteeKey === 'building_fire');
    expect(item?.requiresInsuredAmount).toBe(true);
  });

  it('glass has requiresInsuredAmount: false', () => {
    const baseGroup = GUARANTEE_GROUPS.find((g) => g.groupKey === 'base');
    const item = baseGroup?.items.find((i) => i.guaranteeKey === 'glass');
    expect(item?.requiresInsuredAmount).toBe(false);
  });

  it('all 4 cat group items require insured amount', () => {
    const catGroup = GUARANTEE_GROUPS.find((g) => g.groupKey === 'cat');
    expect(catGroup?.items.every((i) => i.requiresInsuredAmount)).toBe(true);
  });

  it('has a "Catastrofes naturales" group with groupKey "cat"', () => {
    const catGroup = GUARANTEE_GROUPS.find((g) => g.groupKey === 'cat');
    expect(catGroup).toBeDefined();
    expect(catGroup?.label).toBe('Catastrofes naturales');
  });

  it('has a "Complementarias" group with groupKey "complementary"', () => {
    const group = GUARANTEE_GROUPS.find((g) => g.groupKey === 'complementary');
    expect(group).toBeDefined();
    expect(group?.label).toBe('Complementarias');
  });

  it('has a "Especiales" group with groupKey "special"', () => {
    const group = GUARANTEE_GROUPS.find((g) => g.groupKey === 'special');
    expect(group).toBeDefined();
    expect(group?.label).toBe('Especiales');
  });
});

describe('DEFAULT_SELECTED_GUARANTEES', () => {
  it('includes building_fire', () => {
    expect(DEFAULT_SELECTED_GUARANTEES).toContain('building_fire');
  });

  it('includes contents_fire', () => {
    expect(DEFAULT_SELECTED_GUARANTEES).toContain('contents_fire');
  });

  it('has exactly 2 default guarantees selected', () => {
    expect(DEFAULT_SELECTED_GUARANTEES).toHaveLength(2);
  });
});
