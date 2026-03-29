import { useParams } from 'react-router-dom';
import { useAppSelector } from '@/app/hooks';
import { FolioBadge, StatusBadge } from '@/entities/folio';
import { WizardProgressBar } from '@/shared/ui';
import styles from './WizardHeader.module.css';

const STEPS = ['Datos Generales', 'Ubicaciones', 'Coberturas', 'Resultados'];
const TOTAL_STEPS = 4;

export function WizardHeader() {
  const { folioNumber } = useParams<{ folioNumber: string }>();
  const currentStep = useAppSelector((s) => s.quoteWizard.currentStep);
  const activeFolio = useAppSelector((s) => s.quoteWizard.activeFolio);

  const displayFolio = folioNumber ?? activeFolio ?? '';

  return (
    <header className={styles.header}>
      <div className={styles.topRow}>
        <div className={styles.brand}>
          <span className={styles.brandName}>Cotizador de Daños</span>
          {displayFolio && <FolioBadge folioNumber={displayFolio} />}
          <StatusBadge status="in_progress" />
        </div>
        <div className={styles.progress}>
          <WizardProgressBar currentStep={currentStep} totalSteps={TOTAL_STEPS} />
        </div>
      </div>
      <nav className={styles.stepNav} aria-label="Pasos del wizard">
        {STEPS.map((step, idx) => {
          const stepNum = idx + 1;
          const isActive = stepNum === currentStep;
          return (
            <div
              key={step}
              className={`${styles.stepTab} ${isActive ? styles.activeTab : styles.inactiveTab}`}
              aria-current={isActive ? 'step' : undefined}
            >
              <span className={`${styles.stepNumber} ${isActive ? styles.activeNumber : styles.inactiveNumber}`}>
                {stepNum}
              </span>
              <span className={styles.stepName}>{step}</span>
            </div>
          );
        })}
      </nav>
    </header>
  );
}
