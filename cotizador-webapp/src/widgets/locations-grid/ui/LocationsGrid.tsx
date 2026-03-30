import type { ReactNode } from 'react';
import { useLocationsQuery, LocationStatusBadge } from '@/entities/location';
import type { LocationDto } from '@/entities/location';
import { useLayoutQuery, COLUMN_LABELS } from '@/entities/layout';
import type { ColumnKey } from '@/entities/layout';
import styles from './LocationsGrid.module.css';

const DEFAULT_VISIBLE_COLUMNS: ColumnKey[] = [
  'index', 'locationName', 'zipCode', 'businessLine', 'validationStatus',
];

function getCellValue(loc: LocationDto, col: ColumnKey): ReactNode {
  switch (col) {
    case 'index':           return loc.index;
    case 'locationName':    return <strong>{loc.locationName}</strong>;
    case 'address':         return loc.address || '–';
    case 'zipCode':         return loc.zipCode || '–';
    case 'state':           return loc.state || '–';
    case 'municipality':    return loc.municipality || '–';
    case 'neighborhood':    return loc.neighborhood || '–';
    case 'city':            return loc.city || '–';
    case 'constructionType':return loc.constructionType || '–';
    case 'level':           return loc.level ?? '–';
    case 'constructionYear':return loc.constructionYear ?? '–';
    case 'businessLine':    return loc.locationBusinessLine?.description || '–';
    case 'guarantees':      return `${loc.guarantees.length} cobertura${loc.guarantees.length !== 1 ? 's' : ''}`;
    case 'catZone':         return loc.catZone || '–';
    case 'validationStatus':return <LocationStatusBadge status={loc.validationStatus} />;
    default:                return '–';
  }
}

interface Props {
  readonly folio: string;
  readonly onEdit: (index: number) => void;
  readonly onDelete: (index: number) => void;
}

export function LocationsGrid({ folio, onEdit, onDelete }: Props) {
  const { data, isLoading, isError } = useLocationsQuery(folio);
  const { data: layout } = useLayoutQuery(folio);

  const displayMode = layout?.displayMode ?? 'grid';
  const visibleColumns = layout?.visibleColumns ?? DEFAULT_VISIBLE_COLUMNS;

  if (isLoading) {
    return (
      <div className={styles.state} aria-live="polite">
        <span className="material-symbols-outlined" aria-hidden="true" style={{ fontSize: '2rem' }}>progress_activity</span>
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
      <div className={styles.empty}>
        <span className="material-symbols-outlined" aria-hidden="true" style={{ fontSize: '2.5rem', color: '#9ca3af' }}>location_off</span>
        <p>No hay ubicaciones registradas. Agrega la primera.</p>
      </div>
    );
  }

  if (displayMode === 'grid') {
    return (
      <div className={styles.gridCards} aria-label="Tarjetas de ubicaciones de riesgo">
        {locations.map((loc) => (
          <article key={loc.index} className={styles.card}>
            <header className={styles.cardHeader}>
              <span className={styles.cardIndex}>#{loc.index}</span>
              <LocationStatusBadge status={loc.validationStatus} />
            </header>
            <h3 className={styles.cardTitle}>{loc.locationName}</h3>
            <dl className={styles.cardMeta}>
              {visibleColumns
                .filter((col) => col !== 'index' && col !== 'locationName' && col !== 'validationStatus')
                .map((col) => (
                  <div key={col} className={styles.cardField}>
                    <dt className={styles.cardLabel}>{COLUMN_LABELS[col]}</dt>
                    <dd className={styles.cardValue}>{getCellValue(loc, col)}</dd>
                  </div>
                ))}
            </dl>
            <footer className={styles.cardFooter}>
              <button
                className={styles.actionBtn}
                aria-label={`Editar ${loc.locationName}`}
                title="Editar"
                onClick={() => onEdit(loc.index)}
              >
                <span className="material-symbols-outlined" aria-hidden="true">edit</span>
                Editar
              </button>
              <button
                className={`${styles.actionBtn} ${styles.actionBtnDanger}`}
                aria-label={`Eliminar ${loc.locationName}`}
                title="Eliminar"
                onClick={() => onDelete(loc.index)}
              >
                <span className="material-symbols-outlined" aria-hidden="true">delete</span>
                Eliminar
              </button>
            </footer>
          </article>
        ))}
      </div>
    );
  }

  // list mode — tabla dinámica
  return (
    <div className={styles.tableWrapper}>
      <table className={styles.table} aria-label="Tabla de ubicaciones de riesgo">
        <thead>
          <tr className={styles.headerRow}>
            {visibleColumns.map((col) => (
              <th
                key={col}
                scope="col"
                className={`${styles.th} ${col === 'index' ? styles.thIndex : ''}`}
              >
                {COLUMN_LABELS[col]}
              </th>
            ))}
            <th className={`${styles.th} ${styles.thMenu}`} scope="col">
              <span className="visually-hidden">Acciones</span>
            </th>
          </tr>
        </thead>
        <tbody>
          {locations.map((loc) => (
            <tr key={loc.index} className={styles.row}>
              {visibleColumns.map((col) => (
                <td
                  key={col}
                  className={`${styles.cell} ${col === 'index' ? styles.indexCell : ''} ${col === 'locationName' ? styles.nameCell : ''}`}
                >
                  {getCellValue(loc, col)}
                </td>
              ))}
              <td className={`${styles.cell} ${styles.actionsCell}`}>
                <button
                  className={styles.actionBtn}
                  aria-label={`Editar ${loc.locationName}`}
                  title="Editar"
                  onClick={() => onEdit(loc.index)}
                >
                  <span className="material-symbols-outlined" aria-hidden="true">edit</span>
                </button>
                <button
                  className={`${styles.actionBtn} ${styles.actionBtnDanger}`}
                  aria-label={`Eliminar ${loc.locationName}`}
                  title="Eliminar"
                  onClick={() => onDelete(loc.index)}
                >
                  <span className="material-symbols-outlined" aria-hidden="true">delete</span>
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
