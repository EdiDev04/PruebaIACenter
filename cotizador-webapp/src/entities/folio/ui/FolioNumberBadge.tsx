import { useState } from 'react';
import styles from './FolioNumberBadge.module.css';

interface FolioNumberBadgeProps {
  readonly value: string;
}

export function FolioNumberBadge({ value }: FolioNumberBadgeProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    await navigator.clipboard.writeText(value);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className={styles.wrapper} aria-label={`Número de folio: ${value}`}>
      <span className={styles.number}>{value}</span>
      <button
        type="button"
        onClick={handleCopy}
        className={styles.copyBtn}
        aria-label={copied ? 'Copiado' : 'Copiar número de folio'}
        title={copied ? 'Copiado' : 'Copiar al portapapeles'}
      >
        <span className="material-symbols-outlined" aria-hidden="true">
          {copied ? 'check' : 'content_copy'}
        </span>
      </button>
    </div>
  );
}
