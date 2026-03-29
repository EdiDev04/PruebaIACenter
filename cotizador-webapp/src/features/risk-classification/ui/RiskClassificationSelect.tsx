import { Select } from '@/shared/ui';
import type { SelectOption } from '@/shared/ui';
import { useRiskClassificationQuery } from '../model/useRiskClassificationQuery';

const RISK_LABELS: Record<string, string> = {
  standard: 'Estándar',
  preferred: 'Preferente',
  substandard: 'Subestándar',
};

interface Props {
  readonly value: string;
  readonly onChange: (val: string) => void;
  readonly error?: string;
}

export function RiskClassificationSelect({ value, onChange, error }: Props) {
  const { data: classifications = [] } = useRiskClassificationQuery();

  const options: SelectOption[] = classifications.map((c) => ({
    value: c,
    label: RISK_LABELS[c] ?? c,
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
