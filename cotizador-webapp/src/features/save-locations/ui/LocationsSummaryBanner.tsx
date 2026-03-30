import type { LocationsSummaryResponse } from '@/entities/location';
import styles from './LocationsSummaryBanner.module.css';

interface Props {
  readonly summary: LocationsSummaryResponse['data'];
  readonly onEdit: (index: number) => void;
}

export function LocationsSummaryBanner({ summary, onEdit }: Props) {
  if (summary.totalIncomplete === 0) return null;

  const incompleteLocations = summary.locations.filter((l) => l.validationStatus === 'incomplete');

  return (
    <output className={styles.banner}>
      <div className={styles.bannerHeader}>
        <span className="material-symbols-outlined" aria-hidden="true" style={{ fontSize: '1.25rem' }}>
          warning
        </span>
        <strong>
          {summary.totalIncomplete === 1
            ? '1 ubicación con datos pendientes'
            : `${summary.totalIncomplete} ubicaciones con datos pendientes`}
        </strong>
      </div>
      {incompleteLocations.length > 0 && (
        <ul className={styles.list} aria-label="Alertas por ubicación">
          {incompleteLocations.map((loc) => (
            <li key={loc.index} className={styles.item}>
              <div className={styles.itemRow}>
                <div>
                  <span className={styles.locationName}>{loc.locationName}</span>
                  {loc.blockingAlerts.length > 0 && (
                    <ul className={styles.alerts}>
                      {loc.blockingAlerts.map((alert) => (
                        <li key={alert} className={styles.alert}>{alert}</li>
                      ))}
                    </ul>
                  )}
                </div>
                <button
                  type="button"
                  className={styles.editBtn}
                  onClick={() => onEdit(loc.index)}
                  aria-label={`Ir a editar ${loc.locationName}`}
                >
                  Ir a editar
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
      <p className={styles.footer}>
        {summary.totalCalculable} de {summary.locations.length} ubicación(es) lista(s) para calcular
      </p>
    </output>
  );
}
