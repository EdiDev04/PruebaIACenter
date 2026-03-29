import { useId, forwardRef } from 'react';
import styles from './RadioGroup.module.css';

export interface RadioOption {
  readonly value: string;
  readonly label: string;
}

interface RadioGroupProps {
  readonly legend: string;
  readonly name: string;
  readonly options: RadioOption[];
  readonly value?: string;
  readonly onChange?: (value: string) => void;
  readonly error?: string;
}

export const RadioGroup = forwardRef<HTMLFieldSetElement, RadioGroupProps>(
  ({ legend, name, options, value, onChange, error }, ref) => {
    const baseId = useId();
    const errorId = `${baseId}-error`;

    return (
      <fieldset ref={ref} className={styles.fieldset} aria-describedby={error ? errorId : undefined}>
        <legend className={styles.legend}>{legend}</legend>
        <div className={styles.options}>
          {options.map((option) => {
            const optId = `${baseId}-${option.value}`;
            return (
              <label key={option.value} htmlFor={optId} className={styles.optionLabel}>
                <input
                  id={optId}
                  type="radio"
                  name={name}
                  value={option.value}
                  checked={value === option.value}
                  onChange={() => onChange?.(option.value)}
                  className={styles.radio}
                />
                <span className={styles.optionText}>{option.label}</span>
              </label>
            );
          })}
        </div>
        {error && (
          <span id={errorId} className={styles.error} role="alert">
            {error}
          </span>
        )}
      </fieldset>
    );
  }
);

RadioGroup.displayName = 'RadioGroup';
