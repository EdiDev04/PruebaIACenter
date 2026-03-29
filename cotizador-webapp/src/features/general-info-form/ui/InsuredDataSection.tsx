import type { Control, FieldErrors } from 'react-hook-form';
import { Controller } from 'react-hook-form';
import { TextInput } from '@/shared/ui';
import type { GeneralInfoFormData } from '../model/generalInfoSchema';
import styles from './InsuredDataSection.module.css';

interface Props {
  readonly control: Control<GeneralInfoFormData>;
  readonly errors: FieldErrors<GeneralInfoFormData>;
}

export function InsuredDataSection({ control, errors }: Props) {
  return (
    <section className={styles.section}>
      <header className={styles.header}>
        <span className={styles.sectionLabel}>A. DATOS DEL ASEGURADO</span>
      </header>
      <div className={styles.fields}>
        <Controller
          control={control}
          name="name"
          render={({ field }) => (
            <TextInput
              label="Nombre del asegurado *"
              placeholder="Grupo Industrial SA de CV"
              error={errors.name?.message}
              {...field}
            />
          )}
        />
        <div className={styles.row}>
          <Controller
            control={control}
            name="taxId"
            render={({ field }) => (
              <TextInput
                label="RFC *"
                placeholder="GIN850101AAA"
                error={errors.taxId?.message}
                {...field}
                onChange={(e) => field.onChange(e.target.value.toUpperCase())}
              />
            )}
          />
          <Controller
            control={control}
            name="phone"
            render={({ field }) => (
              <TextInput
                label="Teléfono (opcional)"
                placeholder="(55) XXXX-XXXX"
                type="tel"
                error={errors.phone?.message}
                {...field}
              />
            )}
          />
        </div>
        <Controller
          control={control}
          name="email"
          render={({ field }) => (
            <TextInput
              label="Correo electrónico (opcional)"
              placeholder="contacto@empresa.com"
              type="email"
              error={errors.email?.message}
              {...field}
            />
          )}
        />
      </div>
    </section>
  );
}
