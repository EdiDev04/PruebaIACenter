import { useParams } from 'react-router-dom';
import { useAppSelector } from '@/app/hooks';
import { FolioBadge, StatusBadge } from '@/entities/folio';
import { useQuoteStateQuery } from '@/entities/quote-state';
import styles from './WizardHeader.module.css';

export function WizardHeader() {
  const { folioNumber } = useParams<{ folioNumber: string }>();
  const activeFolio = useAppSelector((s) => s.quoteWizard.activeFolio);

  const displayFolio = folioNumber ?? activeFolio ?? '';

  const { data: quoteState } = useQuoteStateQuery(displayFolio);
  const status = quoteState?.quoteStatus ?? 'draft';

  return (
    <header className={styles.header}>
      <div className={styles.topRow}>
        <div className={styles.brand}>
          <span className={styles.brandName}>Cotizador de Daños</span>
          {displayFolio && <FolioBadge folioNumber={displayFolio} />}
          <StatusBadge status={status} />
        </div>
      </div>
    </header>
  );
}
