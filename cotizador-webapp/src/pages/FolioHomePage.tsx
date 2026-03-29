import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { CreateFolioButton } from '@/features/folio-creation';
import { FolioSearchInput, FolioSearchButton, useFolioSearch, folioSearchSchema } from '@/features/folio-search';
import type { FolioSearchFormData } from '@/features/folio-search';
import { FolioActionCard } from '@/widgets';
import { ToastContainer } from '@/shared/ui';
import type { ToastMessage } from '@/shared/ui';
import styles from './FolioHomePage.module.css';

export function FolioHomePage() {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);
  const { searchFolio, isLoading: isSearchLoading } = useFolioSearch();

  const addToast = (message: string, type: ToastMessage['type'] = 'error') => {
    setToasts((prev) => [...prev, { id: crypto.randomUUID(), message, type }]);
  };
  const removeToast = (id: string) => setToasts((prev) => prev.filter((t) => t.id !== id));

  const {
    handleSubmit,
    control,
    formState: { errors },
    watch,
  } = useForm<FolioSearchFormData>({
    resolver: zodResolver(folioSearchSchema),
    mode: 'onChange',
  });

  const folioValue = watch('folioNumber', '');
  const hasFormatError = !!errors.folioNumber;

  const onSearch = handleSubmit((data) => {
    searchFolio(data.folioNumber, (msg) => addToast(msg, 'error'));
  });

  return (
    <div className={styles.page}>
      <header className={styles.topBar}>
        <div className={styles.topBarInner}>
          <div className={styles.brand}>
            <span className="material-symbols-outlined" aria-hidden="true">security</span>
            <span className={styles.brandName}>Cotizador de Daños</span>
          </div>
          <div className={styles.userInfo}>
            <div className={styles.avatar} aria-hidden="true">U</div>
          </div>
        </div>
        <div className={styles.topBarDivider} />
      </header>

      <main className={styles.main}>
        <div className={styles.container}>
          <div className={styles.hero}>
            <h1 className={styles.heroTitle}>Panel de Suscripción</h1>
            <p className={styles.heroSubtitle}>
              Gestione riesgos de propiedad con precisión institucional y flujos de trabajo optimizados.
            </p>
          </div>

          <div className={styles.cards}>
            <FolioActionCard
              title="Crear folio nuevo"
              description="Inicia una nueva cotización de seguros de daños a la propiedad"
              icon="add_circle"
            >
              <CreateFolioButton onToastError={(msg) => addToast(msg, 'error')} />
            </FolioActionCard>

            <FolioActionCard
              title="Abrir folio existente"
              description="Retoma una cotización en progreso"
              icon="folder_open"
            >
              <Controller
                name="folioNumber"
                control={control}
                render={({ field }) => (
                  <FolioSearchInput
                    error={errors.folioNumber?.message}
                    name={field.name}
                    ref={field.ref}
                    value={field.value}
                    onBlur={field.onBlur}
                    onChange={(e) => {
                      const digits = e.target.value.replace(/\D/g, '').slice(0, 9);
                      let formatted = '';
                      if (digits.length > 0) {
                        formatted = `DAN-${digits.slice(0, 4)}`;
                        if (digits.length > 4) formatted += `-${digits.slice(4)}`;
                      }
                      field.onChange(formatted);
                    }}
                  />
                )}
              />
              <FolioSearchButton
                onSubmit={onSearch}
                isLoading={isSearchLoading}
                disabled={hasFormatError || !folioValue}
              />
            </FolioActionCard>
          </div>
        </div>
      </main>

      <footer className={styles.footer}>
        <div className={styles.footerInner}>
          <span>© 2026 Cotizador de Daños. Todos los derechos reservados.</span>
        </div>
      </footer>

      <ToastContainer toasts={toasts} onClose={removeToast} />
    </div>
  );
}
