import { describe, it, expect, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import React from 'react';
import { LocationBreakdown } from '@/widgets/location-breakdown';
import type { LocationPremiumDto } from '@/entities/quote-state';

// ── Fixtures ────────────────────────────────────────────────────────────────
const makeLoc = (
  overrides: Partial<LocationPremiumDto> = {},
): LocationPremiumDto => ({
  locationIndex: 1,
  locationName: 'Bodega Norte',
  netPremium: 5000,
  validationStatus: 'calculable',
  coveragePremiums: [
    { guaranteeKey: 'building_fire', insuredAmount: 200000, rate: 0.002, premium: 400 },
  ],
  ...overrides,
});

afterEach(() => {});

describe('LocationBreakdown', () => {
  it('renders calculable locations', () => {
    // Arrange
    const locations: LocationPremiumDto[] = [
      makeLoc({ locationIndex: 1, locationName: 'Bodega Norte' }),
      makeLoc({ locationIndex: 2, locationName: 'Oficina Sur' }),
    ];

    // Act
    render(<LocationBreakdown premiumsByLocation={locations} />);

    // Assert
    expect(screen.getByText('Bodega Norte')).toBeInTheDocument();
    expect(screen.getByText('Oficina Sur')).toBeInTheDocument();
  });

  it('shows empty state when no calculable locations', () => {
    // Arrange
    const locations: LocationPremiumDto[] = [
      makeLoc({ locationIndex: 1, validationStatus: 'incomplete' }),
    ];

    // Act
    render(<LocationBreakdown premiumsByLocation={locations} />);

    // Assert
    expect(
      screen.getByText(/No hay ubicaciones calculables/i),
    ).toBeInTheDocument();
  });

  it('shows total net premium', () => {
    // Arrange
    const locations: LocationPremiumDto[] = [
      makeLoc({ locationIndex: 1, netPremium: 3000 }),
      makeLoc({ locationIndex: 2, netPremium: 2000 }),
    ];

    // Act
    render(<LocationBreakdown premiumsByLocation={locations} />);

    // Assert — total = 5 000 → formatted contains "5.000"
    expect(screen.getByText('Total')).toBeInTheDocument();
    expect(screen.getByRole('row', { name: '' }).closest('div')?.textContent ?? '').toBeTruthy();
    // Verify the formatted total appears somewhere in the doc
    expect(document.body.textContent).toContain('5.000');
  });

  it('expands coverage accordion on row click', async () => {
    // Arrange
    const user = userEvent.setup();
    const locations: LocationPremiumDto[] = [
      makeLoc({ locationIndex: 1, locationName: 'Bodega Norte' }),
      makeLoc({ locationIndex: 2, locationName: 'Oficina Sur' }),
    ];
    render(<LocationBreakdown premiumsByLocation={locations} />);

    // Initially collapsed
    expect(screen.queryByRole('table')).not.toBeInTheDocument();

    // Act
    await user.click(screen.getByRole('button', { name: /Bodega Norte/i }));

    // Assert
    expect(screen.getByRole('table', { name: /Desglose de coberturas/i })).toBeInTheDocument();
  });

  it('auto-expands when only one location', () => {
    // Arrange
    const locations: LocationPremiumDto[] = [
      makeLoc({ locationIndex: 1, locationName: 'Única' }),
    ];

    // Act
    render(<LocationBreakdown premiumsByLocation={locations} />);

    // Assert — accordion is already visible without any click
    expect(screen.getByRole('table', { name: /Desglose de coberturas/i })).toBeInTheDocument();
  });

  it('collapses accordion on second click', async () => {
    // Arrange
    const user = userEvent.setup();
    const locations: LocationPremiumDto[] = [
      makeLoc({ locationIndex: 1, locationName: 'Bodega Norte' }),
      makeLoc({ locationIndex: 2, locationName: 'Oficina Sur' }),
    ];
    render(<LocationBreakdown premiumsByLocation={locations} />);

    const btn = screen.getByRole('button', { name: /Bodega Norte/i });

    // Act — expand then collapse
    await user.click(btn);
    expect(screen.getByRole('table', { name: /Desglose de coberturas/i })).toBeInTheDocument();
    await user.click(btn);

    // Assert
    expect(screen.queryByRole('table', { name: /Desglose de coberturas/i })).not.toBeInTheDocument();
  });
});
