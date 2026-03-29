import { Modal, Button } from '@/shared/ui';
import styles from './VersionConflictModal.module.css';

interface Props {
  isOpen: boolean;
  onReload: () => void;
  onCancel: () => void;
}

export function VersionConflictModal({ isOpen, onReload, onCancel }: Props) {
  return (
    <Modal isOpen={isOpen} onClose={onCancel} title="Conflicto de versión">
      <div className={styles.content}>
        <p className={styles.message}>
          El folio fue modificado por otro proceso. Debes recargar los datos actualizados para continuar.
        </p>
        <div className={styles.actions}>
          <Button variant="ghost" onClick={onCancel}>
            Cancelar
          </Button>
          <Button variant="primary" onClick={onReload}>
            Recargar datos
          </Button>
        </div>
      </div>
    </Modal>
  );
}
