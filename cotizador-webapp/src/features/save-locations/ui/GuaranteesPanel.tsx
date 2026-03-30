import type { Control, UseFormSetValue } from 'react-hook-form';
import { useWatch } from 'react-hook-form';
import { GUARANTEE_GROUPS } from '@/entities/location/model/guaranteeCatalog';
import type { LocationFormValues } from '@/entities/location';
import { GuaranteeGroup } from './GuaranteeGroup';
import { SAVE_LOCATIONS_STRINGS as S } from '../strings';
import styles from './GuaranteesPanel.module.css';

interface Props {
  readonly control: Control<LocationFormValues>;
  readonly setValue: UseFormSetValue<LocationFormValues>;
}

export function GuaranteesPanel({ control, setValue }: Props) {
  const guarantees = useWatch({ control, name: 'guarantees' }) ?? [];
  const totalSelected = guarantees.length;

  return (
    <div className={styles.panel}>
      <div className={styles.header}>
        <span className={styles.title}>{S.labelCoverages}</span>
        {totalSelected > 0 && (
          <span className={styles.counter} aria-live="polite">
            {totalSelected} seleccionadas
          </span>
        )}
      </div>
      <p className={styles.helper}>{S.textCoveragesHelper}</p>
      <div className={styles.groups}>
        {GUARANTEE_GROUPS.map((group) => (
          <GuaranteeGroup
            key={group.groupKey}
            group={group}
            control={control}
            setValue={setValue}
            defaultOpen={group.groupKey === 'base'}
          />
        ))}
      </div>
    </div>
  );
}
