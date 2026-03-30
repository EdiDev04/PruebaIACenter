import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { LocationRow } from '@/entities/location';
import type { LocationDto } from '@/entities/location';

const calculableLocation: LocationDto = {
  index: 0,
  locationName: 'Bodega Norte',
  address: 'Av. Industria 340',
  zipCode: '06600',
  state: 'Ciudad de México',
  municipality: 'Cuauhtémoc',
  neighborhood: 'Doctores',
  city: 'Ciudad de México',
  constructionType: 'Tipo 1 - Macizo',
  level: 2,
  constructionYear: 1998,
  locationBusinessLine: { code: 'BL-001', description: 'Storage warehouse', fireKey: 'B-03', riskLevel: 'medium' },
  guarantees: [{ guaranteeKey: 'building_fire', insuredAmount: 5000000 }],
  catZone: 'A',
  blockingAlerts: [],
  validationStatus: 'calculable',
};

const incompleteLocation: LocationDto = {
  ...calculableLocation,
  index: 1,
  locationName: 'Oficina Sur',
  zipCode: '',
  locationBusinessLine: null,
  validationStatus: 'incomplete',
};

function renderRow(
  location: LocationDto,
  onEdit = vi.fn(),
  onDelete = vi.fn(),
) {
  return render(
    <table>
      <tbody>
        <LocationRow location={location} onEdit={onEdit} onDelete={onDelete} />
      </tbody>
    </table>,
  );
}

describe('LocationRow', () => {
  it('shows location name', () => {
    // Arrange & Act
    renderRow(calculableLocation);

    // Assert
    expect(screen.getByText('Bodega Norte')).toBeInTheDocument();
  });

  it('shows zip code when present', () => {
    // Arrange & Act
    renderRow(calculableLocation);

    // Assert
    expect(screen.getByText('06600')).toBeInTheDocument();
  });

  it('shows "–" when zip code is empty', () => {
    // Arrange & Act
    renderRow(incompleteLocation);

    // Assert
    expect(screen.getByText('–')).toBeInTheDocument();
  });

  it('shows business line description when present', () => {
    // Arrange & Act
    renderRow(calculableLocation);

    // Assert
    expect(screen.getByText('Storage warehouse')).toBeInTheDocument();
  });

  it('shows index number', () => {
    // Arrange & Act
    renderRow(calculableLocation);

    // Assert
    expect(screen.getByText('0')).toBeInTheDocument();
  });

  it('shows "Calculable" badge for calculable validationStatus', () => {
    // Arrange & Act
    renderRow(calculableLocation);

    // Assert
    expect(screen.getByText('Calculable')).toBeInTheDocument();
  });

  it('shows "Datos pendientes" badge for incomplete validationStatus', () => {
    // Arrange & Act
    renderRow(incompleteLocation);

    // Assert
    expect(screen.getByText('Datos pendientes')).toBeInTheDocument();
  });

  it('calls onEdit with the location when Editar is clicked', async () => {
    // Arrange
    const onEdit = vi.fn();
    renderRow(calculableLocation, onEdit);

    // Act — open dropdown then click Editar
    await userEvent.click(
      screen.getByRole('button', { name: `Opciones para ${calculableLocation.locationName}` }),
    );
    await userEvent.click(screen.getByRole('menuitem', { name: 'Editar' }));

    // Assert
    expect(onEdit).toHaveBeenCalledTimes(1);
    expect(onEdit).toHaveBeenCalledWith(calculableLocation);
  });

  it('calls onDelete with the location when Eliminar is clicked', async () => {
    // Arrange
    const onDelete = vi.fn();
    renderRow(calculableLocation, vi.fn(), onDelete);

    // Act — open dropdown then click Eliminar
    await userEvent.click(
      screen.getByRole('button', { name: `Opciones para ${calculableLocation.locationName}` }),
    );
    await userEvent.click(screen.getByRole('menuitem', { name: 'Eliminar' }));

    // Assert
    expect(onDelete).toHaveBeenCalledTimes(1);
    expect(onDelete).toHaveBeenCalledWith(calculableLocation);
  });

  it('closes the dropdown after clicking Editar', async () => {
    // Arrange
    renderRow(calculableLocation);
    await userEvent.click(
      screen.getByRole('button', { name: `Opciones para ${calculableLocation.locationName}` }),
    );
    expect(screen.getByRole('menuitem', { name: 'Editar' })).toBeInTheDocument();

    // Act
    await userEvent.click(screen.getByRole('menuitem', { name: 'Editar' }));

    // Assert
    expect(screen.queryByRole('menuitem', { name: 'Editar' })).not.toBeInTheDocument();
  });
});
