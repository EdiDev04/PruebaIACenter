import styles from './FolioBadge.module.css';

interface FolioBadgeProps {
  readonly folioNumber: string;
}

export function FolioBadge({ folioNumber }: FolioBadgeProps) {
  return (
    <span className={styles.badge} aria-label={`Folio: ${folioNumber}`}>
      {folioNumber}
    </span>
  );
}
