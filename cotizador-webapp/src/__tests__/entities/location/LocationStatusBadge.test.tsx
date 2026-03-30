import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { LocationStatusBadge } from '@/entities/location';

describe('LocationStatusBadge', () => {
  it('renders "Calculable" text for calculable status', () => {
    // Arrange & Act
    render(<LocationStatusBadge status="calculable" />);

    // Assert
    expect(screen.getByText('Calculable')).toBeInTheDocument();
  });

  it('applies the calculable CSS class for calculable status', () => {
    // Arrange & Act
    const { container } = render(<LocationStatusBadge status="calculable" />);

    // Assert — outer span contains the calculable class
    const badge = container.querySelector('[aria-label="Calculable"]');
    expect(badge).not.toBeNull();
    expect(badge?.className).toMatch(/calculable/);
  });

  it('renders "Datos pendientes" text for incomplete status', () => {
    // Arrange & Act
    render(<LocationStatusBadge status="incomplete" />);

    // Assert
    expect(screen.getByText('Datos pendientes')).toBeInTheDocument();
  });

  it('applies the incomplete CSS class for incomplete status', () => {
    // Arrange & Act
    const { container } = render(<LocationStatusBadge status="incomplete" />);

    // Assert
    const badge = container.querySelector('[aria-label="Datos pendientes"]');
    expect(badge).not.toBeNull();
    expect(badge?.className).toMatch(/incomplete/);
  });

  it('never applies incomplete class for calculable status', () => {
    // Arrange & Act
    const { container } = render(<LocationStatusBadge status="calculable" />);

    // Assert — no incomplete class present
    const badge = container.querySelector('[aria-label="Calculable"]');
    expect(badge?.className).not.toMatch(/incomplete/);
  });

  it('never applies calculable class for incomplete status', () => {
    // Arrange & Act
    const { container } = render(<LocationStatusBadge status="incomplete" />);

    // Assert — no calculable class on the outer badge span
    const badge = container.querySelector('[aria-label="Datos pendientes"]');
    expect(badge?.className).not.toMatch(/calculable/);
  });
});
