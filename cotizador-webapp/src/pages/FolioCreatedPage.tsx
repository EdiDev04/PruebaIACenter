import { useLocation, useNavigate } from 'react-router-dom';
import { useEffect } from 'react';
import { FolioCreatedConfirmation } from '@/features/folio-creation';
import styles from './FolioCreatedPage.module.css';

interface LocationState {
  fromCreation?: boolean;
  folio?: {
    folioNumber: string;
    quoteStatus: 'draft' | 'in_progress' | 'calculated' | 'closed';
    version: number;
    metadata: {
      createdAt: string;
      lastWizardStep: number;
    };
  };
}

export function FolioCreatedPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const state = location.state as LocationState | null;

  useEffect(() => {
    if (!state?.folio) {
      navigate('/', { replace: true });
    }
  }, [state, navigate]);

  if (!state?.folio) return null;

  const { folioNumber, quoteStatus, metadata } = state.folio;

  return (
    <div className={styles.page}>
      <header className={styles.topBar}>
        <nav className={styles.topBarInner}>
          <span className={styles.brandName}>Cotizador de Daños</span>
        </nav>
      </header>
      <main className={styles.main}>
        <FolioCreatedConfirmation
          folioNumber={folioNumber}
          quoteStatus={quoteStatus}
          createdAt={metadata.createdAt}
        />
      </main>
    </div>
  );
}
