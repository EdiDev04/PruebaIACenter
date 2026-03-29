import { ButtonHTMLAttributes } from 'react';
import styles from './Button.module.css';

type ButtonVariant = 'primary' | 'secondary' | 'ghost';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  readonly variant?: ButtonVariant;
  readonly isLoading?: boolean;
  readonly loadingText?: string;
}

export function Button({
  variant = 'primary',
  isLoading = false,
  loadingText,
  children,
  disabled,
  className = '',
  ...rest
}: ButtonProps) {
  return (
    <button
      {...rest}
      disabled={disabled || isLoading}
      className={`${styles.button} ${styles[variant]} ${className}`}
    >
      {isLoading ? (
        <>
          <span className={styles.spinner} aria-hidden="true" />
          <span>{loadingText ?? 'Cargando...'}</span>
        </>
      ) : (
        children
      )}
    </button>
  );
}
