import { Button } from '@/shared/ui';
import styles from './FolioSearchButton.module.css';

interface Props {
  readonly onSubmit: () => void;
  readonly isLoading: boolean;
  readonly disabled?: boolean;
}

export function FolioSearchButton({ onSubmit, isLoading, disabled }: Props) {
  return (
    <Button
      variant="secondary"
      isLoading={isLoading}
      loadingText="Buscando..."
      onClick={onSubmit}
      disabled={disabled || isLoading}
      className={styles.btn}
    >
      🔍 Abrir folio
    </Button>
  );
}
