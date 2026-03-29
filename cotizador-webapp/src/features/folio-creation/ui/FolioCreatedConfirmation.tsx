import { useNavigate } from 'react-router-dom';
import { FolioNumberBadge, StatusBadge } from '@/entities/folio';
import type { QuoteStatus } from '@/entities/folio';
import { Button } from '@/shared/ui';
import styles from './FolioCreatedConfirmation.module.css';

interface Props {
  readonly folioNumber: string;
  readonly quoteStatus: QuoteStatus;
  readonly createdAt: string;
}

export function FolioCreatedConfirmation({ folioNumber, quoteStatus, createdAt }: Props) {
  const navigate = useNavigate();

  const formattedDate = new Date(createdAt).toLocaleString('es-MX', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });

  return (
    <div className={styles.card}>
      <div className={styles.successIcon}>
        <span className="material-symbols-outlined" aria-hidden="true">check_circle</span>
      </div>
      <h1 className={styles.title}>Folio creado exitosamente</h1>

      <div className={styles.folioSection}>
        <span className={styles.folioLabel}>Número de Folio</span>
        <FolioNumberBadge value={folioNumber} />
      </div>

      <div className={styles.metaRow}>
        <StatusBadge status={quoteStatus} />
        <p className={styles.createdAt}>Creado el {formattedDate} hrs</p>
      </div>

      <hr className={styles.separator} />

      <p className={styles.guideText}>
        Ya tienes tu folio. Ahora captura los datos de la cotización para obtener una propuesta formal.
      </p>

      <Button
        variant="primary"
        onClick={() => navigate(`/quotes/${folioNumber}/general-info`)}
        className={styles.ctaBtn}
      >
        Iniciar captura: Datos Generales →
      </Button>
    </div>
  );
}
