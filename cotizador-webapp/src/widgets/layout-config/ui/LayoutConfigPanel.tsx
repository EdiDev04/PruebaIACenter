import { useCallback, useEffect, useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  useLayoutQuery,
  COLUMN_GROUPS,
  PANEL_STRINGS,
  updateLayoutRequestSchema,
} from '@/entities/layout';
import type { ColumnKey, DisplayMode, UpdateLayoutRequest } from '@/entities/layout';
import { useSaveLayout, SAVE_LAYOUT_STRINGS } from '@/features/save-layout';
import { Button, DisplayModeToggle, ColumnGroupCheckbox } from '@/shared/ui';
import styles from './LayoutConfigPanel.module.css';

const DEFAULT_VISIBLE_COLUMNS: ColumnKey[] = [
  'index',
  'locationName',
  'zipCode',
  'businessLine',
  'validationStatus',
];

const DEFAULT_DISPLAY_MODE: DisplayMode = 'grid';

function isDefaultLayout(displayMode: DisplayMode, visibleColumns: ColumnKey[]): boolean {
  if (displayMode !== DEFAULT_DISPLAY_MODE) return false;
  if (visibleColumns.length !== DEFAULT_VISIBLE_COLUMNS.length) return false;
  const sortedA = [...visibleColumns].sort((a, b) => a.localeCompare(b));
  const sortedB = [...DEFAULT_VISIBLE_COLUMNS].sort((a, b) => a.localeCompare(b));
  return sortedA.every((v, i) => v === sortedB[i]);
}

interface LayoutConfigPanelProps {
  readonly folio: string;
  readonly onClose?: () => void;
}

interface ToastState {
  visible: boolean;
  message: string;
  type: 'success' | 'error' | 'warning';
}

export function LayoutConfigPanel({ folio, onClose }: LayoutConfigPanelProps) {
  const [toast, setToast] = useState<ToastState>({ visible: false, message: '', type: 'success' });
  const [invalidFolioError, setInvalidFolioError] = useState(false);
  const [folioNotFoundError, setFolioNotFoundError] = useState(false);

  const showToast = useCallback((message: string, type: ToastState['type']) => {
    setToast({ visible: true, message, type });
    setTimeout(() => setToast((prev) => ({ ...prev, visible: false })), 3000);
  }, []);

  const {
    data: serverLayout,
    isLoading,
    isError: isQueryError,
    refetch,
  } = useLayoutQuery(folio, {
    onInvalidFolio: () => setInvalidFolioError(true),
    onFolioNotFound: () => {
      setFolioNotFoundError(true);
      showToast(SAVE_LAYOUT_STRINGS.folioNotFoundError, 'error');
    },
  });

  const { mutate, isPending, isConflict, isFolioNotFound, isError: isMutateError, reset: resetMutation } =
    useSaveLayout({
      folio,
      onSuccess: () => showToast(SAVE_LAYOUT_STRINGS.savedSuccess, 'success'),
      onFolioNotFound: () => showToast(SAVE_LAYOUT_STRINGS.folioNotFoundError, 'error'),
    });

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { isDirty },
  } = useForm<UpdateLayoutRequest>({
    resolver: zodResolver(updateLayoutRequestSchema),
    defaultValues: {
      displayMode: DEFAULT_DISPLAY_MODE,
      visibleColumns: DEFAULT_VISIBLE_COLUMNS,
      version: 1,
    },
  });

  useEffect(() => {
    if (serverLayout) {
      reset({
        displayMode: serverLayout.displayMode,
        visibleColumns: serverLayout.visibleColumns,
        version: serverLayout.version,
      });
    }
  }, [serverLayout, reset]);

  const watchedDisplayMode = watch('displayMode');
  const watchedVisibleColumns = watch('visibleColumns');

  const isDefault = isDefaultLayout(watchedDisplayMode, watchedVisibleColumns);

  const handleRestoreDefaults = () => {
    reset({
      displayMode: DEFAULT_DISPLAY_MODE,
      visibleColumns: DEFAULT_VISIBLE_COLUMNS,
      version: serverLayout?.version ?? 1,
    });
    resetMutation();
  };

  const handleReload = () => {
    resetMutation();
    refetch();
  };

  const onSubmit = (data: UpdateLayoutRequest) => {
    mutate(data);
  };

  if (isLoading) {
    return (
      <div
        className={styles.panel}
        aria-label={PANEL_STRINGS.loadingAria}
        aria-busy="true"
      >
        <div className={styles.skeleton} />
        <div className={`${styles.skeleton} ${styles.skeletonShort}`} />
        <div className={`${styles.skeleton} ${styles.skeletonMed}`} />
        <div className={`${styles.skeleton} ${styles.skeletonMed}`} />
      </div>
    );
  }

  if (invalidFolioError) {
    return (
      <div className={styles.panel}>
        <div className={`${styles.alert} ${styles.alertError}`} role="alert">
          {SAVE_LAYOUT_STRINGS.invalidFolioError}
        </div>
      </div>
    );
  }

  if (folioNotFoundError || (isQueryError && !invalidFolioError)) {
    return (
      <div className={styles.panel}>
        <div className={`${styles.alert} ${styles.alertError}`} role="alert">
          {folioNotFoundError
            ? SAVE_LAYOUT_STRINGS.folioNotFoundError
            : SAVE_LAYOUT_STRINGS.networkError}
          <Button
            type="button"
            variant="ghost"
            onClick={handleReload}
            className={styles.alertAction}
          >
            {SAVE_LAYOUT_STRINGS.reload}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.panel} role="dialog" aria-modal="true" aria-label={PANEL_STRINGS.title}>
      {/* Header */}
      <div className={styles.header}>
        <h2 className={styles.title}>{PANEL_STRINGS.title}</h2>
        <span
          className={`${styles.badge} ${isDefault ? styles.badgeDefault : styles.badgeCustom}`}
          aria-label={isDefault ? PANEL_STRINGS.badgeDefault : PANEL_STRINGS.badgeCustom}
        >
          {isDefault ? PANEL_STRINGS.badgeDefault : PANEL_STRINGS.badgeCustom}
        </span>
        {onClose && (
          <button
            type="button"
            onClick={onClose}
            className={styles.closeBtn}
            aria-label="Cerrar panel de configuración"
          >
            <span className="material-symbols-outlined" aria-hidden="true">close</span>
          </button>
        )}
      </div>

      {/* Conflict alert */}
      {isConflict && (
        <div className={`${styles.alert} ${styles.alertWarning}`} role="alert">
          <span>{SAVE_LAYOUT_STRINGS.conflictError}</span>
          <Button
            type="button"
            variant="ghost"
            onClick={handleReload}
            className={styles.alertAction}
          >
            {SAVE_LAYOUT_STRINGS.reload}
          </Button>
        </div>
      )}

      {/* Network error (non-conflict) */}
      {isMutateError && !isConflict && !isFolioNotFound && (
        <div className={`${styles.alert} ${styles.alertError}`} role="alert">
          <span>{SAVE_LAYOUT_STRINGS.networkError}</span>
          <Button
            type="button"
            variant="ghost"
            onClick={() => resetMutation()}
            className={styles.alertAction}
          >
            {SAVE_LAYOUT_STRINGS.retry}
          </Button>
        </div>
      )}

      {/* Toast */}
      {toast.visible && (
        <div
          className={`${styles.toast} ${styles[`toast_${toast.type}`]}`}
          role="status"
          aria-live="polite"
        >
          {toast.message}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        {/* Display mode */}
        <section className={styles.section}>
          <p className={styles.sectionLabel}>{PANEL_STRINGS.displayModeLabel}</p>
          <Controller
            control={control}
            name="displayMode"
            render={({ field }) => (
              <DisplayModeToggle
                value={field.value}
                onChange={field.onChange}
                disabled={isPending || isConflict}
              />
            )}
          />
        </section>

        {/* Columns */}
        <section className={styles.section}>
          <p className={styles.sectionLabel}>
            {PANEL_STRINGS.columnsLabel}{' '}
            <span className={styles.columnCounter} aria-live="polite">
              ({watchedVisibleColumns.length} de 15)
            </span>
          </p>

          <Controller
            control={control}
            name="visibleColumns"
            render={({ field }) => (
              <div className={styles.groupList}>
                {COLUMN_GROUPS.map((group) => (
                  <ColumnGroupCheckbox
                    key={group.label}
                    groupLabel={group.label}
                    columns={group.keys.map((key) => ({ key, label: key }))}
                    selected={field.value}
                    onChange={field.onChange}
                    defaultExpanded={group.defaultExpanded}
                    disabled={isPending || isConflict}
                  />
                ))}
              </div>
            )}
          />
        </section>

        {/* Footer actions */}
        <div className={styles.footer}>
          <Button
            type="button"
            variant="ghost"
            onClick={handleRestoreDefaults}
            disabled={isPending || isConflict || isDefault}
          >
            {SAVE_LAYOUT_STRINGS.restoreDefaults}
          </Button>
          <Button
            type="submit"
            variant="primary"
            isLoading={isPending}
            loadingText={SAVE_LAYOUT_STRINGS.saving}
            disabled={isPending || isConflict || !isDirty}
          >
            {SAVE_LAYOUT_STRINGS.saveButton}
          </Button>
        </div>
      </form>
    </div>
  );
}
