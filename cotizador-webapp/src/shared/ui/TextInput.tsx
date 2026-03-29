import { forwardRef, InputHTMLAttributes, useId } from 'react';
import styles from './TextInput.module.css';

interface TextInputProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
  helperText?: string;
}

export const TextInput = forwardRef<HTMLInputElement, TextInputProps>(
  ({ label, error, helperText, id, className = '', ...rest }, ref) => {
    const generatedId = useId();
    const inputId = id ?? generatedId;
    const errorId = `${inputId}-error`;
    const helperId = `${inputId}-helper`;

    return (
      <div className={`${styles.wrapper} ${className}`}>
        <label htmlFor={inputId} className={styles.label}>
          {label}
        </label>
        <input
          ref={ref}
          id={inputId}
          className={`${styles.input} ${error ? styles.inputError : ''}`}
          aria-describedby={
            [error ? errorId : '', helperText ? helperId : ''].filter(Boolean).join(' ') || undefined
          }
          aria-invalid={error ? 'true' : undefined}
          {...rest}
        />
        {error && (
          <span id={errorId} className={styles.error} role="alert">
            {error}
          </span>
        )}
        {helperText && !error && (
          <span id={helperId} className={styles.helper}>
            {helperText}
          </span>
        )}
      </div>
    );
  }
);

TextInput.displayName = 'TextInput';
