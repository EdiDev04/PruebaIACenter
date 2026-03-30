import styles from './LocationStatusBadge.module.css';

interface Props {
  readonly status: 'calculable' | 'incomplete';
}

export function LocationStatusBadge({ status }: Props) {
  const isCalculable = status === 'calculable';
  return (
    <span
      className={`${styles.badge} ${isCalculable ? styles.calculable : styles.incomplete}`}
      aria-label={isCalculable ? 'Calculable' : 'Datos pendientes'}
    >
      <span
        className={`${styles.dot} ${isCalculable ? styles.dotCalculable : styles.dotIncomplete}`}
        aria-hidden="true"
      />
      {isCalculable ? 'Calculable' : 'Datos pendientes'}
    </span>
  );
}
