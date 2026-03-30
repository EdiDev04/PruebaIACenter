import type { LocationDto } from '../model/types';
import { LocationStatusBadge } from './LocationStatusBadge';
import styles from './LocationRow.module.css';

interface Props {
  readonly location: LocationDto;
  readonly onEdit: (location: LocationDto) => void;
  readonly onDelete: (location: LocationDto) => void;
}

export function LocationRow({ location, onEdit, onDelete }: Props) {
  return (
    <tr className={styles.row}>
      <td className={`${styles.cell} ${styles.indexCell}`}>{location.index}</td>
      <td className={`${styles.cell} ${styles.nameCell}`}>{location.locationName}</td>
      <td className={styles.cell}>{location.zipCode || '–'}</td>
      <td className={styles.cell}>{location.locationBusinessLine?.description || '–'}</td>
      <td className={styles.cell}>
        <LocationStatusBadge status={location.validationStatus} />
      </td>
      <td className={`${styles.cell} ${styles.actionsCell}`}>
        <button
          className={styles.actionBtn}
          aria-label={`Editar ${location.locationName}`}
          title="Editar"
          onClick={() => onEdit(location)}
        >
          <span className="material-symbols-outlined" aria-hidden="true">edit</span>
        </button>
        <button
          className={`${styles.actionBtn} ${styles.actionBtnDanger}`}
          aria-label={`Eliminar ${location.locationName}`}
          title="Eliminar"
          onClick={() => onDelete(location)}
        >
          <span className="material-symbols-outlined" aria-hidden="true">delete</span>
        </button>
      </td>
    </tr>
  );
}
