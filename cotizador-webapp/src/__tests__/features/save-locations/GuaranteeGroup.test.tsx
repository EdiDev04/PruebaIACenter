import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import React from 'react';
import { useForm } from 'react-hook-form';
import { GuaranteeGroup } from '@/features/save-locations/ui/GuaranteeGroup';
import type { GuaranteeGroup as GuaranteeGroupType } from '@/entities/location/model/guaranteeCatalog';
import type { LocationFormValues } from '@/entities/location';

/** Test group fixture — subset of the real catalog */
const baseGroup: GuaranteeGroupType = {
  groupKey: 'base',
  label: 'Coberturas base',
  items: [
    { guaranteeKey: 'building_fire', label: 'Incendio de edificio', requiresInsuredAmount: true, recommended: true },
    { guaranteeKey: 'glass', label: 'Cristales', requiresInsuredAmount: false },
  ],
};

const catGroup: GuaranteeGroupType = {
  groupKey: 'cat',
  label: 'Catastrofes naturales',
  items: [
    { guaranteeKey: 'cat_tev', label: 'Terremoto y erupcion volcanica', requiresInsuredAmount: true },
  ],
};

/** Wrapper component that provides a real useForm context */
function GuaranteeGroupWrapper({
  group,
  defaultGuarantees = [],
  defaultOpen = false,
  onSetValue,
}: {
  group: GuaranteeGroupType;
  defaultGuarantees?: LocationFormValues['guarantees'];
  defaultOpen?: boolean;
  onSetValue?: ReturnType<typeof vi.fn>;
}) {
  const { control, setValue } = useForm<LocationFormValues>({
    defaultValues: { guarantees: defaultGuarantees },
  });
  const wrappedSetValue: typeof setValue = onSetValue
    ? ((...args) => {
        onSetValue(...args);
        return setValue(...args);
      }) as typeof setValue
    : setValue;
  return (
    <GuaranteeGroup
      group={group}
      control={control}
      setValue={wrappedSetValue}
      defaultOpen={defaultOpen}
    />
  );
}

describe('GuaranteeGroup', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders the group label', () => {
    // Arrange & Act
    render(<GuaranteeGroupWrapper group={baseGroup} />);

    // Assert
    expect(screen.getByRole('button', { name: /Coberturas base/ })).toBeInTheDocument();
  });

  it('starts collapsed by default (defaultOpen=false) — items are hidden', () => {
    // Arrange & Act
    render(<GuaranteeGroupWrapper group={baseGroup} />);

    // Assert — checkboxes are not visible until expanded
    expect(screen.queryByRole('checkbox', { name: 'Incendio de edificio' })).not.toBeInTheDocument();
  });

  it('starts expanded when defaultOpen=true', () => {
    // Arrange & Act
    render(<GuaranteeGroupWrapper group={baseGroup} defaultOpen />);

    // Assert — items visible
    expect(screen.getByRole('checkbox', { name: 'Incendio de edificio' })).toBeInTheDocument();
  });

  it('expands when the header button is clicked', async () => {
    // Arrange
    render(<GuaranteeGroupWrapper group={baseGroup} />);
    expect(screen.queryByRole('checkbox', { name: 'Incendio de edificio' })).not.toBeInTheDocument();

    // Act
    await userEvent.click(screen.getByRole('button', { name: /Coberturas base/ }));

    // Assert
    expect(screen.getByRole('checkbox', { name: 'Incendio de edificio' })).toBeInTheDocument();
  });

  it('shows counter "0 de 2 seleccionadas" when collapsed with no guarantees selected', () => {
    // Arrange & Act
    render(<GuaranteeGroupWrapper group={baseGroup} defaultGuarantees={[]} />);

    // Assert
    expect(screen.getByText('0 de 2 seleccionadas')).toBeInTheDocument();
  });

  it('shows counter "1 de 2 seleccionadas" when one guarantee is pre-selected', () => {
    // Arrange & Act
    render(
      <GuaranteeGroupWrapper
        group={baseGroup}
        defaultGuarantees={[{ guaranteeKey: 'building_fire', insuredAmount: 0 }]}
      />,
    );

    // Assert
    expect(screen.getByText('1 de 2 seleccionadas')).toBeInTheDocument();
  });

  it('checkbox becomes checked when clicked (adds guarantee)', async () => {
    // Arrange
    render(<GuaranteeGroupWrapper group={baseGroup} defaultGuarantees={[]} defaultOpen />);
    const checkbox = screen.getByRole('checkbox', { name: 'Incendio de edificio' });
    expect(checkbox).not.toBeChecked();

    // Act
    await userEvent.click(checkbox);

    // Assert
    expect(screen.getByRole('checkbox', { name: 'Incendio de edificio' })).toBeChecked();
  });

  it('checkbox becomes unchecked when clicked again (removes guarantee)', async () => {
    // Arrange — building_fire starts checked
    render(
      <GuaranteeGroupWrapper
        group={baseGroup}
        defaultGuarantees={[{ guaranteeKey: 'building_fire', insuredAmount: 5000000 }]}
        defaultOpen
      />,
    );
    const checkbox = screen.getByRole('checkbox', { name: 'Incendio de edificio' });
    expect(checkbox).toBeChecked();

    // Act — uncheck building_fire
    await userEvent.click(checkbox);

    // Assert
    expect(screen.getByRole('checkbox', { name: 'Incendio de edificio' })).not.toBeChecked();
  });

  it('shows insured amount input when a guarantee with requiresInsuredAmount is selected', () => {
    // Arrange — building_fire is already selected
    render(
      <GuaranteeGroupWrapper
        group={baseGroup}
        defaultGuarantees={[{ guaranteeKey: 'building_fire', insuredAmount: 0 }]}
        defaultOpen
      />,
    );

    // Assert — amount input is visible
    expect(
      screen.getByRole('spinbutton', { name: 'Suma asegurada para Incendio de edificio' }),
    ).toBeInTheDocument();
  });

  it('does not show insured amount input for glass (requiresInsuredAmount=false)', () => {
    // Arrange — glass is selected but doesn't need amount
    render(
      <GuaranteeGroupWrapper
        group={baseGroup}
        defaultGuarantees={[{ guaranteeKey: 'glass', insuredAmount: 0 }]}
        defaultOpen
      />,
    );

    // Assert — glass doesn't show an amount input
    expect(
      screen.queryByRole('spinbutton', { name: 'Suma asegurada para Cristales' }),
    ).not.toBeInTheDocument();
  });

  it('shows "Recomendado" badge for recommended items', () => {
    // Arrange & Act
    render(<GuaranteeGroupWrapper group={baseGroup} defaultOpen />);

    // Assert
    expect(screen.getByText('Recomendado')).toBeInTheDocument();
  });

  it('renders a different group correctly', () => {
    // Arrange & Act
    render(<GuaranteeGroupWrapper group={catGroup} defaultOpen />);

    // Assert
    expect(
      screen.getByRole('checkbox', { name: 'Terremoto y erupcion volcanica' }),
    ).toBeInTheDocument();
  });
});
