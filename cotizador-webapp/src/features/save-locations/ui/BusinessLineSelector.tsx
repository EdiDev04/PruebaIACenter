import type { Control } from 'react-hook-form';
import { Controller } from 'react-hook-form';
import { ComboBox } from '@/shared/ui';
import type { ComboBoxOption } from '@/shared/ui';
import type { LocationFormValues } from '@/entities/location';
import type { BusinessLineDto } from '@/entities/business-line';
import { SAVE_LOCATIONS_STRINGS as S } from '../strings';

interface Props {
  readonly control: Control<LocationFormValues>;
  readonly options: BusinessLineDto[];
}

export function BusinessLineSelector({ control, options }: Props) {
  const comboOptions: ComboBoxOption[] = options.map((bl) => ({
    value: bl.code,
    label: `${bl.description}`,
    fireKey: bl.fireKey,
    description: bl.description,
  }));

  return (
    <Controller
      name="businessLine"
      control={control}
      render={({ field, fieldState }) => {
        const selectedCode = field.value
          ? options.find((o) => o.fireKey === field.value?.fireKey)?.code
          : undefined;

        return (
          <ComboBox
            label={S.labelBusinessLine}
            options={comboOptions}
            value={selectedCode}
            placeholder={S.placeholderBusinessLine}
            onSelect={(opt) => {
              const selected = options.find((o) => o.code === opt.value);
              field.onChange(
                selected
                  ? {
                      code: selected.code,
                      description: selected.description,
                      fireKey: selected.fireKey,
                      riskLevel: selected.riskLevel,
                    }
                  : undefined,
              );
            }}
            error={fieldState.error?.message}
            displayValue={(opt) => (opt ? `${opt.label}` : '')}
          />
        );
      }}
    />
  );
}
