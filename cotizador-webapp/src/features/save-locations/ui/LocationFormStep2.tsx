import type { UseFormReturn } from 'react-hook-form';
import type { LocationFormValues } from '@/entities/location';
import type { BusinessLineDto } from '@/entities/business-line';
import { BusinessLineSelector } from './BusinessLineSelector';
import { GuaranteesPanel } from './GuaranteesPanel';
import { SAVE_LOCATIONS_STRINGS as S } from '../strings';
import styles from './LocationFormStep2.module.css';

interface Props {
  readonly form: UseFormReturn<LocationFormValues>;
  readonly businessLines: BusinessLineDto[];
}

export function LocationFormStep2({ form, businessLines }: Props) {
  const { control, setValue } = form;

  return (
    <div className={styles.step}>
      <div className={styles.section}>
        <p className={styles.helper}>{S.helperBusinessLine}</p>
        <BusinessLineSelector control={control} options={businessLines} />
      </div>

      <div className={styles.divider} />

      <GuaranteesPanel control={control} setValue={setValue} />
    </div>
  );
}
