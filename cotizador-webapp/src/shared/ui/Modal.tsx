import { ReactNode } from 'react';
import styles from './Modal.module.css';

interface ModalProps {
  readonly isOpen: boolean;
  readonly title: string;
  readonly children: ReactNode;
  readonly onClose: () => void;
  readonly footer?: ReactNode;
}

export function Modal({ isOpen, title, children, onClose, footer }: ModalProps) {
  if (!isOpen) return null;

  return (
    <div
      className={styles.backdrop}
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <dialog
        open
        className={styles.panel}
        aria-labelledby="modal-title"
        aria-modal="true"
        onKeyDown={(e) => {
          if (e.key === 'Escape') onClose();
        }}
      >
        <header className={styles.header}>
          <h2 id="modal-title" className={styles.title}>{title}</h2>
          <button className={styles.closeBtn} onClick={onClose} aria-label="Cerrar modal">
            <span className="material-symbols-outlined" aria-hidden="true">close</span>
          </button>
        </header>
        <div className={styles.body}>{children}</div>
        {footer && <footer className={styles.footer}>{footer}</footer>}
      </dialog>
    </div>
  );
}
