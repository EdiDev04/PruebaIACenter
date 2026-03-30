import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';
import { IncompleteAlerts } from '@/widgets/incomplete-alerts';
import type { LocationAlertDto } from '@/entities/quote-state';

// ── Mock react-router-dom navigate ──────────────────────────────────────────
const mockNavigate = vi.fn();

vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

// ── Fixtures ────────────────────────────────────────────────────────────────
const alert1: LocationAlertDto = {
  index: 1,
  locationName: 'Bodega Norte',
  missingFields: ['zipCode', 'constructionType'],
};

function renderAlerts(alerts: LocationAlertDto[], folio = 'DAN-2026-00001') {
  return render(
    <MemoryRouter>
      <IncompleteAlerts alerts={alerts} folio={folio} />
    </MemoryRouter>,
  );
}

afterEach(() => {
  vi.clearAllMocks();
});

describe('IncompleteAlerts', () => {
  it('returns null when no alerts', () => {
    // Arrange / Act
    const { container } = renderAlerts([]);

    // Assert
    expect(container.firstChild).toBeNull();
  });

  it('renders alerts with location names', () => {
    // Arrange / Act
    renderAlerts([alert1]);

    // Assert
    expect(screen.getByText('Bodega Norte')).toBeInTheDocument();
  });

  it('shows missing fields as chips', () => {
    // Arrange / Act
    renderAlerts([alert1]);

    // Assert
    expect(screen.getByText('Código Postal')).toBeInTheDocument();
    expect(screen.getByText('Tipo de Construcción')).toBeInTheDocument();
  });

  it('navigates to locations on edit click', async () => {
    // Arrange
    const user = userEvent.setup();
    renderAlerts([alert1], 'DAN-2026-00001');

    // Act
    await user.click(screen.getByRole('button', { name: /Editar ubicación/i }));

    // Assert
    expect(mockNavigate).toHaveBeenCalledWith('/quotes/DAN-2026-00001/locations');
  });
});
