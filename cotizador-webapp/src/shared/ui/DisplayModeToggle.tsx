import type { DisplayMode } from '@/entities/layout';
import { DISPLAY_MODE_LABELS } from '@/entities/layout';
import styles from './DisplayModeToggle.module.css';

interface DisplayModeToggleProps {
  readonly value: DisplayMode;
  readonly onChange: (mode: DisplayMode) => void;
  readonly disabled?: boolean;
}

export function DisplayModeToggle({ value, onChange, disabled = false }: DisplayModeToggleProps) {
  return (
    <div
      role="group"
      aria-label="Modo de visualización"
      className={styles.wrapper}
    >
      {(['grid', 'list'] as DisplayMode[]).map((mode) => (
        <button
          key={mode}
          type="button"
          role="radio"
          aria-checked={value === mode}
          disabled={disabled}
          onClick={() => onChange(mode)}
          className={`${styles.segment} ${value === mode ? styles.active : ''}`}
        >
          <span className="material-symbols-outlined" aria-hidden="true">
            {mode === 'grid' ? 'grid_view' : 'view_list'}
          </span>
          {DISPLAY_MODE_LABELS[mode]}
        </button>
      ))}
    </div>
  );
}
