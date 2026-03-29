import { forwardRef, useId, useState, useRef, useEffect } from 'react';
import styles from './ComboBox.module.css';

export interface ComboBoxOption {
  value: string;
  label: string;
  [key: string]: string;
}

interface ComboBoxProps {
  readonly label: string;
  readonly options: ComboBoxOption[];
  readonly value?: string;
  readonly onSelect: (option: ComboBoxOption) => void;
  readonly error?: string;
  readonly displayValue?: (option: ComboBoxOption | null) => string;
  readonly placeholder?: string;
}

export const ComboBox = forwardRef<HTMLInputElement, ComboBoxProps>(
  ({ label, options, value, onSelect, error, displayValue, placeholder }, ref) => {
    const generatedId = useId();
    const listId = `${generatedId}-list`;
    const errorId = `${generatedId}-error`;

    const [query, setQuery] = useState('');
    const [open, setOpen] = useState(false);
    const containerRef = useRef<HTMLDivElement>(null);

    const selectedOption = options.find((o) => o.value === value) ?? null;

    const filtered = query.trim() === ''
      ? options
      : options.filter((o) =>
          o.label.toLowerCase().includes(query.toLowerCase())
        );

    useEffect(() => {
      function handleClickOutside(e: MouseEvent) {
        if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
          setOpen(false);
        }
      }
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const resolvedDisplayValue = displayValue ? displayValue(selectedOption) : (selectedOption?.label ?? '');
    const inputDisplayValue = open ? query : resolvedDisplayValue;

    return (
      <div className={styles.wrapper} ref={containerRef}>
        <label htmlFor={generatedId} className={styles.label}>{label}</label>
        <div className={`${styles.inputWrapper} ${error ? styles.inputWrapperError : ''}`}>
          <input
            ref={ref}
            id={generatedId}
            type="text"
            role="combobox"
            aria-expanded={open}
            aria-controls={listId}
            aria-autocomplete="list"
            aria-describedby={error ? errorId : undefined}
            aria-invalid={error ? 'true' : undefined}
            value={inputDisplayValue}
            placeholder={placeholder}
            onChange={(e) => {
              setQuery(e.target.value);
              setOpen(true);
            }}
            onFocus={() => {
              setQuery('');
              setOpen(true);
            }}
            className={styles.input}
          />
          <span className="material-symbols-outlined" aria-hidden="true" style={{ pointerEvents: 'none', color: 'var(--color-on-surface-variant)', position: 'absolute', right: '0.5rem', top: '50%', transform: 'translateY(-50%)' }}>
            search
          </span>
        </div>
        {open && filtered.length > 0 && (
          <div id={listId} role="listbox" className={styles.dropdown}>
            {filtered.map((option) => (
              <div
                key={option.value}
                role="option"
                aria-selected={option.value === value}
                className={`${styles.option} ${option.value === value ? styles.optionSelected : ''}`}
                onMouseDown={(e) => {
                  e.preventDefault();
                  onSelect(option);
                  setQuery('');
                  setOpen(false);
                }}
              >
                {option.label}
              </div>
            ))}
          </div>
        )}
        {error && (
          <span id={errorId} className={styles.error} role="alert">{error}</span>
        )}
      </div>
    );
  }
);

ComboBox.displayName = 'ComboBox';
