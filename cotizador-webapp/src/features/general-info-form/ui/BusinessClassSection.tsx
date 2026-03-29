import type { Control, FieldErrors } from 'react-hook-form';
import { Controller } from 'react-hook-form';
import { RadioGroup } from '@/shared/ui';
import type { RadioOption } from '@/shared/ui';
import { RiskClassificationSelect } from '@/entities/catalog';
import type { GeneralInfoFormData } from '../model/generalInfoSchema';
import styles from './BusinessClassSection.module.css';

interface Props {
  control: Control<GeneralInfoFormData>;
  errors: FieldErrors<GeneralInfoFormData>;
}

const BUSINESS_TYPE_OPTIONS: RadioOption[] = [
  { value: 'commercial', label: 'Comercial' },
  { value: 'industrial', label: 'Industrial' },
  { value: 'residential', label: 'Residencial' },
];

export function BusinessClassSection({ control, errors }: Props) {
  return (
    <section className={styles.section}>
      <header className={styles.header}>
        <span className={styles.sectionLabel}>C. TIPO Y CLASIFICACIÓN DE RIESGO</span>
      </header>
      <div className={styles.fields}>
        <Controller
          control={control}
          name="businessType"
          render={({ field }) => (
            <RadioGroup
              name="businessType"
              legend="Tipo de negocio *"
              options={BUSINESS_TYPE_OPTIONS}
              value={field.value}
              onChange={field.onChange}
              error={errors.businessType?.message}
            />
          )}
        />
        <Controller
          control={control}
          name="riskClassification"
          render={({ field }) => (
            <RiskClassificationSelect
              value={field.value}
              onChange={field.onChange}
              error={errors.riskClassification?.message}
            />
          )}
        />
      </div>
    </section>
  );
}
