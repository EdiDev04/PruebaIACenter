import type { UseFormReturn } from 'react-hook-form';
import { Controller } from 'react-hook-form';
import type { ZipCodeDto } from '@/entities/zip-code';
import type { LocationFormValues } from '@/entities/location';
import { TextInput, RadioGroup } from '@/shared/ui';
import type { RadioOption } from '@/shared/ui';
import { ZipCodeField } from './ZipCodeField';
import { AutoResolvedFields } from './AutoResolvedFields';
import { SAVE_LOCATIONS_STRINGS as S } from '../strings';
import styles from './LocationFormStep1.module.css';

const CONSTRUCTION_TYPE_OPTIONS: RadioOption[] = [
  { value: 'Tipo 1 - Macizo', label: 'Tipo 1 – Macizo' },
  { value: 'Tipo 2 - Mixto', label: 'Tipo 2 – Mixto' },
  { value: 'Tipo 3 - Ligero', label: 'Tipo 3 – Ligero' },
  { value: 'Tipo 4 - Metalico', label: 'Tipo 4 – Metálico' },
];

interface Props {
  readonly form: UseFormReturn<LocationFormValues>;
  readonly onZipResolved: (data: ZipCodeDto) => void;
}

export function LocationFormStep1({ form, onZipResolved }: Props) {
  const {
    register,
    control,
    setValue,
    watch,
    formState: { errors },
  } = form;

  const state = watch('state');
  const municipality = watch('municipality');
  const neighborhood = watch('neighborhood');
  const catZone = watch('catZone');

  function handleZipCleared() {
    setValue('state', undefined);
    setValue('municipality', undefined);
    setValue('neighborhood', undefined);
    setValue('city', undefined);
    setValue('catZone', undefined);
  }

  return (
    <div className={styles.step}>
      <TextInput
        label={`${S.labelLocationName} *`}
        error={errors.locationName?.message}
        {...register('locationName')}
      />

      <TextInput
        label={`${S.labelAddress} *`}
        error={errors.address?.message}
        {...register('address')}
      />

      <Controller
        name="zipCode"
        control={control}
        render={({ field, fieldState }) => (
          <ZipCodeField
            value={field.value ?? ''}
            onChange={field.onChange}
            onResolved={(data) => {
              setValue('state', data.state);
              setValue('municipality', data.municipality);
              setValue('neighborhood', data.neighborhood);
              setValue('city', data.city);
              setValue('catZone', data.catZone);
              onZipResolved(data);
            }}
            onCleared={handleZipCleared}
            error={fieldState.error?.message}
          />
        )}
      />

      <AutoResolvedFields
        state={state}
        municipality={municipality}
        neighborhood={neighborhood}
        catZone={catZone}
      />

      <Controller
        name="constructionType"
        control={control}
        render={({ field, fieldState }) => (
          <RadioGroup
            legend={S.labelConstructionType}
            name="constructionType"
            options={CONSTRUCTION_TYPE_OPTIONS}
            value={field.value}
            onChange={field.onChange}
            error={fieldState.error?.message}
          />
        )}
      />

      <div className={styles.row}>
        <TextInput
          label={S.labelLevel}
          type="number"
          min={0}
          error={errors.level?.message}
          {...register('level', { valueAsNumber: true })}
        />
        <TextInput
          label={S.labelConstructionYear}
          type="number"
          min={1800}
          max={2026}
          error={errors.constructionYear?.message}
          {...register('constructionYear', { valueAsNumber: true })}
        />
      </div>
    </div>
  );
}
