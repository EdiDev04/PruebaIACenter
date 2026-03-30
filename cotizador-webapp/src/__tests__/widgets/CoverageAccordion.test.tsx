import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { CoverageAccordion } from '@/widgets/location-breakdown/ui/CoverageAccordion';
import type { CoveragePremiumDto } from '@/entities/quote-state';

// ── Fixtures ────────────────────────────────────────────────────────────────
const makeCov = (overrides: Partial<CoveragePremiumDto> = {}): CoveragePremiumDto => ({
  guaranteeKey: 'building_fire',
  insuredAmount: 100000,
  rate: 0.002,
  premium: 200,
  ...overrides,
});

describe('CoverageAccordion', () => {
  it('renders coverage rows', () => {
    // Arrange
    const coveragePremiums: CoveragePremiumDto[] = [
      makeCov({ guaranteeKey: 'building_fire' }),
      makeCov({ guaranteeKey: 'contents_fire' }),
      makeCov({ guaranteeKey: 'theft' }),
    ];

    // Act
    render(<CoverageAccordion coveragePremiums={coveragePremiums} />);

    // Assert — 3 data rows (each row role)
    const rows = screen.getAllByRole('row');
    // header row + 3 data rows = 4
    expect(rows.length).toBe(4);
  });

  it('shows guarantee labels in Spanish', () => {
    // Arrange
    const coveragePremiums: CoveragePremiumDto[] = [
      makeCov({ guaranteeKey: 'building_fire' }),
    ];

    // Act
    render(<CoverageAccordion coveragePremiums={coveragePremiums} />);

    // Assert
    expect(screen.getByText('Incendio — Inmueble')).toBeInTheDocument();
  });

  it('shows flat rate indicator for glass', () => {
    // Arrange
    const coveragePremiums: CoveragePremiumDto[] = [
      makeCov({ guaranteeKey: 'glass', rate: 0 }),
    ];

    // Act
    render(<CoverageAccordion coveragePremiums={coveragePremiums} />);

    // Assert
    expect(screen.getByText('Tarifa plana')).toBeInTheDocument();
  });

  it('returns null for empty coveragePremiums', () => {
    // Arrange / Act
    const { container } = render(<CoverageAccordion coveragePremiums={[]} />);

    // Assert
    expect(container.firstChild).toBeNull();
  });

  it('formats insured amounts as currency', () => {
    // Arrange
    const coveragePremiums: CoveragePremiumDto[] = [
      makeCov({ insuredAmount: 500000 }),
    ];

    // Act
    render(<CoverageAccordion coveragePremiums={coveragePremiums} />);

    // Assert — COP format uses "." as thousands sep
    expect(screen.getByText(/500\.000/)).toBeInTheDocument();
  });
});
