import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import React from 'react';
import { IncompleteAlerts } from '@/widgets/incomplete-alerts';
import type { LocationAlertDto } from '@/entities/quote-state';

// ── Fixtures ────────────────────────────────────────────────────────────────
const alert1: LocationAlertDto = {
  index: 1,
  locationName: 'Bodega Norte',
  missingFields: ['zipCode', 'constructionType'],
};

function renderAlerts(alerts: LocationAlertDto[], onEditLocation = vi.fn()) {
  return render(
    <IncompleteAlerts alerts={alerts} onEditLocation={onEditLocation} />,
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

  it('calls onEditLocation when edit button is clicked', async () => {
    // Arrange
    const user = userEvent.setup();
    const onEditLocation = vi.fn();
    renderAlerts([alert1], onEditLocation);

    // Act
    await user.click(screen.getByRole('button', { name: /Editar ubicación/i }));

    // Assert
    expect(onEditLocation).toHaveBeenCalledTimes(1);
  });
});
