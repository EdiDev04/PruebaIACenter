import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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

function makeFormMocks(guarantees: LocationFormValues['guarantees'] = []) {
  const getValues = vi.fn().mockImplementation((key: string) => {
    if (key === 'guarantees') return guarantees;
    return undefined;
  }) as unknown as import('react-hook-form').UseFormGetValues<LocationFormValues>;

  const setValue = vi.fn() as unknown as import('react-hook-form').UseFormSetValue<LocationFormValues>;
  return { getValues, setValue };
}

describe('GuaranteeGroup', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders the group label', () => {
    // Arrange
    const { getValues, setValue } = makeFormMocks();

    // Act
    render(<GuaranteeGroup group={baseGroup} getValues={getValues} setValue={setValue} />);

    // Assert
    expect(screen.getByRole('button', { name: /Coberturas base/ })).toBeInTheDocument();
  });

  it('starts collapsed by default (defaultOpen=false) — items are hidden', () => {
    // Arrange
    const { getValues, setValue } = makeFormMocks();

    // Act
    render(<GuaranteeGroup group={baseGroup} getValues={getValues} setValue={setValue} />);

    // Assert — checkboxes are not visible until expanded
    expect(screen.queryByRole('checkbox', { name: 'Incendio de edificio' })).not.toBeInTheDocument();
  });

  it('starts expanded when defaultOpen=true', () => {
    // Arrange
    const { getValues, setValue } = makeFormMocks();

    // Act
    render(
      <GuaranteeGroup group={baseGroup} getValues={getValues} setValue={setValue} defaultOpen />,
    );

    // Assert — items visible
    expect(screen.getByRole('checkbox', { name: 'Incendio de edificio' })).toBeInTheDocument();
  });

  it('expands when the header button is clicked', async () => {
    // Arrange
    const { getValues, setValue } = makeFormMocks();
    render(<GuaranteeGroup group={baseGroup} getValues={getValues} setValue={setValue} />);
    expect(screen.queryByRole('checkbox', { name: 'Incendio de edificio' })).not.toBeInTheDocument();

    // Act
    await userEvent.click(screen.getByRole('button', { name: /Coberturas base/ }));

    // Assert
    expect(screen.getByRole('checkbox', { name: 'Incendio de edificio' })).toBeInTheDocument();
  });

  it('shows counter "0 de 2 seleccionadas" when collapsed with no guarantees selected', () => {
    // Arrange
    const { getValues, setValue } = makeFormMocks([]);

    // Act
    render(<GuaranteeGroup group={baseGroup} getValues={getValues} setValue={setValue} />);

    // Assert
    expect(screen.getByText('0 de 2 seleccionadas')).toBeInTheDocument();
  });

  it('shows counter "1 de 2 seleccionadas" when one guarantee is pre-selected', () => {
    // Arrange
    const { getValues, setValue } = makeFormMocks([
      { guaranteeKey: 'building_fire', insuredAmount: 0 },
    ]);

    // Act
    render(<GuaranteeGroup group={baseGroup} getValues={getValues} setValue={setValue} />);

    // Assert
    expect(screen.getByText('1 de 2 seleccionadas')).toBeInTheDocument();
  });

  it('calls setValue to add the guaranteeKey when a checkbox is checked', async () => {
    // Arrange
    const { getValues, setValue } = makeFormMocks([]);
    render(
      <GuaranteeGroup group={baseGroup} getValues={getValues} setValue={setValue} defaultOpen />,
    );

    // Act
    await userEvent.click(screen.getByRole('checkbox', { name: 'Incendio de edificio' }));

    // Assert
    expect(setValue).toHaveBeenCalledWith(
      'guarantees',
      [{ guaranteeKey: 'building_fire', insuredAmount: 0 }],
      { shouldValidate: true },
    );
  });

  it('calls setValue to remove the guaranteeKey when a checkbox is unchecked', async () => {
    // Arrange — building_fire starts checked
    const { getValues, setValue } = makeFormMocks([
      { guaranteeKey: 'building_fire', insuredAmount: 5000000 },
    ]);
    render(
      <GuaranteeGroup group={baseGroup} getValues={getValues} setValue={setValue} defaultOpen />,
    );

    // Act — uncheck building_fire
    await userEvent.click(screen.getByRole('checkbox', { name: 'Incendio de edificio' }));

    // Assert — setValue called with empty array (building_fire removed)
    expect(setValue).toHaveBeenCalledWith('guarantees', [], { shouldValidate: true });
  });

  it('shows insured amount input when a guarantee with requiresInsuredAmount is selected', () => {
    // Arrange — building_fire is already selected
    const { getValues, setValue } = makeFormMocks([
      { guaranteeKey: 'building_fire', insuredAmount: 0 },
    ]);

    // Act
    render(
      <GuaranteeGroup group={baseGroup} getValues={getValues} setValue={setValue} defaultOpen />,
    );

    // Assert — amount input is visible
    expect(
      screen.getByRole('spinbutton', { name: 'Suma asegurada para Incendio de edificio' }),
    ).toBeInTheDocument();
  });

  it('does not show insured amount input for glass (requiresInsuredAmount=false)', () => {
    // Arrange — glass is selected but doesn't need amount
    const { getValues, setValue } = makeFormMocks([
      { guaranteeKey: 'glass', insuredAmount: 0 },
    ]);

    // Act
    render(
      <GuaranteeGroup group={baseGroup} getValues={getValues} setValue={setValue} defaultOpen />,
    );

    // Assert — glass doesn't show an amount input
    expect(
      screen.queryByRole('spinbutton', { name: 'Suma asegurada para Cristales' }),
    ).not.toBeInTheDocument();
  });

  it('shows "Recomendado" badge for recommended items', () => {
    // Arrange
    const { getValues, setValue } = makeFormMocks();

    // Act
    render(
      <GuaranteeGroup group={baseGroup} getValues={getValues} setValue={setValue} defaultOpen />,
    );

    // Assert
    expect(screen.getByText('Recomendado')).toBeInTheDocument();
  });

  it('renders a different group correctly', () => {
    // Arrange
    const { getValues, setValue } = makeFormMocks();

    // Act
    render(
      <GuaranteeGroup group={catGroup} getValues={getValues} setValue={setValue} defaultOpen />,
    );

    // Assert
    expect(
      screen.getByRole('checkbox', { name: 'Terremoto y erupcion volcanica' }),
    ).toBeInTheDocument();
  });
});
