import { Button } from '@/shared/ui';
import styles from './WizardStepNav.module.css';

interface Props {
  onBack?: () => void;
  onSave: () => void;
  isSaving: boolean;
  canGoBack?: boolean;
  formId?: string;
}

export function WizardStepNav({ onBack, onSave, isSaving, canGoBack = false, formId }: Props) {
  return (
    <footer className={styles.nav}>
      <div className={styles.inner}>
        {canGoBack && onBack && (
          <Button variant="ghost" type="button" onClick={onBack} disabled={isSaving}>
            ← Anterior
          </Button>
        )}
        <Button
          variant="primary"
          type={formId ? 'submit' : 'button'}
          form={formId}
          isLoading={isSaving}
          loadingText="Guardando..."
          onClick={formId ? undefined : onSave}
          className={styles.saveBtn}
        >
          Guardar y continuar →
        </Button>
      </div>
    </footer>
  );
}
