import { Select } from '@/shared/ui';
import type { SelectOption } from '@/shared/ui';
import { useRiskClassificationQuery } from '../model/useRiskClassificationQuery';

interface Props {
  readonly value: string;
  readonly onChange: (val: string) => void;
  readonly error?: string;
}

export function RiskClassificationSelect({ value, onChange, error }: Props) {
  const { data: classifications = [] } = useRiskClassificationQuery();

  const options: SelectOption[] = classifications.map((c) => ({
    value: c.code,
    label: c.description,
  }));

  return (
    <Select
      label="Clasificación de riesgo *"
      options={options}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder="Seleccionar clasificación..."
      error={error}
    />
  );
}
