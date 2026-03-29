import { useEffect, useId } from 'react';
import styles from './Toast.module.css';

export type ToastType = 'error' | 'success' | 'warning' | 'info';

export interface ToastMessage {
  id: string;
  message: string;
  type: ToastType;
}

interface ToastProps {
  readonly message: string;
  readonly type: ToastType;
  readonly onClose: () => void;
  readonly duration?: number;
}

const ICON_MAP: Record<ToastType, string> = {
  error: 'error',
  success: 'check_circle',
  warning: 'warning',
  info: 'info',
};

export function Toast({ message, type, onClose, duration = 5000 }: ToastProps) {
  const id = useId();

  useEffect(() => {
    const timer = setTimeout(onClose, duration);
    return () => clearTimeout(timer);
  }, [onClose, duration]);

  return (
    <div
      role="alert"
      aria-live="assertive"
      aria-atomic="true"
      className={`${styles.toast} ${styles[type]}`}
      id={id}
    >
      <span className="material-symbols-outlined" aria-hidden="true">
        {ICON_MAP[type]}
      </span>
      <span className={styles.message}>{message}</span>
      <button
        onClick={onClose}
        className={styles.closeBtn}
        aria-label="Cerrar notificación"
      >
        <span className="material-symbols-outlined" aria-hidden="true">close</span>
      </button>
    </div>
  );
}

interface ToastContainerProps {
  readonly toasts: ToastMessage[];
  readonly onClose: (id: string) => void;
}

export function ToastContainer({ toasts, onClose }: ToastContainerProps) {
  return (
    <div className={styles.container} aria-label="Notificaciones">
      {toasts.map((toast) => (
        <Toast
          key={toast.id}
          message={toast.message}
          type={toast.type}
          onClose={() => onClose(toast.id)}
        />
      ))}
    </div>
  );
}
