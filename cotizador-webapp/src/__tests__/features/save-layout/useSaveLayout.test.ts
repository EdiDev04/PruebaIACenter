import { describe, it, expect, vi, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { useSaveLayout } from '@/features/save-layout/model/useSaveLayout';
import * as layoutApi from '@/entities/layout/api/layoutApi';
import type { UpdateLayoutRequest } from '@/entities/layout';

vi.mock('@/entities/layout/api/layoutApi');

const mockRequest: UpdateLayoutRequest = {
  displayMode: 'grid',
  visibleColumns: ['index', 'locationName', 'zipCode', 'businessLine', 'validationStatus'],
  version: 2,
};

const mockSuccessResponse: layoutApi.LayoutResponse = {
  data: {
    displayMode: 'grid',
    visibleColumns: ['index', 'locationName', 'zipCode', 'businessLine', 'validationStatus'],
    version: 3,
  },
};

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  function Wrapper({ children }: { children: React.ReactNode }) {
    return React.createElement(QueryClientProvider, { client: queryClient }, children);
  }
  return { wrapper: Wrapper, queryClient };
}

describe('useSaveLayout', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('invalidates the ["layout", folio] query on a successful mutation', async () => {
    // Arrange
    vi.mocked(layoutApi.updateLayout).mockResolvedValue(mockSuccessResponse);
    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    // Act
    const { result } = renderHook(
      () => useSaveLayout({ folio: 'DAN-2026-00001' }),
      { wrapper },
    );
    await act(async () => {
      result.current.mutate(mockRequest);
    });
    await waitFor(() => expect(result.current.isPending).toBe(false));

    // Assert
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['layout', 'DAN-2026-00001'] });
  });

  it('sets isConflict to true when error type is versionConflict', async () => {
    // Arrange
    const conflictError = { type: 'versionConflict', message: 'Conflicto de versión', field: null };
    vi.mocked(layoutApi.updateLayout).mockRejectedValue(conflictError);
    const { wrapper } = createWrapper();

    // Act
    const { result } = renderHook(
      () => useSaveLayout({ folio: 'DAN-2026-00001' }),
      { wrapper },
    );
    await act(async () => {
      result.current.mutate(mockRequest);
    });
    await waitFor(() => expect(result.current.isError).toBe(true));

    // Assert
    expect(result.current.isConflict).toBe(true);
    expect(result.current.isFolioNotFound).toBe(false);
  });

  it('sets isFolioNotFound to true when error type is folioNotFound', async () => {
    // Arrange
    const notFoundError = { type: 'folioNotFound', message: 'El folio no existe', field: null };
    vi.mocked(layoutApi.updateLayout).mockRejectedValue(notFoundError);
    const { wrapper } = createWrapper();

    // Act
    const { result } = renderHook(
      () => useSaveLayout({ folio: 'DAN-2026-00001' }),
      { wrapper },
    );
    await act(async () => {
      result.current.mutate(mockRequest);
    });
    await waitFor(() => expect(result.current.isError).toBe(true));

    // Assert
    expect(result.current.isFolioNotFound).toBe(true);
    expect(result.current.isConflict).toBe(false);
  });

  it('propagates validation errors and sets isValidationError to true', async () => {
    // Arrange
    const validationError = {
      type: 'validationError',
      message: 'Campo inválido',
      field: 'visibleColumns',
    };
    vi.mocked(layoutApi.updateLayout).mockRejectedValue(validationError);
    const { wrapper } = createWrapper();

    // Act
    const { result } = renderHook(
      () => useSaveLayout({ folio: 'DAN-2026-00001' }),
      { wrapper },
    );
    await act(async () => {
      result.current.mutate(mockRequest);
    });
    await waitFor(() => expect(result.current.isError).toBe(true));

    // Assert
    expect(result.current.isValidationError).toBe(true);
    expect(result.current.error?.field).toBe('visibleColumns');
  });

  it('calls the onSuccess callback after a successful mutation', async () => {
    // Arrange
    vi.mocked(layoutApi.updateLayout).mockResolvedValue(mockSuccessResponse);
    const onSuccess = vi.fn();
    const { wrapper } = createWrapper();

    // Act
    const { result } = renderHook(
      () => useSaveLayout({ folio: 'DAN-2026-00001', onSuccess }),
      { wrapper },
    );
    await act(async () => {
      result.current.mutate(mockRequest);
    });
    await waitFor(() => expect(result.current.isPending).toBe(false));

    // Assert
    expect(onSuccess).toHaveBeenCalledTimes(1);
  });

  it('calls the onFolioNotFound callback when error type is folioNotFound', async () => {
    // Arrange
    const notFoundError = { type: 'folioNotFound', message: 'El folio no existe', field: null };
    vi.mocked(layoutApi.updateLayout).mockRejectedValue(notFoundError);
    const onFolioNotFound = vi.fn();
    const { wrapper } = createWrapper();

    // Act
    const { result } = renderHook(
      () => useSaveLayout({ folio: 'DAN-2026-00001', onFolioNotFound }),
      { wrapper },
    );
    await act(async () => {
      result.current.mutate(mockRequest);
    });
    await waitFor(() => expect(result.current.isError).toBe(true));

    // Assert
    expect(onFolioNotFound).toHaveBeenCalledTimes(1);
  });
});
