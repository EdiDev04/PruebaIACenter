import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createRef } from 'react';
import { FolioSearchInput } from '@/features/folio-search/ui/FolioSearchInput';

describe('FolioSearchInput', () => {
  it('renders the "Número de folio" label', () => {
    render(<FolioSearchInput />);
    expect(screen.getByLabelText('Número de folio')).toBeInTheDocument();
  });

  it('shows the placeholder "DAN-YYYY-NNNNN"', () => {
    render(<FolioSearchInput />);
    const input = screen.getByLabelText('Número de folio');
    expect(input).toHaveAttribute('placeholder', 'DAN-YYYY-NNNNN');
  });

  it('displays the error message when the error prop is provided', () => {
    render(<FolioSearchInput error="Folio no encontrado" />);
    expect(screen.getByRole('alert')).toHaveTextContent('Folio no encontrado');
  });

  it('does not render an alert when no error is provided', () => {
    render(<FolioSearchInput />);
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('calls the onChange handler when the user types', async () => {
    const handleChange = vi.fn();
    render(<FolioSearchInput onChange={handleChange} />);
    const input = screen.getByLabelText('Número de folio');
    await userEvent.type(input, 'D');
    expect(handleChange).toHaveBeenCalled();
  });

  it('forwards the ref to the underlying input element', () => {
    const ref = createRef<HTMLInputElement>();
    render(<FolioSearchInput ref={ref} />);
    expect(ref.current).toBeInstanceOf(HTMLInputElement);
  });
});
