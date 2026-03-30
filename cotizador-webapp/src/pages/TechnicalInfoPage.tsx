import { useNavigate, useParams } from 'react-router-dom';
import { useState } from 'react';
import { CoverageOptionsForm } from '@/widgets/coverage-options-form';
import { WizardStepNav } from '@/widgets';
import { ToastContainer } from '@/shared/ui';
import type { ToastMessage } from '@/shared/ui';
import styles from './TechnicalInfoPage.module.css';

const FORM_ID = 'coverage-options-form';

export function TechnicalInfoPage() {
  const navigate = useNavigate();
  const { folioNumber } = useParams<{ folioNumber: string }>();
  const folio = folioNumber ?? '';
  const [toasts, setToasts] = useState<ToastMessage[]>([]);
  const [isSaving, setIsSaving] = useState(false);

  const addToast = (message: string, type: ToastMessage['type'] = 'error') =>
    setToasts((prev) => [...prev, { id: crypto.randomUUID(), message, type }]);

  const removeToast = (id: string) =>
    setToasts((prev) => prev.filter((t) => t.id !== id));

  return (
    <div className={styles.page}>
      <div className={styles.intro}>
        <h1 className={styles.title}>Opciones de Cobertura</h1>
        <p className={styles.subtitle}>
          Configure las garantías habilitadas y los parámetros de deducible y coaseguro del folio.
        </p>
      </div>

      <CoverageOptionsForm
        folio={folio}
        formId={FORM_ID}
        onSaveStart={() => setIsSaving(true)}
        onSaveEnd={() => setIsSaving(false)}
        onToastMessage={addToast}
        onNavigateNext={() => navigate(`/quotes/${folio}/results`)}
      />

      <WizardStepNav
        onBack={() => navigate(`/quotes/${folio}/locations`)}
        onSave={() => {}}
        isSaving={isSaving}
        canGoBack
        formId={FORM_ID}
      />

      <ToastContainer toasts={toasts} onClose={removeToast} />
    </div>
  );
}
