import type { LocationAlertDto } from '@/entities/quote-state';
import styles from './IncompleteAlerts.module.css';

const FIELD_LABELS: Record<string, string> = {
  zipCode: 'Código Postal',
  'businessLine.fireKey': 'Giro de Negocio (Incendio)',
  constructionType: 'Tipo de Construcción',
  level: 'Número de Pisos',
  guarantees: 'Garantías tarifables',
};

function getFieldLabel(field: string): string {
  return FIELD_LABELS[field] ?? field;
}

interface IncompleteAlertsProps {
  alerts: LocationAlertDto[];
  onEditLocation: () => void;
}

export function IncompleteAlerts({ alerts, onEditLocation }: IncompleteAlertsProps) {

  if (alerts.length === 0) return null;

  return (
    <section className={styles.panel} aria-label="Ubicaciones con datos pendientes">
      <h3 className={styles.title}>
        <span className={styles.icon} aria-hidden="true">⚠</span>
        Ubicaciones con datos pendientes ({alerts.length})
      </h3>
      <ul className={styles.list} role="list">
        {alerts.map((alert) => (
          <li key={alert.index} className={styles.item}>
            <div className={styles.itemHeader}>
              <span className={styles.locationName}>{alert.locationName}</span>
              <button
                type="button"
                className={styles.editLink}
                onClick={onEditLocation}
              >
                Editar ubicación →
              </button>
            </div>
            <div className={styles.fields}>
              <span className={styles.fieldsLabel}>Campos faltantes:</span>
              <div className={styles.chips}>
                {alert.missingFields.map((field) => (
                  <span key={field} className={styles.chip}>
                    {getFieldLabel(field)}
                  </span>
                ))}
              </div>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}
