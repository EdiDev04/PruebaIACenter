import styles from './PlaceholderPage.module.css';

interface Props {
  label: string;
  stepNumber?: number;
}

export function PlaceholderPage({ label, stepNumber }: Props) {
  return (
    <div className={styles.page}>
      {stepNumber !== undefined && (
        <span className={styles.step}>Paso {stepNumber}</span>
      )}
      <h1 className={styles.title}>{label}</h1>
      <p className={styles.subtitle}>Esta sección estará disponible próximamente.</p>
    </div>
  );
}
