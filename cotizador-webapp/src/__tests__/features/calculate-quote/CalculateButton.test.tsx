import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { CalculateButton } from '@/features/calculate-quote/ui/CalculateButton';

vi.mock('@/features/calculate-quote/model/useCalculateQuote', () => ({
  useCalculateQuote: vi.fn(),
}));

import { useCalculateQuote } from '@/features/calculate-quote/model/useCalculateQuote';

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

function renderButton(props?: Partial<React.ComponentProps<typeof CalculateButton>>) {
  const { wrapper } = createWrapper();
  return render(
    <CalculateButton folio="DAN-2026-00001" version={5} {...props} />,
    { wrapper },
  );
}

describe('CalculateButton', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders the calculate button with correct text', () => {
    // Arrange
    vi.mocked(useCalculateQuote).mockReturnValue({ mutate: vi.fn(), isPending: false } as any);

    // Act
    renderButton();

    // Assert
    expect(screen.getByRole('button', { name: 'Calcular cotización' })).toBeInTheDocument();
  });

  it('is disabled when disabled prop is true', () => {
    // Arrange
    vi.mocked(useCalculateQuote).mockReturnValue({ mutate: vi.fn(), isPending: false } as any);

    // Act
    renderButton({ disabled: true });

    // Assert
    expect(screen.getByRole('button', { name: 'Calcular cotización' })).toBeDisabled();
  });

  it('is enabled when disabled prop is false', () => {
    // Arrange
    vi.mocked(useCalculateQuote).mockReturnValue({ mutate: vi.fn(), isPending: false } as any);

    // Act
    renderButton({ disabled: false });

    // Assert
    expect(screen.getByRole('button', { name: 'Calcular cotización' })).not.toBeDisabled();
  });

  it('calls mutate when clicked', async () => {
    // Arrange
    const mockMutate = vi.fn();
    vi.mocked(useCalculateQuote).mockReturnValue({ mutate: mockMutate, isPending: false } as any);
    renderButton({ version: 5 });

    // Act
    await userEvent.click(screen.getByRole('button', { name: 'Calcular cotización' }));

    // Assert
    expect(mockMutate).toHaveBeenCalledWith({ version: 5 });
  });

  it('shows calculating text and is disabled when isPending is true', () => {
    // Arrange
    vi.mocked(useCalculateQuote).mockReturnValue({ mutate: vi.fn(), isPending: true } as any);

    // Act
    renderButton();

    // Assert
    const btn = screen.getByRole('button', { name: 'Calculando...' });
    expect(btn).toBeInTheDocument();
    expect(btn).toBeDisabled();
  });
});
