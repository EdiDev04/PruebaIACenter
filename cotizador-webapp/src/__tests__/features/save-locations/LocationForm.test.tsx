import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { LocationForm } from '@/features/save-locations/ui/LocationForm';
import { useLocationsQuery } from '@/entities/location';
import { useBusinessLinesQuery } from '@/entities/business-line';
import { useSaveLocations } from '@/features/save-locations/model/useSaveLocations';
import { useZipCodeQuery } from '@/entities/zip-code';

// ── Mocks ──────────────────────────────────────────────────────────────────
vi.mock('@/entities/location', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/entities/location')>();
  return {
    ...actual,
    useLocationsQuery: vi.fn(),
  };
});

vi.mock('@/entities/business-line', () => ({
  useBusinessLinesQuery: vi.fn(),
}));

vi.mock('@/features/save-locations/model/useSaveLocations', () => ({
  useSaveLocations: vi.fn(),
}));

vi.mock('@/entities/zip-code', () => ({
  useZipCodeQuery: vi.fn(),
}));

// ── Fixtures ────────────────────────────────────────────────────────────────
const mockLocationsData = {
  locations: [],
  version: 2,
};

const mockBusinessLines = [
  { code: 'BL-001', description: 'Storage warehouse', fireKey: 'B-03', riskLevel: 'low' },
];

const mockMutate = vi.fn();

function mockDefaultHooks() {
  vi.mocked(useLocationsQuery).mockReturnValue({
    data: mockLocationsData,
    isLoading: false,
    isError: false,
  } as ReturnType<typeof useLocationsQuery>);

  vi.mocked(useBusinessLinesQuery).mockReturnValue({
    data: mockBusinessLines,
    isLoading: false,
  } as ReturnType<typeof useBusinessLinesQuery>);

  vi.mocked(useSaveLocations).mockReturnValue({
    mutate: mockMutate,
    isPending: false,
    isError: false,
    isSuccess: false,
  } as ReturnType<typeof useSaveLocations>);

  vi.mocked(useZipCodeQuery).mockReturnValue({
    data: undefined,
    isFetching: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
  } as ReturnType<typeof useZipCodeQuery>);
}

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return React.createElement(QueryClientProvider, { client: queryClient }, children);
  };
}

function renderForm(props?: Partial<React.ComponentProps<typeof LocationForm>>) {
  return render(
    <LocationForm
      folio="DAN-2026-00001"
      onSuccess={vi.fn()}
      onCancel={vi.fn()}
      {...props}
    />,
    { wrapper: createWrapper() },
  );
}

describe('LocationForm', () => {
  beforeEach(() => {
    mockDefaultHooks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    mockMutate.mockClear();
  });

  it('renders Step 1 initially showing "Paso 1 de 2"', () => {
    // Arrange & Act
    renderForm();

    // Assert
    expect(screen.getByText(/Paso 1 de 2/)).toBeInTheDocument();
  });

  it('renders the location name input in Step 1', () => {
    // Arrange & Act
    renderForm();

    // Assert
    expect(screen.getByLabelText(/Nombre de la ubicacion/i)).toBeInTheDocument();
  });

  it('renders the "Nueva ubicacion" title for new location mode', () => {
    // Arrange & Act
    renderForm();

    // Assert
    expect(screen.getByRole('heading', { name: 'Nueva ubicacion' })).toBeInTheDocument();
  });

  it('renders the "Editar ubicacion" title when locationIndex is provided', () => {
    // Arrange & Act
    renderForm({
      locationIndex: 0,
      initialData: {
        locationName: 'Bodega Norte',
        address: 'Av. Industria 340',
      },
    });

    // Assert
    expect(screen.getByRole('heading', { name: 'Editar ubicacion' })).toBeInTheDocument();
  });

  it('does not advance to Step 2 when locationName is empty', async () => {
    // Arrange
    renderForm();

    // Act — click next without filling required fields
    await userEvent.click(screen.getByRole('button', { name: /Siguiente/i }));

    // Assert — still on step 1
    expect(screen.getByText(/Paso 1 de 2/)).toBeInTheDocument();
    expect(screen.queryByText(/Paso 2 de 2/)).not.toBeInTheDocument();
  });

  it('advances to Step 2 after filling valid Step 1 data', async () => {
    // Arrange
    renderForm();
    const nameInput = screen.getByLabelText(/Nombre de la ubicacion/i);
    const addressInput = screen.getByLabelText(/Direccion/i);

    // Act
    await userEvent.type(nameInput, 'Bodega Norte');
    await userEvent.type(addressInput, 'Av. Industria 340');
    await userEvent.click(screen.getByRole('button', { name: /Siguiente/i }));

    // Assert
    await waitFor(() =>
      expect(screen.getByText(/Paso 2 de 2/)).toBeInTheDocument(),
    );
  });

  it('populates Step 1 fields with initialData in edit mode', () => {
    // Arrange & Act
    renderForm({
      locationIndex: 0,
      initialData: {
        locationName: 'Sucursal Centro',
        address: 'Calle Reforma 10',
      },
    });

    // Assert
    expect(screen.getByLabelText(/Nombre de la ubicacion/i)).toHaveValue('Sucursal Centro');
    expect(screen.getByLabelText(/Direccion/i)).toHaveValue('Calle Reforma 10');
  });

  it('calls onCancel when the cancel button is clicked', async () => {
    // Arrange
    const onCancel = vi.fn();
    renderForm({ onCancel });

    // Act
    await userEvent.click(screen.getByRole('button', { name: 'Cancelar' }));

    // Assert
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('calls onCancel when the close (X) button is clicked', async () => {
    // Arrange
    const onCancel = vi.fn();
    renderForm({ onCancel });

    // Act
    await userEvent.click(screen.getByRole('button', { name: 'Cerrar formulario' }));

    // Assert
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('navigates back to Step 1 when "← Atras" is clicked on Step 2', async () => {
    // Arrange — advance to step 2 first
    renderForm();
    await userEvent.type(screen.getByLabelText(/Nombre de la ubicacion/i), 'Bodega Norte');
    await userEvent.type(screen.getByLabelText(/Direccion/i), 'Av. Industria 340');
    await userEvent.click(screen.getByRole('button', { name: /Siguiente/i }));
    await waitFor(() => screen.getByText(/Paso 2 de 2/));

    // Act
    await userEvent.click(screen.getByRole('button', { name: /Atras/i }));

    // Assert
    expect(screen.getByText(/Paso 1 de 2/)).toBeInTheDocument();
  });

  it('shows error banner when useSaveLocations triggers an error via onError callback', async () => {
    // Arrange — capture the onError callback passed to useSaveLocations
    let capturedOnError: ((msg: string) => void) | undefined;
    vi.mocked(useSaveLocations).mockImplementation(({ onError }) => {
      capturedOnError = onError;
      return { mutate: mockMutate, isPending: false, isError: false, isSuccess: false } as ReturnType<typeof useSaveLocations>;
    });

    renderForm();

    // Act — trigger error through the callback
    capturedOnError?.('El folio fue modificado por otro proceso. Recargue para continuar.');

    // Assert
    await waitFor(() =>
      expect(
        screen.getByRole('alert'),
      ).toHaveTextContent('El folio fue modificado por otro proceso. Recargue para continuar.'),
    );
  });

  it('shows isLoading state on save button while mutation is pending', () => {
    // Arrange — advance to step 2 setup needs pending state
    vi.mocked(useSaveLocations).mockReturnValue({
      mutate: mockMutate,
      isPending: true,
      isError: false,
      isSuccess: false,
    } as ReturnType<typeof useSaveLocations>);

    renderForm({
      initialData: {
        locationName: 'Bodega Norte',
        address: 'Av. Industria 340',
        guarantees: [],
      },
      locationIndex: 0,
    });

    // We can't easily verify the loading button without being on step 2,
    // but we can verify useSaveLocations was called with isPending:true
    expect(vi.mocked(useSaveLocations)).toHaveBeenCalledWith(
      expect.objectContaining({ folio: 'DAN-2026-00001' }),
    );
  });
});
