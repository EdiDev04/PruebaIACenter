import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { useCreateFolio } from '@/features/folio-creation/model/useCreateFolio';
import * as folioApi from '@/features/folio-creation/api/folioApi';

vi.mock('@/features/folio-creation/api/folioApi');

const mockFolioResponse: folioApi.CreatedFolioResponse = {
  data: {
    folioNumber: 'DAN-2026-00001',
    quoteStatus: 'draft',
    version: 1,
    metadata: { createdAt: '2026-01-01T00:00:00Z', lastWizardStep: 1 },
  },
};

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

describe('useCreateFolio', () => {
  beforeEach(() => {
    let uuidCount = 0;
    vi.spyOn(crypto, 'randomUUID').mockImplementation(() => `test-uuid-${++uuidCount}` as `${string}-${string}-${string}-${string}-${string}`);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('calls createFolio with the current idempotency key on mutate', async () => {
    const mockCreate = vi.mocked(folioApi.createFolio).mockResolvedValue(mockFolioResponse);

    const { result } = renderHook(() => useCreateFolio(), { wrapper: createWrapper() });

    await act(async () => {
      await result.current.mutateAsync(undefined);
    });

    expect(mockCreate).toHaveBeenCalledTimes(1);
    expect(mockCreate).toHaveBeenCalledWith('test-uuid-1');
  });

  it('keeps the same idempotency key after an error to allow retry', async () => {
    const capturedKeys: string[] = [];

    vi.mocked(folioApi.createFolio).mockImplementation((key) => {
      capturedKeys.push(key);
      return Promise.reject(new Error('Network error'));
    });

    const { result } = renderHook(() => useCreateFolio(), { wrapper: createWrapper() });

    // First attempt (fails)
    await act(async () => {
      await result.current.mutateAsync(undefined).catch(() => {});
    });

    // Second attempt (retry — should reuse the same idempotency key)
    await act(async () => {
      await result.current.mutateAsync(undefined).catch(() => {});
    });

    expect(capturedKeys).toHaveLength(2);
    expect(capturedKeys[0]).toBe('test-uuid-1');
    expect(capturedKeys[1]).toBe('test-uuid-1');
  });

  it('generates a new idempotency key after a successful creation', async () => {
    const capturedKeys: string[] = [];

    vi.mocked(folioApi.createFolio).mockImplementation((key) => {
      capturedKeys.push(key);
      return Promise.resolve(mockFolioResponse);
    });

    const { result } = renderHook(() => useCreateFolio(), { wrapper: createWrapper() });

    // First mutation (success — onSuccess rotates the key)
    await act(async () => {
      await result.current.mutateAsync(undefined);
    });

    await waitFor(() => expect(capturedKeys).toHaveLength(1));

    // Second mutation (should use the rotated key)
    await act(async () => {
      await result.current.mutateAsync(undefined);
    });

    expect(capturedKeys).toHaveLength(2);
    expect(capturedKeys[0]).toBe('test-uuid-1');
    expect(capturedKeys[1]).toBe('test-uuid-2');
  });
});
