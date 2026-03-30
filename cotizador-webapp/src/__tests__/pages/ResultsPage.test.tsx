import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import React from 'react';
import { ResultsPage } from '@/pages/ResultsPage';
import { useQuoteStateQuery } from '@/entities/quote-state';
import type { QuoteStateDto } from '@/entities/quote-state';

// ── Mocks ──────────────────────────────────────────────────────────────────
vi.mock('@/entities/quote-state', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/entities/quote-state')>();
  return {
    ...actual,
    useQuoteStateQuery: vi.fn(),
  };
});

vi.mock('@/features/calculate-quote', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/features/calculate-quote')>();
  return {
    ...actual,
    CalculateButton: ({ buttonLabel, onSuccess }: { folio: string; version: number; buttonLabel?: string; onSuccess?: () => void; onError?: () => void; variant?: string }) => (
      <button type="button" onClick={() => onSuccess?.()}>
        {buttonLabel ?? 'Calcular cotización'}
      </button>
    ),
  };
});

// ── Fixtures ────────────────────────────────────────────────────────────────
const baseState: QuoteStateDto = {
  folioNumber: 'DAN-2026-00001',
  quoteStatus: 'draft',
  version: 1,
  progress: {
    generalInfo: true,
    layoutConfiguration: true,
    locations: true,
    coverageOptions: true,
  },
  readyForCalculation: true,
  calculationResult: null,
  locations: {
    total: 2,
    calculable: 2,
    incomplete: 0,
    alerts: [],
  },
};

const calcResult: QuoteStateDto['calculationResult'] = {
  netPremium: 5000,
  commercialPremiumBeforeTax: 6000,
  commercialPremium: 6960,
  premiumsByLocation: [
    {
      locationIndex: 1,
      locationName: 'Bodega Norte',
      netPremium: 3000,
      validationStatus: 'calculable',
      coveragePremiums: [
        { guaranteeKey: 'building_fire', insuredAmount: 200000, rate: 0.002, premium: 400 },
      ],
    },
    {
      locationIndex: 2,
      locationName: 'Oficina Sur',
      netPremium: 2000,
      validationStatus: 'calculable',
      coveragePremiums: [
        { guaranteeKey: 'contents_fire', insuredAmount: 100000, rate: 0.002, premium: 200 },
      ],
    },
  ],
};

function mockQuery(overrides: Partial<ReturnType<typeof useQuoteStateQuery>>) {
  vi.mocked(useQuoteStateQuery).mockReturnValue({
    data: undefined,
    isLoading: false,
    error: null,
    ...overrides,
  } as ReturnType<typeof useQuoteStateQuery>);
}

function renderPage(folio = 'DAN-2026-00001') {
  return render(
    <MemoryRouter initialEntries={[`/quotes/${folio}/results`]}>
      <Routes>
        <Route path="/quotes/:folioNumber/results" element={<ResultsPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

afterEach(() => {
  vi.clearAllMocks();
});

// ── Tests ──────────────────────────────────────────────────────────────────
describe('ResultsPage', () => {
  describe('Estado "no calculado + ready"', () => {
    it('shows calculate button when not calculated and ready', () => {
      // Arrange
      mockQuery({ data: { ...baseState, readyForCalculation: true, calculationResult: null } });

      // Act
      renderPage();

      // Assert
      expect(screen.getByRole('button', { name: /Calcular/i })).toBeInTheDocument();
    });

    it('shows calculable locations count', () => {
      // Arrange
      mockQuery({
        data: {
          ...baseState,
          readyForCalculation: true,
          calculationResult: null,
          locations: { ...baseState.locations, calculable: 2 },
        },
      });

      // Act
      renderPage();

      // Assert
      expect(screen.getByText(/2/)).toBeInTheDocument();
    });
  });

  describe('Estado "no calculado + not ready"', () => {
    it('shows go to locations link when not ready', () => {
      // Arrange
      mockQuery({ data: { ...baseState, readyForCalculation: false, calculationResult: null } });

      // Act
      renderPage();

      // Assert
      expect(screen.getByRole('button', { name: /Ir a ubicaciones/i })).toBeInTheDocument();
    });

    it('does not show calculate button when not ready', () => {
      // Arrange
      mockQuery({ data: { ...baseState, readyForCalculation: false, calculationResult: null } });

      // Act
      renderPage();

      // Assert
      expect(
        screen.queryByRole('button', { name: /Calcular cotización/i }),
      ).not.toBeInTheDocument();
    });
  });

  describe('Estado "calculado"', () => {
    it('shows financial summary when calculated', () => {
      // Arrange
      mockQuery({ data: { ...baseState, calculationResult: calcResult } });

      // Act
      renderPage();

      // Assert
      expect(screen.getByRole('region', { name: 'Resumen financiero' })).toBeInTheDocument();
    });

    it('shows location breakdown when calculated', () => {
      // Arrange
      mockQuery({ data: { ...baseState, calculationResult: calcResult } });

      // Act
      renderPage();

      // Assert
      expect(screen.getByText('Bodega Norte')).toBeInTheDocument();
      expect(screen.getByText('Oficina Sur')).toBeInTheDocument();
    });

    it('shows incomplete alerts when there are incomplete locations', () => {
      // Arrange
      mockQuery({
        data: {
          ...baseState,
          calculationResult: calcResult,
          locations: {
            ...baseState.locations,
            incomplete: 1,
            alerts: [
              { index: 3, locationName: 'Depósito', missingFields: ['zipCode'] },
            ],
          },
        },
      });

      // Act
      renderPage();

      // Assert
      expect(screen.getByText('Depósito')).toBeInTheDocument();
    });

    it('shows recalculate button when calculated', () => {
      // Arrange
      mockQuery({ data: { ...baseState, calculationResult: calcResult } });

      // Act
      renderPage();

      // Assert
      expect(screen.getByRole('button', { name: /Recalcular/i })).toBeInTheDocument();
    });
  });

  describe('Loading / Error', () => {
    it('shows loading state', () => {
      // Arrange
      mockQuery({ isLoading: true, data: undefined, error: null });

      // Act
      renderPage();

      // Assert
      expect(screen.getByRole('generic', { hidden: true })).toBeTruthy();
      expect(document.querySelector('[aria-busy="true"]')).toBeInTheDocument();
    });

    it('shows error state when fetch fails', () => {
      // Arrange
      mockQuery({ isLoading: false, data: undefined, error: new Error('fail') });

      // Act
      renderPage();

      // Assert
      expect(screen.getByRole('alert')).toBeInTheDocument();
      expect(screen.getByText(/Error al cargar/i)).toBeInTheDocument();
    });
  });
});
