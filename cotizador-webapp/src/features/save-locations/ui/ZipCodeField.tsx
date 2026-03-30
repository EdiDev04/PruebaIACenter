import { useEffect, useState, useId } from 'react';
import { useZipCodeQuery } from '@/entities/zip-code';
import type { ZipCodeDto } from '@/entities/zip-code';
import { SAVE_LOCATIONS_STRINGS as S } from '../strings';
import styles from './ZipCodeField.module.css';

interface Props {
  readonly value: string;
  readonly onChange: (val: string) => void;
  readonly onResolved: (data: ZipCodeDto) => void;
  readonly onCleared: () => void;
  readonly error?: string;
}

function useDebounce(value: string, delay: number) {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(t);
  }, [value, delay]);
  return debounced;
}

export function ZipCodeField({ value, onChange, onResolved, onCleared, error }: Props) {
  const inputId = useId();
  const errorId = `${inputId}-error`;
  const debounced = useDebounce(value, 300);

  const { data, isFetching, isError, error: queryError, refetch } = useZipCodeQuery(debounced);

  const isServiceUnavailable =
    isError && (queryError as { type?: string })?.type === 'coreOhsUnavailable';
  const isNotFound = isError && (queryError as { type?: string })?.type === 'zipCodeNotFound';

  useEffect(() => {
    if (data) {
      onResolved(data);
    }
  }, [data, onResolved]);

  useEffect(() => {
    if (!/^\d{5}$/.test(debounced)) {
      onCleared();
    }
  }, [debounced, onCleared]);

  return (
    <div className={styles.wrapper}>
      <label htmlFor={inputId} className={styles.label}>
        {S.labelZipCode}
      </label>
      <div className={styles.inputRow}>
        <input
          id={inputId}
          type="text"
          inputMode="numeric"
          maxLength={5}
          value={value}
          onChange={(e) => onChange(e.target.value.replaceAll(/\D/gu, ''))}
          className={`${styles.input} ${error ? styles.inputError : ''}`}
          aria-describedby={errorId}
          aria-invalid={error ? 'true' : undefined}
          placeholder="00000"
        />
        {isFetching && (
          <span className={`${styles.status} ${styles.resolving}`} aria-live="polite">
            <span className="material-symbols-outlined" style={{ fontSize: '1rem', animation: 'spin 1s linear infinite' }} aria-hidden="true">
              progress_activity
            </span>
            {S.cpResolving}
          </span>
        )}
        {!isFetching && data && (
          <span className={`${styles.status} ${styles.resolved}`} aria-live="polite">
            {S.cpResolved}
          </span>
        )}
        {!isFetching && isServiceUnavailable && (
          <button
            type="button"
            className={styles.retryBtn}
            onClick={() => refetch()}
            aria-label="Reintentar consulta de codigo postal"
          >
            {S.btnRetry}
          </button>
        )}
      </div>
      {isNotFound && (
        <span id={errorId} className={styles.error} aria-live="polite">
          {S.cpNotFound}
        </span>
      )}
      {isServiceUnavailable && (
        <span id={errorId} className={styles.errorCritical} aria-live="polite">
          {S.cpServiceUnavailable}
        </span>
      )}
      {error && !isNotFound && !isServiceUnavailable && (
        <span id={errorId} className={styles.errorCritical} role="alert">
          {error}
        </span>
      )}
    </div>
  );
}
