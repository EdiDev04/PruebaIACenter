import { Button, Modal } from '@/shared/ui';
import styles from './DeleteLocationModal.module.css';

interface Props {
  readonly locationName: string;
  readonly onConfirm: () => void;
  readonly onCancel: () => void;
}

export function DeleteLocationModal({ locationName, onConfirm, onCancel }: Props) {
  return (
    <Modal
      isOpen
      title="Eliminar ubicación"
      onClose={onCancel}
      footer={
        <div className={styles.footer}>
          <Button variant="ghost" type="button" onClick={onCancel}>
            Cancelar
          </Button>
          <Button variant="primary" type="button" onClick={onConfirm}>
            Eliminar
          </Button>
        </div>
      }
    >
      <p>
        ¿Deseas eliminar la ubicación{' '}
        <strong>&ldquo;{locationName}&rdquo;</strong>? Esta acción no se puede deshacer.
      </p>
    </Modal>
  );
}
