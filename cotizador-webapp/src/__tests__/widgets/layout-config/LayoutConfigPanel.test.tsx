import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { LayoutConfigPanel } from '@/widgets/layout-config/ui/LayoutConfigPanel';
import * as layoutApi from '@/entities/layout/api/layoutApi';
import type { ColumnKey, DisplayMode } from '@/entities/layout';
import { SAVE_LAYOUT_STRINGS } from '@/features/save-layout';

vi.mock('@/entities/layout/api/layoutApi');

/** Default visible columns — mirrors the component's DEFAULT_VISIBLE_COLUMNS constant */
const DEFAULT_VISIBLE_COLUMNS: ColumnKey[] = [
  'index',
  'locationName',
  'zipCode',
  'businessLine',
  'validationStatus',
];

const defaultServerData: layoutApi.LayoutResponse = {
  data: {
    displayMode: 'grid' as DisplayMode,
    visibleColumns: DEFAULT_VISIBLE_COLUMNS,
    version: 1,
  },
};

function renderPanel(props: { folio: string; onClose?: () => void }) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <LayoutConfigPanel {...props} />
    </QueryClientProvider>,
  );
}

describe('LayoutConfigPanel', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renderiza skeleton mientras carga', () => {
    // Arrange — never-resolving promise keeps the component in isLoading state
    vi.mocked(layoutApi.getLayout).mockReturnValue(new Promise(() => {}));

    // Act
    renderPanel({ folio: 'DAN-2026-00001' });

    // Assert
    const skeleton = screen.getByLabelText('Cargando configuración de vista');
    expect(skeleton).toHaveAttribute('aria-busy', 'true');
  });

  it('renderiza el panel principal con datos del servidor', async () => {
    // Arrange
    vi.mocked(layoutApi.getLayout).mockResolvedValue(defaultServerData);

    // Act
    renderPanel({ folio: 'DAN-2026-00001' });

    // Assert — panel dialog is visible with server data loaded
    await waitFor(() =>
      expect(screen.getByRole('dialog', { name: 'Configurar vista' })).toBeInTheDocument(),
    );
    // DisplayMode reflects server value (grid = 'Grilla' selected)
    expect(screen.getByRole('radio', { name: 'Grilla' })).toHaveAttribute('aria-checked', 'true');
    // Column counter shows 5 visible columns
    expect(screen.getByText('(5 de 15)')).toBeInTheDocument();
  });

  it('toggle entre modo grid y list actualiza el estado local del formulario', async () => {
    // Arrange
    vi.mocked(layoutApi.getLayout).mockResolvedValue(defaultServerData);
    renderPanel({ folio: 'DAN-2026-00001' });
    await waitFor(() => screen.getByRole('dialog', { name: 'Configurar vista' }));

    // Initial state — grid is selected
    expect(screen.getByRole('radio', { name: 'Grilla' })).toHaveAttribute('aria-checked', 'true');
    expect(screen.getByRole('radio', { name: 'Lista' })).toHaveAttribute('aria-checked', 'false');

    // Act — switch to list
    await userEvent.click(screen.getByRole('radio', { name: 'Lista' }));

    // Assert — list is now selected
    expect(screen.getByRole('radio', { name: 'Lista' })).toHaveAttribute('aria-checked', 'true');
    expect(screen.getByRole('radio', { name: 'Grilla' })).toHaveAttribute('aria-checked', 'false');
  });

  it('seleccionar/deseleccionar columna actualiza visibleColumns en el formulario', async () => {
    // Arrange — "Identificación" group is defaultExpanded, so its checkboxes are immediately visible
    vi.mocked(layoutApi.getLayout).mockResolvedValue(defaultServerData);
    renderPanel({ folio: 'DAN-2026-00001' });
    await waitFor(() => screen.getByRole('dialog', { name: 'Configurar vista' }));
    expect(screen.getByText('(5 de 15)')).toBeInTheDocument();

    // Act — select 'address' (Dirección — unchecked in default selection)
    const addressCheckbox = screen.getByRole('checkbox', { name: 'Dirección' });
    expect(addressCheckbox).not.toBeChecked();
    await userEvent.click(addressCheckbox);

    // Assert — column count increased to 6
    expect(screen.getByText('(6 de 15)')).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'Dirección' })).toBeChecked();

    // Act — deselect 'locationName' (Nombre de ubicación — checked in default selection)
    await userEvent.click(screen.getByRole('checkbox', { name: 'Nombre de ubicación' }));

    // Assert — column count back to 5
    expect(screen.getByText('(5 de 15)')).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'Nombre de ubicación' })).not.toBeChecked();
  });

  it('el último checkbox no puede desmarcarse (protección del mínimo 1 columna)', async () => {
    // Arrange — only one visible column so it must be protected
    vi.mocked(layoutApi.getLayout).mockResolvedValue({
      data: { displayMode: 'grid', visibleColumns: ['index'], version: 1 },
    });
    renderPanel({ folio: 'DAN-2026-00001' });
    await waitFor(() => screen.getByRole('dialog', { name: 'Configurar vista' }));

    // Assert — '# (Número)' is checked and disabled because it is the last column
    const indexCheckbox = screen.getByRole('checkbox', { name: '# (Número)' });
    expect(indexCheckbox).toBeChecked();
    expect(indexCheckbox).toBeDisabled();
  });

  it('botón "Guardar configuración" llama a updateLayout con los valores correctos', async () => {
    // Arrange
    vi.mocked(layoutApi.getLayout).mockResolvedValue(defaultServerData);
    vi.mocked(layoutApi.updateLayout).mockResolvedValue(defaultServerData);
    renderPanel({ folio: 'DAN-2026-00001' });
    await waitFor(() => screen.getByRole('dialog', { name: 'Configurar vista' }));

    // Make the form dirty by changing the display mode (grid → list)
    await userEvent.click(screen.getByRole('radio', { name: 'Lista' }));

    // Act — submit the form
    const saveButton = screen.getByRole('button', { name: 'Guardar configuración' });
    expect(saveButton).not.toBeDisabled();
    await userEvent.click(saveButton);

    // Assert — updateLayout called with updated displayMode and original server columns/version
    await waitFor(() =>
      expect(layoutApi.updateLayout).toHaveBeenCalledWith('DAN-2026-00001', {
        displayMode: 'list',
        visibleColumns: DEFAULT_VISIBLE_COLUMNS,
        version: 1,
      }),
    );
  });

  it('botón "Restaurar predeterminados" resetea displayMode a "grid"', async () => {
    // Arrange — load non-default layout (list mode)
    vi.mocked(layoutApi.getLayout).mockResolvedValue({
      data: { displayMode: 'list', visibleColumns: DEFAULT_VISIBLE_COLUMNS, version: 3 },
    });
    renderPanel({ folio: 'DAN-2026-00001' });
    await waitFor(() => screen.getByRole('dialog', { name: 'Configurar vista' }));

    // Precondition — "Lista" is selected and "Restaurar predeterminados" is enabled
    expect(screen.getByRole('radio', { name: 'Lista' })).toHaveAttribute('aria-checked', 'true');
    const restoreButton = screen.getByRole('button', { name: 'Restaurar predeterminados' });
    expect(restoreButton).not.toBeDisabled();

    // Act
    await userEvent.click(restoreButton);

    // Assert — display mode reset to default "grid"
    expect(screen.getByRole('radio', { name: 'Grilla' })).toHaveAttribute('aria-checked', 'true');
  });

  it('alerta ámbar se muestra cuando la mutación falla con versionConflict', async () => {
    // Arrange
    vi.mocked(layoutApi.getLayout).mockResolvedValue(defaultServerData);
    vi.mocked(layoutApi.updateLayout).mockRejectedValue({
      type: 'versionConflict',
      message: 'Conflicto de versión',
      field: null,
    });
    renderPanel({ folio: 'DAN-2026-00001' });
    await waitFor(() => screen.getByRole('dialog', { name: 'Configurar vista' }));

    // Make form dirty and submit
    await userEvent.click(screen.getByRole('radio', { name: 'Lista' }));
    await userEvent.click(screen.getByRole('button', { name: 'Guardar configuración' }));

    // Assert — conflict alert appears with the correct message
    await waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent(SAVE_LAYOUT_STRINGS.conflictError),
    );
  });

  it('botón "Recargar" en alerta 409 vuelve a ejecutar la query', async () => {
    // Arrange — first load succeeds, save fails with conflict
    vi.mocked(layoutApi.getLayout).mockResolvedValue(defaultServerData);
    vi.mocked(layoutApi.updateLayout).mockRejectedValue({
      type: 'versionConflict',
      message: 'Conflicto de versión',
      field: null,
    });
    renderPanel({ folio: 'DAN-2026-00001' });
    await waitFor(() => screen.getByRole('dialog', { name: 'Configurar vista' }));

    // Generate the conflict alert
    await userEvent.click(screen.getByRole('radio', { name: 'Lista' }));
    await userEvent.click(screen.getByRole('button', { name: 'Guardar configuración' }));
    await waitFor(() => screen.getByRole('alert'));

    const getLayoutCallsBefore = vi.mocked(layoutApi.getLayout).mock.calls.length;

    // Act — click "Recargar" inside the conflict alert
    await userEvent.click(screen.getByRole('button', { name: SAVE_LAYOUT_STRINGS.reload }));

    // Assert — getLayout is called again (refetch triggered)
    await waitFor(() =>
      expect(vi.mocked(layoutApi.getLayout).mock.calls.length).toBeGreaterThan(
        getLayoutCallsBefore,
      ),
    );
  });
});
