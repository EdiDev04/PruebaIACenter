import { ComboBox } from '@/shared/ui';
import type { ComboBoxOption } from '@/shared/ui';
import { useSubscribersQuery } from '../model/useSubscribersQuery';

interface Props {
  readonly value: string;
  readonly onChange: (code: string, officeName: string) => void;
  readonly error?: string;
}

export function SubscriberComboBox({ value, onChange, error }: Props) {
  const { data: subscribers = [], isLoading } = useSubscribersQuery();

  const options: ComboBoxOption[] = isLoading
    ? [{ value: '', label: 'Cargando suscriptores...' }]
    : subscribers.map((s) => ({
        value: s.code,
        label: `${s.name} (${s.code})`,
      }));

  const handleSelect = (option: ComboBoxOption) => {
    const subscriber = subscribers.find((s) => s.code === option.value);
    onChange(option.value, subscriber?.office ?? '');
  };

  return (
    <ComboBox
      label="Suscriptor *"
      options={options}
      value={value}
      onSelect={handleSelect}
      placeholder="Seleccionar suscriptor..."
      error={error}
    />
  );
}
