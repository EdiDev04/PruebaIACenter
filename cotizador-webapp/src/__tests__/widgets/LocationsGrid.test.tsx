import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { LocationsGrid } from '@/widgets/locations-grid/ui/LocationsGrid';
import { useLocationsQuery } from '@/entities/location';
import type { LocationDto } from '@/entities/location';

// ── Mocks ──────────────────────────────────────────────────────────────────
vi.mock('@/entities/location', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/entities/location')>();
  return {
    ...actual,
    useLocationsQuery: vi.fn(),
  };
});

// ── Fixtures ────────────────────────────────────────────────────────────────
const locationA: LocationDto = {
  index: 0,
  locationName: 'Bodega Norte',
  address: 'Av. Industria 340',
  zipCode: '06600',
  state: 'Ciudad de México',
  municipality: 'Cuauhtémoc',
  neighborhood: 'Doctores',
  city: 'Ciudad de México',
  constructionType: 'Tipo 1 - Macizo',
  level: 1,
  constructionYear: 2000,
  locationBusinessLine: { code: 'BL-001', description: 'Storage warehouse', fireKey: 'B-03', riskLevel: 'medium' },
  guarantees: [],
  catZone: 'A',
  blockingAlerts: [],
  validationStatus: 'calculable',
};

const locationB: LocationDto = {
  ...locationA,
  index: 1,
  locationName: 'Oficina Sur',
  zipCode: '',
  locationBusinessLine: null,
  validationStatus: 'incomplete',
};

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return React.createElement(QueryClientProvider, { client: queryClient }, children);
  };
}

function renderGrid(props?: Partial<React.ComponentProps<typeof LocationsGrid>>) {
  return render(
    <LocationsGrid
      folio="DAN-2026-00001"
      onEdit={vi.fn()}
      onDelete={vi.fn()}
      {...props}
    />,
    { wrapper: createWrapper() },
  );
}

describe('LocationsGrid', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('shows loading spinner while fetching locations', () => {
    // Arrange
    vi.mocked(useLocationsQuery).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    } as ReturnType<typeof useLocationsQuery>);

    // Act
    renderGrid();

    // Assert
    expect(screen.getByText('Cargando ubicaciones...')).toBeInTheDocument();
  });

  it('shows error message when query fails', () => {
    // Arrange
    vi.mocked(useLocationsQuery).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
    } as ReturnType<typeof useLocationsQuery>);

    // Act
    renderGrid();

    // Assert
    expect(
      screen.getByText(/No fue posible cargar las ubicaciones/),
    ).toBeInTheDocument();
  });

  it('shows empty state message when there are no locations', () => {
    // Arrange
    vi.mocked(useLocationsQuery).mockReturnValue({
      data: { locations: [], version: 1 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useLocationsQuery>);

    // Act
    renderGrid();

    // Assert
    expect(screen.getByText(/No hay ubicaciones registradas/)).toBeInTheDocument();
  });

  it('renders a row for each location', () => {
    // Arrange
    vi.mocked(useLocationsQuery).mockReturnValue({
      data: { locations: [locationA, locationB], version: 2 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useLocationsQuery>);

    // Act
    renderGrid();

    // Assert — one row per location name
    expect(screen.getByText('Bodega Norte')).toBeInTheDocument();
    expect(screen.getByText('Oficina Sur')).toBeInTheDocument();
  });

  it('renders a table with the aria-label "Tabla de ubicaciones de riesgo"', () => {
    // Arrange
    vi.mocked(useLocationsQuery).mockReturnValue({
      data: { locations: [locationA], version: 1 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useLocationsQuery>);

    // Act
    renderGrid();

    // Assert
    expect(
      screen.getByRole('table', { name: 'Tabla de ubicaciones de riesgo' }),
    ).toBeInTheDocument();
  });

  it('calls onEdit with the correct location index when Editar is clicked', async () => {
    // Arrange
    const onEdit = vi.fn();
    vi.mocked(useLocationsQuery).mockReturnValue({
      data: { locations: [locationA], version: 1 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useLocationsQuery>);
    renderGrid({ onEdit });

    // Act — open kebab menu then click Editar
    await userEvent.click(
      screen.getByRole('button', { name: `Opciones para ${locationA.locationName}` }),
    );
    await userEvent.click(screen.getByRole('menuitem', { name: 'Editar' }));

    // Assert — grid maps location.index to the onEdit prop
    expect(onEdit).toHaveBeenCalledTimes(1);
    expect(onEdit).toHaveBeenCalledWith(locationA.index);
  });

  it('calls onDelete with the correct location index when Eliminar is clicked', async () => {
    // Arrange
    const onDelete = vi.fn();
    vi.mocked(useLocationsQuery).mockReturnValue({
      data: { locations: [locationB], version: 1 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useLocationsQuery>);
    renderGrid({ onDelete });

    // Act
    await userEvent.click(
      screen.getByRole('button', { name: `Opciones para ${locationB.locationName}` }),
    );
    await userEvent.click(screen.getByRole('menuitem', { name: 'Eliminar' }));

    // Assert
    expect(onDelete).toHaveBeenCalledTimes(1);
    expect(onDelete).toHaveBeenCalledWith(locationB.index);
  });

  it('renders calculable and incomplete badges for respective locations', () => {
    // Arrange
    vi.mocked(useLocationsQuery).mockReturnValue({
      data: { locations: [locationA, locationB], version: 2 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useLocationsQuery>);

    // Act
    renderGrid();

    // Assert
    expect(screen.getByText('Calculable')).toBeInTheDocument();
    expect(screen.getByText('Datos pendientes')).toBeInTheDocument();
  });

  it('uses the folio prop to call useLocationsQuery', () => {
    // Arrange
    vi.mocked(useLocationsQuery).mockReturnValue({
      data: { locations: [], version: 1 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useLocationsQuery>);

    // Act
    renderGrid({ folio: 'DAN-2026-00099' });

    // Assert
    expect(vi.mocked(useLocationsQuery)).toHaveBeenCalledWith('DAN-2026-00099');
  });
});
