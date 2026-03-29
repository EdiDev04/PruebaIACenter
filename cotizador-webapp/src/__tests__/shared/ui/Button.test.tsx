import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Button } from '@/shared/ui';

describe('Button', () => {
  it('renders children text correctly', () => {
    render(<Button>Guardar</Button>);
    expect(screen.getByRole('button', { name: 'Guardar' })).toBeInTheDocument();
  });

  it('shows loadingText and is disabled when isLoading is true', () => {
    render(
      <Button isLoading loadingText="Guardando...">
        Guardar
      </Button>
    );
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
    expect(screen.getByText('Guardando...')).toBeInTheDocument();
    expect(screen.queryByText('Guardar')).not.toBeInTheDocument();
  });

  it('shows fallback "Cargando..." when isLoading is true and no loadingText is provided', () => {
    render(<Button isLoading>Guardar</Button>);
    const button = screen.getByRole('button');
    expect(button).toBeDisabled();
    expect(screen.getByText('Cargando...')).toBeInTheDocument();
  });

  it('is disabled when the disabled prop is true', () => {
    render(<Button disabled>Guardar</Button>);
    expect(screen.getByRole('button')).toBeDisabled();
  });

  it('calls onClick when clicked and button is enabled', async () => {
    const handleClick = vi.fn();
    render(<Button onClick={handleClick}>Guardar</Button>);
    await userEvent.click(screen.getByRole('button'));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('does not call onClick when the button is disabled', async () => {
    const handleClick = vi.fn();
    render(
      <Button disabled onClick={handleClick}>
        Guardar
      </Button>
    );
    await userEvent.click(screen.getByRole('button'));
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('does not call onClick when isLoading is true', async () => {
    const handleClick = vi.fn();
    render(
      <Button isLoading loadingText="Cargando..." onClick={handleClick}>
        Guardar
      </Button>
    );
    await userEvent.click(screen.getByRole('button'));
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('applies the secondary variant class to the button element', () => {
    render(<Button variant="secondary">Cancelar</Button>);
    const button = screen.getByRole('button');
    expect(button.className).toMatch(/secondary/);
  });
});
