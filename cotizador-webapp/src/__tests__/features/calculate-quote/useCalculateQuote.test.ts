import { describe, it, expect, vi, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { useCalculateQuote } from '@/features/calculate-quote/model/useCalculateQuote';
import * as sharedApi from '@/shared/api';
import { CALCULATE_QUOTE_STRINGS } from '@/features/calculate-quote/strings';
import type { CalculateResultResponse } from '@/features/calculate-quote/model/useCalculateQuote';

vi.mock('@/shared/api', () => ({
  httpClient: {
    post: vi.fn(),
  },
}));

const mockResult: CalculateResultResponse = {
  netPremium: 10000,
  commercialPremiumBeforeTax: 11000,
  commercialPremium: 12000,
  premiumsByLocation: [],
  quoteStatus: 'calculated',
  version: 6,
};

const FOLIO = 'DAN-2026-00001';

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

describe('useCalculateQuote', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('invalidates quote-state query on success', async () => {
    // Arrange
    vi.mocked(sharedApi.httpClient.post).mockResolvedValue({ data: mockResult });
    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    // Act
    const { result } = renderHook(
      () => useCalculateQuote({ folio: FOLIO }),
      { wrapper },
    );
    await act(async () => {
      result.current.mutate({ version: 5 });
    });
    await waitFor(() => expect(result.current.isPending).toBe(false));

    // Assert
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['quote-state', FOLIO] });
  });

  it('calls onError with versionConflict type when API returns 409', async () => {
    // Arrange
    vi.mocked(sharedApi.httpClient.post).mockRejectedValue({ type: 'versionConflict', message: 'Conflicto' });
    const onError = vi.fn();
    const { wrapper } = createWrapper();

    // Act
    const { result } = renderHook(
      () => useCalculateQuote({ folio: FOLIO, onError }),
      { wrapper },
    );
    await act(async () => {
      result.current.mutate({ version: 5 });
    });
    await waitFor(() => expect(result.current.isError).toBe(true));

    // Assert
    expect(onError).toHaveBeenCalledWith(
      expect.stringContaining('Recarga la página'),
      'versionConflict',
    );
  });

  it('calls onError with noCalculable type when API returns invalidQuoteState', async () => {
    // Arrange
    vi.mocked(sharedApi.httpClient.post).mockRejectedValue({ type: 'invalidQuoteState' });
    const onError = vi.fn();
    const { wrapper } = createWrapper();

    // Act
    const { result } = renderHook(
      () => useCalculateQuote({ folio: FOLIO, onError }),
      { wrapper },
    );
    await act(async () => {
      result.current.mutate({ version: 5 });
    });
    await waitFor(() => expect(result.current.isError).toBe(true));

    // Assert
    expect(onError).toHaveBeenCalledWith(
      CALCULATE_QUOTE_STRINGS.errorNoCalculable,
      'noCalculable',
    );
  });

  it('calls onError with generic message for unknown errors', async () => {
    // Arrange
    vi.mocked(sharedApi.httpClient.post).mockRejectedValue({ type: 'unknown', message: 'Error desconocido' });
    const onError = vi.fn();
    const { wrapper } = createWrapper();

    // Act
    const { result } = renderHook(
      () => useCalculateQuote({ folio: FOLIO, onError }),
      { wrapper },
    );
    await act(async () => {
      result.current.mutate({ version: 5 });
    });
    await waitFor(() => expect(result.current.isError).toBe(true));

    // Assert
    expect(onError).toHaveBeenCalledWith('Error desconocido', 'generic');
  });
});
