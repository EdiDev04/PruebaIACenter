import { useId } from 'react';
import type { GuaranteeDto } from '../model/types';
import styles from './GuaranteeCheckboxGroup.module.css';

const CATEGORY_LABELS: Record<string, string> = {
  fire: 'Coberturas de Incendio',
  cat: 'Catástrofes',
  additional: 'Coberturas Complementarias',
  special: 'Coberturas Especiales',
};

const RECOMMENDED_KEYS = ['building_fire', 'contents_fire'];

interface Props {
  readonly category: string;
  readonly guarantees: GuaranteeDto[];
  readonly selectedKeys: string[];
  readonly onToggle: (key: string, checked: boolean) => void;
  readonly onSelectAll: (category: string, keys: string[]) => void;
  readonly disabled?: boolean;
}

export function GuaranteeCheckboxGroup({
  category,
  guarantees,
  selectedKeys,
  onToggle,
  onSelectAll,
  disabled = false,
}: Props) {
  const baseId = useId();
  const groupLabel = CATEGORY_LABELS[category] ?? category;
  const selectedCount = guarantees.filter((g) => selectedKeys.includes(g.key)).length;
  const allSelected = selectedCount === guarantees.length;

  return (
    <div className={styles.group}>
      <div className={styles.header}>
        <span className={styles.title}>
          {groupLabel}
          <span className={styles.count} aria-label={`${selectedCount} de ${guarantees.length} seleccionadas`}>
            {' '}({selectedCount}/{guarantees.length})
          </span>
        </span>
        <button
          type="button"
          className={styles.selectAll}
          onClick={() => onSelectAll(category, guarantees.map((g) => g.key))}
          disabled={disabled || allSelected}
          aria-label={`Seleccionar todas las garantías de ${groupLabel}`}
        >
          Seleccionar todas
        </button>
      </div>
      <ul className={styles.list} role="group" aria-label={groupLabel}>
        {guarantees.map((guarantee) => {
          const checked = selectedKeys.includes(guarantee.key);
          const checkId = `${baseId}-${guarantee.key}`;
          const descId = `${baseId}-${guarantee.key}-desc`;
          const isRecommended = RECOMMENDED_KEYS.includes(guarantee.key);

          return (
            <li key={guarantee.key} className={styles.item}>
              <input
                type="checkbox"
                id={checkId}
                checked={checked}
                onChange={(e) => onToggle(guarantee.key, e.target.checked)}
                disabled={disabled}
                aria-describedby={descId}
                className={styles.checkbox}
              />
              <label htmlFor={checkId} className={styles.label}>
                <span className={styles.labelText}>{guarantee.name}</span>
                {isRecommended && (
                  <span className={styles.badgeRecommended} aria-label="Cobertura recomendada">
                    recomendado
                  </span>
                )}
                {guarantee.requiresInsuredAmount && (
                  <span className={styles.badgeInsuredAmount}>
                    Requiere suma asegurada
                  </span>
                )}
              </label>
              <span
                id={descId}
                className={styles.tooltip}
                title={guarantee.description}
                aria-label={guarantee.description}
                tabIndex={0}
              >
                <span className="material-symbols-outlined" aria-hidden="true">
                  help_outline
                </span>
              </span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
