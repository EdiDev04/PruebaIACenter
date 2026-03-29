import type { Control, FieldErrors } from 'react-hook-form';
import { Controller } from 'react-hook-form';
import { TextInput, ReadOnlyField } from '@/shared/ui';
import { SubscriberComboBox } from '@/entities/catalog';
import type { GeneralInfoFormData } from '../model/generalInfoSchema';
import styles from './ConductionDataSection.module.css';

interface Props {
  control: Control<GeneralInfoFormData>;
  errors: FieldErrors<GeneralInfoFormData>;
  onSubscriberChange: (code: string, officeName: string) => void;
}

export function ConductionDataSection({ control, errors, onSubscriberChange }: Props) {
  return (
    <section className={styles.section}>
      <header className={styles.header}>
        <span className={styles.sectionLabel}>B. DATOS DE CONDUCCIÓN</span>
      </header>
      <div className={styles.fields}>
        <Controller
          control={control}
          name="subscriberCode"
          render={({ field }) => (
            <SubscriberComboBox
              value={field.value}
              onChange={(code, officeName) => {
                field.onChange(code);
                onSubscriberChange(code, officeName);
              }}
              error={errors.subscriberCode?.message}
            />
          )}
        />
        <div className={styles.row}>
          <Controller
            control={control}
            name="officeName"
            render={({ field }) => (
              <ReadOnlyField
                label="Oficina"
                value={field.value || '—'}
                hasAutoChip
              />
            )}
          />
          <Controller
            control={control}
            name="agentCode"
            render={({ field }) => (
              <TextInput
                label="Código de agente *"
                placeholder="001"
                helperText="Ingrese los 3 dígitos del agente"
                error={errors.agentCode?.message}
                name={field.name}
                ref={field.ref}
                value={field.value}
                onBlur={field.onBlur}
                onChange={(e) => {
                  const digits = e.target.value.replace(/\D/g, '').slice(0, 3);
                  const formatted = digits.length > 0 ? `AGT-${digits}` : '';
                  field.onChange(formatted);
                }}
              />
            )}
          />
        </div>
      </div>
    </section>
  );
}
