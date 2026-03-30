import { useNavigate, useParams, useLocation } from 'react-router-dom';
import type { ProgressDto } from '@/entities/quote-state';
import styles from './ProgressBar.module.css';

interface Step {
  key: string;
  label: string;
  path: string;
  getCompleted: (progress: ProgressDto, hasCalculation: boolean) => boolean;
}

const STEPS: Step[] = [
  {
    key: 'generalInfo',
    label: 'Datos Generales',
    path: 'general-info',
    getCompleted: (p) => p.generalInfo,
  },
  {
    key: 'locations',
    label: 'Ubicaciones',
    path: 'locations',
    getCompleted: (p) => p.locations,
  },
  {
    key: 'coverageOptions',
    label: 'Coberturas',
    path: 'technical-info',
    getCompleted: (p) => p.coverageOptions,
  },
  {
    key: 'results',
    label: 'Resultados',
    path: 'results',
    getCompleted: (_p, hasCalculation) => hasCalculation,
  },
];

/** Map URL path segment → step key */
const PATH_TO_STEP: Record<string, string> = {
  'general-info': 'generalInfo',
  locations: 'locations',
  'technical-info': 'coverageOptions',
  results: 'results',
};

interface ProgressBarProps {
  readonly progress: ProgressDto;
  readonly hasCalculation?: boolean;
}

export function ProgressBar({ progress, hasCalculation = false }: ProgressBarProps) {
  const navigate = useNavigate();
  const { folioNumber } = useParams<{ folioNumber: string }>();
  const { pathname } = useLocation();

  const parts = pathname.split('/');
  const pathSegment = parts[parts.length - 1] ?? '';
  const activeKey = PATH_TO_STEP[pathSegment] ?? '';

  const handleStepClick = (path: string) => {
    if (folioNumber) navigate(`/quotes/${folioNumber}/${path}`);
  };

  return (
    <nav className={styles.bar} aria-label="Progreso de cotización">
      {STEPS.map((step, idx) => {
        const completed = step.getCompleted(progress, hasCalculation);
        const isActive = activeKey === step.key;
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
                {completed ? '✓' : idx + 1}
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
