import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { DeleteLocationModal } from '@/features/delete-location/ui/DeleteLocationModal';

describe('DeleteLocationModal', () => {
  it('renders the location name in the confirmation message', () => {
    // Arrange & Act
    render(
      <DeleteLocationModal
        locationName="Bodega Norte"
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    // Assert — the name appears in the message (surrounded by curly quotes &ldquo;…&rdquo;)
    expect(screen.getByText(/Bodega Norte/)).toBeInTheDocument();
  });

  it('renders the modal title "Eliminar ubicación"', () => {
    // Arrange & Act
    render(
      <DeleteLocationModal
        locationName="Bodega Norte"
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    // Assert
    expect(screen.getByText('Eliminar ubicación')).toBeInTheDocument();
  });

  it('calls onConfirm when the primary "Eliminar" button is clicked', async () => {
    // Arrange
    const onConfirm = vi.fn();
    render(
      <DeleteLocationModal
        locationName="Bodega Norte"
        onConfirm={onConfirm}
        onCancel={vi.fn()}
      />,
    );

    // Act
    await userEvent.click(screen.getByRole('button', { name: 'Eliminar' }));

    // Assert
    expect(onConfirm).toHaveBeenCalledTimes(1);
  });

  it('calls onCancel when the "Cancelar" button is clicked', async () => {
    // Arrange
    const onCancel = vi.fn();
    render(
      <DeleteLocationModal
        locationName="Bodega Norte"
        onConfirm={vi.fn()}
        onCancel={onCancel}
      />,
    );

    // Act
    await userEvent.click(screen.getByRole('button', { name: 'Cancelar' }));

    // Assert
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('calls onCancel when the close "X" button is clicked', async () => {
    // Arrange
    const onCancel = vi.fn();
    render(
      <DeleteLocationModal
        locationName="Bodega Norte"
        onConfirm={vi.fn()}
        onCancel={onCancel}
      />,
    );

    // Act
    await userEvent.click(screen.getByRole('button', { name: 'Cerrar modal' }));

    // Assert
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('shows the irreversibility warning message', () => {
    // Arrange & Act
    render(
      <DeleteLocationModal
        locationName="Sucursal Centro"
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    // Assert
    expect(screen.getByText(/Esta acción no se puede deshacer/)).toBeInTheDocument();
  });

  it('renders with a different location name correctly', () => {
    // Arrange & Act
    render(
      <DeleteLocationModal
        locationName="Oficina Sur"
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    // Assert
    expect(screen.getByText(/Oficina Sur/)).toBeInTheDocument();
  });
});
