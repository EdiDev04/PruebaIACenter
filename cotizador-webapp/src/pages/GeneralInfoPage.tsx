import { useNavigate, useParams } from 'react-router-dom';
import { useState } from 'react';
import { GeneralInfoForm } from '@/features/general-info-form';
import { WizardStepNav } from '@/widgets';
import { ToastContainer } from '@/shared/ui';
import type { ToastMessage } from '@/shared/ui';
import styles from './GeneralInfoPage.module.css';

const FORM_ID = 'general-info-form';

export function GeneralInfoPage() {
  const navigate = useNavigate();
  const { folioNumber } = useParams<{ folioNumber: string }>();
  const [toasts, setToasts] = useState<ToastMessage[]>([]);
  const [isSaving, setIsSaving] = useState(false);

  const addToast = (message: string, type: ToastMessage['type'] = 'error') =>
    setToasts((prev) => [...prev, { id: crypto.randomUUID(), message, type }]);

  const removeToast = (id: string) =>
    setToasts((prev) => prev.filter((t) => t.id !== id));

  const handleSaveSuccess = () => {
    setIsSaving(false);
    navigate(`/quotes/${folioNumber}/locations`);
  };

  const handleSaveError = (msg: string) => {
    setIsSaving(false);
    addToast(msg, 'error');
  };

  return (
    <div className={styles.page}>
      <div className={styles.intro}>
        <h1 className={styles.title}>Datos Generales</h1>
        <p className={styles.subtitle}>
          Complete la información básica del asegurado para comenzar con la cotización.
        </p>
      </div>

      <GeneralInfoForm
        formId={FORM_ID}
        onSaveSuccess={handleSaveSuccess}
        onToastError={handleSaveError}
        onSaveStart={() => setIsSaving(true)}
      />

      <WizardStepNav
        onSave={() => {}}
        isSaving={isSaving}
        canGoBack={false}
        formId={FORM_ID}
      />

      <ToastContainer toasts={toasts} onClose={removeToast} />
    </div>
  );
}
