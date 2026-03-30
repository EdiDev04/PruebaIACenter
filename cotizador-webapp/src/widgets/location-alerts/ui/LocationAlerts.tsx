import { useNavigate } from 'react-router-dom';
import type { LocationAlertDto } from '@/entities/quote-state';
import styles from './LocationAlerts.module.css';

const FIELD_LABELS: Record<string, string> = {
  zipCode: 'Código postal',
  'businessLine.fireKey': 'Giro de negocio (clave incendio)',
  'businessLine.earthquakeKey': 'Giro de negocio (clave sismo)',
  constructionType: 'Tipo de construcción',
  numberOfFloors: 'Número de pisos',
  'insuredValues.buildingValue': 'Valor del inmueble',
  'insuredValues.contentsValue': 'Valor de contenidos',
};

function getFieldLabel(field: string): string {
  return FIELD_LABELS[field] ?? field;
}

interface LocationAlertsProps {
  readonly alerts: LocationAlertDto[];
  readonly folio: string;
  readonly calculable: number;
  readonly total: number;
}

export function LocationAlerts({ alerts, folio, calculable, total }: LocationAlertsProps) {
  const navigate = useNavigate();

  if (total === 0) {
    return (
      <section className={styles.panel} aria-label="Estado de ubicaciones">
        <p className={styles.emptyState}>
          No hay ubicaciones registradas. Agregue una ubicación para continuar.
        </p>
      </section>
    );
  }

  if (alerts.length === 0) {
    return (
      <section className={styles.panelSuccess} aria-label="Estado de ubicaciones">
        <span className={styles.successIcon} aria-hidden="true">✓</span>
        <p className={styles.successMessage}>
          Todas las ubicaciones están completas y listas para calcular.
        </p>
      </section>
    );
  }

  return (
    <section className={styles.panel} aria-label="Ubicaciones con datos pendientes">
      <h3 className={styles.panelTitle}>Ubicaciones con datos pendientes</h3>
      <ul className={styles.alertList} role="list">
        {alerts.map((alert) => (
          <li key={alert.index} className={styles.alertItem}>
            <div className={styles.alertHeader}>
              <span className={styles.alertIcon} aria-hidden="true">⚠</span>
              <span className={styles.alertName}>{alert.locationName}</span>
            </div>
            <div className={styles.missingFields}>
              <span className={styles.missingLabel}>Campos faltantes:</span>
              <ul className={styles.chipList} role="list">
                {alert.missingFields.map((field) => (
                  <li key={field} className={styles.chip}>
                    {getFieldLabel(field)}
                  </li>
                ))}
              </ul>
            </div>
            <button
              type="button"
              className={styles.editButton}
              onClick={() => navigate(`/quotes/${folio}/locations?edit=${alert.index}`)}
              aria-label={`Ir a editar ubicación ${alert.locationName}`}
            >
              Ir a editar
            </button>
          </li>
        ))}
      </ul>
      <p className={styles.summary} aria-live="polite">
        {calculable} de {total} ubicación(es) lista(s) para calcular
      </p>
    </section>
  );
}
