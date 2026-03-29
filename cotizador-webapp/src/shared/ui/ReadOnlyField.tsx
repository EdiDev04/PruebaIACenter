import styles from './ReadOnlyField.module.css';

interface ReadOnlyFieldProps {
  readonly label: string;
  readonly value: string;
  readonly hasAutoChip?: boolean;
  readonly 'aria-live'?: 'polite' | 'off' | 'assertive';
}

export function ReadOnlyField({ label, value, hasAutoChip = false, 'aria-live': ariaLive = 'polite' }: ReadOnlyFieldProps) {
  return (
    <div className={styles.wrapper}>
      <span className={styles.label}>{label}</span>
      <div
        className={styles.field}
        aria-live={ariaLive}
        aria-label={`${label}: ${value}${hasAutoChip ? ' (completado automáticamente)' : ''}`}
      >
        <span className={styles.value}>{value}</span>
        {hasAutoChip && (
          <span className={styles.chip} aria-hidden="true">[auto]</span>
        )}
      </div>
    </div>
  );
}
