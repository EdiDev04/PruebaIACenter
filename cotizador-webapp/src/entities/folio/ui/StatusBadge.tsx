import styles from './StatusBadge.module.css';

export type QuoteStatus = 'draft' | 'in_progress' | 'calculated' | 'closed';

const STATUS_CONFIG: Record<QuoteStatus, { label: string; className: string }> = {
  draft: { label: 'En borrador', className: 'draft' },
  in_progress: { label: 'En progreso', className: 'inProgress' },
  calculated: { label: 'Calculado', className: 'calculated' },
  closed: { label: 'Cerrado', className: 'closed' },
};

interface StatusBadgeProps {
  readonly status: QuoteStatus;
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const config = STATUS_CONFIG[status] ?? STATUS_CONFIG.draft;
  return (
    <span className={`${styles.badge} ${styles[config.className]}`}>
      <span className={styles.dot} aria-hidden="true" />
      {config.label}
    </span>
  );
}
