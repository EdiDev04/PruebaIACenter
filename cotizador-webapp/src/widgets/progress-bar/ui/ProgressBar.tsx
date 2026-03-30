import { useNavigate, useParams } from 'react-router-dom';
import type { ProgressDto } from '@/entities/quote-state';
import styles from './ProgressBar.module.css';

interface Step {
  key: keyof ProgressDto;
  label: string;
  path: string;
}

const STEPS: Step[] = [
  { key: 'generalInfo', label: 'Datos Generales', path: 'general-info' },
  { key: 'layoutConfiguration', label: 'Layout', path: 'locations' },
  { key: 'locations', label: 'Ubicaciones', path: 'locations' },
  { key: 'coverageOptions', label: 'Opciones de Cobertura', path: 'technical-info' },
];

interface ProgressBarProps {
  readonly progress: ProgressDto;
  readonly currentStep?: string;
}

export function ProgressBar({ progress, currentStep }: ProgressBarProps) {
  const navigate = useNavigate();
  const { folioNumber } = useParams<{ folioNumber: string }>();

  const handleStepClick = (path: string) => {
    if (folioNumber) navigate(`/quotes/${folioNumber}/${path}`);
  };

  return (
    <nav className={styles.bar} aria-label="Progreso de cotización">
      {STEPS.map((step, idx) => {
        const completed = progress[step.key];
        const isActive = currentStep === step.key;
        return (
          <div key={step.key} className={styles.stepWrapper}>
            <button
              type="button"
              className={`${styles.step} ${completed ? styles.completed : styles.pending} ${isActive ? styles.active : ''}`}
              onClick={() => handleStepClick(step.path)}
              aria-label={`${step.label}: ${completed ? 'completado' : 'pendiente'}`}
              aria-current={isActive ? 'step' : undefined}
            >
              <span
                className={`${styles.circle} ${completed ? styles.circleCompleted : styles.circlePending}`}
                aria-hidden="true"
              >
                {completed ? '✓' : ''}
              </span>
              <span className={styles.label}>{step.label}</span>
            </button>
            {idx < STEPS.length - 1 && (
              <div
                className={`${styles.connector} ${completed ? styles.connectorCompleted : styles.connectorPending}`}
                aria-hidden="true"
              />
            )}
          </div>
        );
      })}
    </nav>
  );
}
