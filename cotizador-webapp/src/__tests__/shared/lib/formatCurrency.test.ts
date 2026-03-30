import { describe, it, expect } from 'vitest';
import { formatCurrency, formatRate } from '@/shared/lib/formatCurrency';

describe('formatCurrency', () => {
  it('formats a number as COP currency', () => {
    const result = formatCurrency(125430.5);
    expect(result).toContain('125.430');
    expect(result).toContain('50');
  });

  it('formats zero as $0,00', () => {
    const result = formatCurrency(0);
    expect(result).toContain('0');
  });

  it('formats large amounts correctly', () => {
    const result = formatCurrency(1000000);
    expect(result).toContain('1.000.000');
  });
});

describe('formatRate', () => {
  it('formats a rate as percentage', () => {
    const result = formatRate(0.00125);
    expect(result).toContain(',');
    expect(result).toContain('%');
  });

  it('returns flat rate indicator for zero rate', () => {
    const result = formatRate(0);
    expect(result).toBe('0,00%');
  });
});
