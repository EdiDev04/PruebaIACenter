import styles from './WizardProgressBar.module.css';

interface WizardProgressBarProps {
  readonly currentStep: number;
  readonly totalSteps: number;
}

export function WizardProgressBar({ currentStep, totalSteps }: WizardProgressBarProps) {
  const percentage = Math.round((currentStep / totalSteps) * 100);

  return (
    <div className={styles.wrapper}>
      <span className={styles.label}>Paso {currentStep} de {totalSteps}</span>
      <progress
        className={styles.track}
        value={percentage}
        max={100}
        aria-label={`Progreso: paso ${currentStep} de ${totalSteps}`}
      />
    </div>
  );
}
