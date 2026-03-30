import styles from './AutoResolvedFields.module.css';

interface Props {
  readonly state?: string;
  readonly municipality?: string;
  readonly neighborhood?: string;
  readonly catZone?: string;
}

interface AutoFieldProps {
  readonly label: string;
  readonly value?: string;
}

function AutoField({ label, value }: AutoFieldProps) {
  return (
    <div className={styles.field}>
      <span className={styles.fieldLabel}>{label}</span>
      <span className={styles.fieldValue}>
        <span className={styles.chip} aria-hidden="true">auto</span>
        {value ? (
          <span>{value}</span>
        ) : (
          <span className={styles.empty} aria-label={`${label} no resuelto`}>–</span>
        )}
      </span>
    </div>
  );
}

export function AutoResolvedFields({ state, municipality, neighborhood, catZone }: Props) {
  return (
    <div className={styles.container} aria-live="polite" aria-label="Datos resueltos automáticamente desde código postal">
      <p className={styles.heading}>Datos resueltos automáticamente</p>
      <div className={styles.grid}>
        <AutoField label="Estado" value={state} />
        <AutoField label="Municipio" value={municipality} />
        <AutoField label="Colonia" value={neighborhood} />
        <div className={styles.field}>
          <span className={styles.fieldLabel}>Zona catastrófica</span>
          <span className={styles.fieldValue}>
            <span className={styles.chip} aria-hidden="true">auto</span>
            {catZone ? (
              <span className={styles.catZoneBadge} aria-label={`Zona catastrófica ${catZone}`}>
                Zona {catZone}
              </span>
            ) : (
              <span className={styles.empty}>–</span>
            )}
          </span>
        </div>
      </div>
    </div>
  );
}
