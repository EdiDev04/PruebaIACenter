import { describe, it, expect, vi, afterEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { useLayoutQuery } from '@/entities/layout/model/useLayoutQuery';
import * as layoutApi from '@/entities/layout/api/layoutApi';

vi.mock('@/entities/layout/api/layoutApi');

const mockLayoutResponse: layoutApi.LayoutResponse = {
  data: {
    displayMode: 'grid',
    visibleColumns: ['index', 'locationName', 'zipCode'],
    version: 3,
  },
};

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return React.createElement(QueryClientProvider, { client: queryClient }, children);
  };
}

describe('useLayoutQuery', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('maps displayMode, visibleColumns and version from a successful response', async () => {
    // Arrange
    vi.mocked(layoutApi.getLayout).mockResolvedValue(mockLayoutResponse);

    // Act
    const { result } = renderHook(
      () => useLayoutQuery('DAN-2026-00001'),
      { wrapper: createWrapper() },
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // Assert
    expect(result.current.data?.displayMode).toBe('grid');
    expect(result.current.data?.visibleColumns).toEqual(['index', 'locationName', 'zipCode']);
    expect(result.current.data?.version).toBe(3);
  });

  it('does not execute the query when folio is empty string', () => {
    // Arrange
    vi.mocked(layoutApi.getLayout).mockResolvedValue(mockLayoutResponse);

    // Act
    const { result } = renderHook(
      () => useLayoutQuery(''),
      { wrapper: createWrapper() },
    );

    // Assert — fetchStatus idle means the query was never started
    expect(result.current.fetchStatus).toBe('idle');
    expect(layoutApi.getLayout).not.toHaveBeenCalled();
  });

  it('propagates a folioNotFound error and does not retry', async () => {
    // Arrange
    const notFoundError = { type: 'folioNotFound', message: 'Folio no existe', field: null };
    vi.mocked(layoutApi.getLayout).mockRejectedValue(notFoundError);

    // Act
    const { result } = renderHook(
      () => useLayoutQuery('DAN-2026-99999'),
      { wrapper: createWrapper() },
    );
    await waitFor(() => expect(result.current.isError).toBe(true));

    // Assert — error propagated and API called exactly once (no retry)
    expect(result.current.error).toEqual(notFoundError);
    expect(vi.mocked(layoutApi.getLayout)).toHaveBeenCalledTimes(1);
  });

  it('propagates a validationError and does not retry on invalid folio format', async () => {
    // Arrange
    const validationError = { type: 'validationError', message: 'Formato de folio inválido', field: null };
    vi.mocked(layoutApi.getLayout).mockRejectedValue(validationError);

    // Act
    const { result } = renderHook(
      () => useLayoutQuery('INVALID'),
      { wrapper: createWrapper() },
    );
    await waitFor(() => expect(result.current.isError).toBe(true));

    // Assert — no retry, API called exactly once
    expect(result.current.isError).toBe(true);
    expect(vi.mocked(layoutApi.getLayout)).toHaveBeenCalledTimes(1);
  });

  it('calls onInvalidFolio callback when error type is validationError', async () => {
    // Arrange
    const validationError = { type: 'validationError', message: 'Formato inválido', field: null };
    vi.mocked(layoutApi.getLayout).mockRejectedValue(validationError);
    const onInvalidFolio = vi.fn();

    // Act
    const { result } = renderHook(
      () => useLayoutQuery('INVALID', { onInvalidFolio }),
      { wrapper: createWrapper() },
    );
    await waitFor(() => expect(result.current.isError).toBe(true));

    // Assert
    expect(onInvalidFolio).toHaveBeenCalledTimes(1);
  });

  it('calls onFolioNotFound callback when error type is folioNotFound', async () => {
    // Arrange
    const notFoundError = { type: 'folioNotFound', message: 'Folio no existe', field: null };
    vi.mocked(layoutApi.getLayout).mockRejectedValue(notFoundError);
    const onFolioNotFound = vi.fn();

    // Act
    const { result } = renderHook(
      () => useLayoutQuery('DAN-2026-99999', { onFolioNotFound }),
      { wrapper: createWrapper() },
    );
    await waitFor(() => expect(result.current.isError).toBe(true));

    // Assert
    expect(onFolioNotFound).toHaveBeenCalledTimes(1);
  });
});
