import { forwardRef, SelectHTMLAttributes, useId } from 'react';
import styles from './Select.module.css';

export interface SelectOption {
  value: string;
  label: string;
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label: string;
  options: SelectOption[];
  error?: string;
  placeholder?: string;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, options, error, placeholder, id, ...rest }, ref) => {
    const generatedId = useId();
    const selectId = id ?? generatedId;
    const errorId = `${selectId}-error`;

    return (
      <div className={styles.wrapper}>
        <label htmlFor={selectId} className={styles.label}>{label}</label>
        <select
          ref={ref}
          id={selectId}
          className={`${styles.select} ${error ? styles.selectError : ''}`}
          aria-describedby={error ? errorId : undefined}
          aria-invalid={error ? 'true' : undefined}
          {...rest}
        >
          {placeholder && (
            <option value="" disabled>
              {placeholder}
            </option>
          )}
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        {error && (
          <span id={errorId} className={styles.error} role="alert">{error}</span>
        )}
      </div>
    );
  }
);

Select.displayName = 'Select';
