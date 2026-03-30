import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AutoResolvedFields } from '@/features/save-locations/ui/AutoResolvedFields';

describe('AutoResolvedFields', () => {
  it('renders the "Datos resueltos automáticamente" heading', () => {
    // Arrange & Act
    render(<AutoResolvedFields />);

    // Assert
    expect(screen.getByText('Datos resueltos automáticamente')).toBeInTheDocument();
  });

  it('shows "auto" chip for each field', () => {
    // Arrange & Act
    render(
      <AutoResolvedFields
        state="Ciudad de México"
        municipality="Cuauhtémoc"
        neighborhood="Doctores"
        catZone="A"
      />,
    );

    // Assert — 4 chips (hidden from accessibility, but in the DOM)
    const chips = screen.getAllByText('auto');
    expect(chips).toHaveLength(4);
  });

  it('shows state value when provided', () => {
    // Arrange & Act
    render(<AutoResolvedFields state="Ciudad de México" />);

    // Assert
    expect(screen.getByText('Ciudad de México')).toBeInTheDocument();
  });

  it('shows municipality value when provided', () => {
    // Arrange & Act
    render(<AutoResolvedFields municipality="Cuauhtémoc" />);

    // Assert
    expect(screen.getByText('Cuauhtémoc')).toBeInTheDocument();
  });

  it('shows neighborhood value when provided', () => {
    // Arrange & Act
    render(<AutoResolvedFields neighborhood="Doctores" />);

    // Assert
    expect(screen.getByText('Doctores')).toBeInTheDocument();
  });

  it('shows "–" placeholder when state is undefined', () => {
    // Arrange & Act — only municipality is set, state/neighborhood/catZone will show "–"
    render(<AutoResolvedFields municipality="Cuauhtémoc" />);

    // Assert — at least one "–" exists for the unresolved fields
    const dashes = screen.getAllByText('–');
    expect(dashes.length).toBeGreaterThanOrEqual(1);
  });

  it('shows catZone badge "Zona A" when catZone is provided', () => {
    // Arrange & Act
    render(<AutoResolvedFields catZone="A" />);

    // Assert — badge text is rendered
    expect(screen.getByText('Zona A')).toBeInTheDocument();
  });

  it('shows "–" for catZone when catZone is undefined', () => {
    // Arrange & Act
    render(<AutoResolvedFields state="Ciudad de México" />);

    // Assert — at least one "–" exists (municipality, neighborhood, catZone are unresolved)
    const dashes = screen.getAllByText('–');
    expect(dashes.length).toBeGreaterThanOrEqual(1);
  });

  it('shows all resolved fields at once', () => {
    // Arrange & Act
    render(
      <AutoResolvedFields
        state="Jalisco"
        municipality="Guadalajara"
        neighborhood="Centro"
        catZone="B"
      />,
    );

    // Assert
    expect(screen.getByText('Jalisco')).toBeInTheDocument();
    expect(screen.getByText('Guadalajara')).toBeInTheDocument();
    expect(screen.getByText('Centro')).toBeInTheDocument();
    expect(screen.getByText('Zona B')).toBeInTheDocument();
  });
});
