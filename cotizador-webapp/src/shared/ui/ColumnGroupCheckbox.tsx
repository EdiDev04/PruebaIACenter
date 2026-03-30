import { useState } from 'react';
import type { ColumnKey } from '@/entities/layout';
import { COLUMN_LABELS } from '@/entities/layout';
import styles from './ColumnGroupCheckbox.module.css';

interface Column {
  key: ColumnKey;
  label: string;
}

interface ColumnGroupCheckboxProps {
  readonly groupLabel: string;
  readonly columns: Column[];
  readonly selected: ColumnKey[];
  readonly onChange: (cols: ColumnKey[]) => void;
  readonly defaultExpanded?: boolean;
  readonly disabled?: boolean;
  readonly isLastSelectedGroup?: boolean;
}

export function ColumnGroupCheckbox({
  groupLabel,
  columns,
  selected,
  onChange,
  defaultExpanded = false,
  disabled = false,
}: ColumnGroupCheckboxProps) {
  const [expanded, setExpanded] = useState(defaultExpanded);

  const selectedInGroup = columns.filter((c) => selected.includes(c.key));
  const count = selectedInGroup.length;
  const total = columns.length;

  const handleToggle = (key: ColumnKey) => {
    if (selected.includes(key)) {
      if (selected.length === 1) return; // protección último checkbox
      onChange(selected.filter((k) => k !== key));
    } else {
      onChange([...selected, key]);
    }
  };

  const headerId = `group-header-${groupLabel.replace(/\s+/g, '-').toLowerCase()}`;

  return (
    <div className={styles.group}>
      <button
        type="button"
        className={styles.groupHeader}
        onClick={() => setExpanded((prev) => !prev)}
        aria-expanded={expanded}
        aria-controls={`group-body-${headerId}`}
        id={headerId}
      >
        <span className={styles.groupLabel}>{groupLabel}</span>
        <span className={styles.counter} aria-label={`${count} de ${total} seleccionadas`}>
          {count === 0 ? (
            <span className={styles.counterZero}>{count} de {total}</span>
          ) : (
            `${count} de ${total}`
          )}
        </span>
        <span
          className={`material-symbols-outlined ${styles.chevron} ${expanded ? styles.expanded : ''}`}
          aria-hidden="true"
        >
          expand_more
        </span>
      </button>

      {expanded && (
        <ul
          id={`group-body-${headerId}`}
          className={styles.checkboxList}
          role="list"
        >
          {columns.map(({ key }) => {
            const isChecked = selected.includes(key);
            const isProtected = isChecked && selected.length === 1;

            return (
              <li key={key} className={styles.checkboxItem}>
                <label
                  className={`${styles.label} ${disabled ? styles.disabled : ''}`}
                  title={isProtected ? 'Debe haber al menos una columna visible' : undefined}
                >
                  <input
                    type="checkbox"
                    checked={isChecked}
                    disabled={disabled || isProtected}
                    onChange={() => handleToggle(key)}
                    aria-label={COLUMN_LABELS[key]}
                    aria-describedby={isProtected ? `tooltip-${key}` : undefined}
                    className={styles.checkbox}
                  />
                  <span>{COLUMN_LABELS[key]}</span>
                  {isProtected && (
                    <span
                      id={`tooltip-${key}`}
                      role="tooltip"
                      className={styles.tooltip}
                    >
                      Debe haber al menos una columna visible
                    </span>
                  )}
                </label>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
