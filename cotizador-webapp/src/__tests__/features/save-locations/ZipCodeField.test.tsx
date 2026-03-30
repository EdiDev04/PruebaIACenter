import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ZipCodeField } from '@/features/save-locations/ui/ZipCodeField';
import type { ZipCodeDto } from '@/entities/zip-code';
import { useZipCodeQuery } from '@/entities/zip-code';

// Mock the zip-code entity so we can control what the query returns
vi.mock('@/entities/zip-code', () => ({
  useZipCodeQuery: vi.fn(),
}));

const mockZipCodeData: ZipCodeDto = {
  zipCode: '06600',
  state: 'Ciudad de México',
  municipality: 'Cuauhtémoc',
  neighborhood: 'Doctores',
  city: 'Ciudad de México',
  catZone: 'A',
  technicalLevel: 1,
};

function defaultQueryState(overrides: object = {}) {
  return {
    data: undefined,
    isFetching: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
    ...overrides,
  };
}

function renderField(props: {
  value?: string;
  onChange?: (v: string) => void;
  onResolved?: (d: ZipCodeDto) => void;
  onCleared?: () => void;
  error?: string;
}) {
  return render(
    <ZipCodeField
      value={props.value ?? ''}
      onChange={props.onChange ?? vi.fn()}
      onResolved={props.onResolved ?? vi.fn()}
      onCleared={props.onCleared ?? vi.fn()}
      error={props.error}
    />,
  );
}

describe('ZipCodeField', () => {
  beforeEach(() => {
    vi.mocked(useZipCodeQuery).mockReturnValue(defaultQueryState() as ReturnType<typeof useZipCodeQuery>);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders the zip code input with empty value initially', () => {
    // Arrange & Act
    renderField({});

    // Assert
    const input = screen.getByPlaceholderText('00000');
    expect(input).toBeInTheDocument();
    expect(input).toHaveValue('');
  });

  it('shows "Resolviendo..." spinner when isFetching is true', () => {
    // Arrange
    vi.mocked(useZipCodeQuery).mockReturnValue(
      defaultQueryState({ isFetching: true }) as ReturnType<typeof useZipCodeQuery>,
    );

    // Act
    renderField({ value: '06600' });

    // Assert
    expect(screen.getByText('Resolviendo...')).toBeInTheDocument();
  });

  it('shows "Resuelto ✓" when query returned data and is not fetching', () => {
    // Arrange
    vi.mocked(useZipCodeQuery).mockReturnValue(
      defaultQueryState({ data: mockZipCodeData, isFetching: false }) as ReturnType<typeof useZipCodeQuery>,
    );

    // Act
    renderField({ value: '06600' });

    // Assert
    expect(screen.getByText('Resuelto ✓')).toBeInTheDocument();
  });

  it('shows amber not-found message when error type is zipCodeNotFound', () => {
    // Arrange
    vi.mocked(useZipCodeQuery).mockReturnValue(
      defaultQueryState({
        isError: true,
        error: { type: 'zipCodeNotFound', message: 'CP no encontrado' },
      }) as ReturnType<typeof useZipCodeQuery>,
    );

    // Act
    renderField({ value: '99999' });

    // Assert
    expect(
      screen.getByText(/Código postal no encontrado/),
    ).toBeInTheDocument();
  });

  it('shows "Reintentar" button when error type is coreOhsUnavailable', () => {
    // Arrange
    vi.mocked(useZipCodeQuery).mockReturnValue(
      defaultQueryState({
        isError: true,
        error: { type: 'coreOhsUnavailable', message: 'Servicio no disponible' },
      }) as ReturnType<typeof useZipCodeQuery>,
    );

    // Act
    renderField({ value: '06600' });

    // Assert
    expect(
      screen.getByRole('button', { name: /Reintentar consulta de codigo postal/i }),
    ).toBeInTheDocument();
  });

  it('calls refetch when "Reintentar" button is clicked', async () => {
    // Arrange
    const refetch = vi.fn();
    vi.mocked(useZipCodeQuery).mockReturnValue(
      defaultQueryState({
        isError: true,
        error: { type: 'coreOhsUnavailable', message: 'Servicio no disponible' },
        refetch,
      }) as ReturnType<typeof useZipCodeQuery>,
    );
    renderField({ value: '06600' });

    // Act
    await userEvent.click(
      screen.getByRole('button', { name: /Reintentar consulta de codigo postal/i }),
    );

    // Assert
    expect(refetch).toHaveBeenCalledTimes(1);
  });

  it('filters non-digit characters and calls onChange with only digits', async () => {
    // Arrange
    const onChange = vi.fn();
    renderField({ onChange });

    // Act
    const input = screen.getByPlaceholderText('00000');
    await userEvent.type(input, 'a');

    // Assert — the filter replaces non-digits with empty string
    expect(onChange).toHaveBeenLastCalledWith('');
  });

  it('calls onChange with digits when user types numeric characters', async () => {
    // Arrange
    const onChange = vi.fn();
    renderField({ onChange });

    // Act
    const input = screen.getByPlaceholderText('00000');
    await userEvent.type(input, '1');

    // Assert
    expect(onChange).toHaveBeenLastCalledWith('1');
  });

  it('shows field-level error message passed via error prop', () => {
    // Arrange & Act
    renderField({ error: 'El codigo postal debe ser de 5 digitos' });

    // Assert
    expect(screen.getByText('El codigo postal debe ser de 5 digitos')).toBeInTheDocument();
  });
});
