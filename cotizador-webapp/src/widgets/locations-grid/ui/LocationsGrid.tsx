import { useLocationsQuery, LocationRow } from '@/entities/location';
import styles from './LocationsGrid.module.css';

interface Props {
  readonly folio: string;
  readonly onEdit: (index: number) => void;
  readonly onDelete: (index: number) => void;
}

export function LocationsGrid({ folio, onEdit, onDelete }: Props) {
  const { data, isLoading, isError } = useLocationsQuery(folio);

  if (isLoading) {
    return (
      <div className={styles.state} aria-live="polite">
        <span className="material-symbols-outlined" aria-hidden="true" style={{ fontSize: '2rem' }}>
          progress_activity
        </span>
        <p>Cargando ubicaciones...</p>
      </div>
    );
  }

  if (isError) {
    return (
      <div className={styles.state} role="alert">
        <p>No fue posible cargar las ubicaciones. Intenta de nuevo.</p>
      </div>
    );
  }

  const locations = data?.locations ?? [];

  if (locations.length === 0) {
    return (
      <output className={styles.empty}>
        <span className="material-symbols-outlined" aria-hidden="true" style={{ fontSize: '2.5rem', color: '#9ca3af' }}>
          location_off
        </span>
        <p>No hay ubicaciones registradas. Agrega la primera.</p>
      </output>
    );
  }

  return (
    <div className={styles.tableWrapper}>
      <table className={styles.table} aria-label="Tabla de ubicaciones de riesgo">
        <thead>
          <tr className={styles.headerRow}>
            <th className={`${styles.th} ${styles.thIndex}`} scope="col">#</th>
            <th className={styles.th} scope="col">Nombre</th>
            <th className={styles.th} scope="col">C.P.</th>
            <th className={styles.th} scope="col">Giro</th>
            <th className={styles.th} scope="col">Estado</th>
            <th className={`${styles.th} ${styles.thMenu}`} scope="col">
              <span className="visually-hidden">Acciones</span>
            </th>
          </tr>
        </thead>
        <tbody>
          {locations.map((loc) => (
            <LocationRow
              key={loc.index}
              location={loc}
              onEdit={(l) => onEdit(l.index)}
              onDelete={(l) => onDelete(l.index)}
            />
          ))}
        </tbody>
      </table>
    </div>
  );
}
