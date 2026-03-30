import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import type { LocationDto } from '@/entities/location';
import { useLocationsQuery, useLocationsSummaryQuery } from '@/entities/location';
import { LocationForm } from '@/features/save-locations/ui/LocationForm';
import { useDeleteLocation } from '@/features/delete-location/model/useDeleteLocation';
import { DeleteLocationModal } from '@/features/delete-location/ui/DeleteLocationModal';
import { LocationsGrid } from '@/widgets/locations-grid';
import { LayoutConfigPanel } from '@/widgets/layout-config';
import { Button, ToastContainer } from '@/shared/ui';
import type { ToastMessage } from '@/shared/ui';
import styles from './LocationsPage.module.css';

interface FormState {
  locationIndex?: number;
  initialData?: Partial<LocationDto>;
}

export function LocationsPage() {
  const navigate = useNavigate();
  const { folioNumber } = useParams<{ folioNumber: string }>();
  const folio = folioNumber ?? '';

  const [formState, setFormState] = useState<FormState | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<{ index: number; name: string } | null>(null);
  const [toasts, setToasts] = useState<ToastMessage[]>([]);
  const [showLayoutConfig, setShowLayoutConfig] = useState(false);

  const { data: summaryData } = useLocationsSummaryQuery(folio);
  const { data: locationsData } = useLocationsQuery(folio);

  const canContinue = (summaryData?.totalCalculable ?? 0) >= 1;

  const addToast = (message: string, type: ToastMessage['type'] = 'error') =>
    setToasts((prev) => [...prev, { id: crypto.randomUUID(), message, type }]);

  const removeToast = (id: string) =>
    setToasts((prev) => prev.filter((t) => t.id !== id));

  const { deleteLocation, isPending: isDeleting } = useDeleteLocation({
    folio,
    onSuccess: () => {
      setDeleteTarget(null);
      addToast('Ubicación eliminada correctamente', 'success');
    },
    onError: (msg) => {
      setDeleteTarget(null);
      addToast(msg, 'error');
    },
  });

  function handleOpenNew() {
    setFormState({});
  }

  function handleOpenEdit(index: number) {
    const location = locationsData?.locations.find((l) => l.index === index);
    setFormState({
      locationIndex: index,
      initialData: location,
    });
  }

  function handleOpenDelete(index: number) {
    const location = locationsData?.locations.find((l) => l.index === index);
    if (location) {
      setDeleteTarget({ index, name: location.locationName });
    }
  }

  function handleFormSuccess() {
    setFormState(null);
    addToast(
      formState?.locationIndex === undefined
        ? 'Ubicación agregada correctamente'
        : 'Ubicación actualizada correctamente',
      'success',
    );
  }



  function handleContinue() {
    navigate(`/quotes/${folio}/technical-info`);
  }

  return (
    <div className={styles.page}>
      <div className={styles.intro}>
        <div>
          <h1 className={styles.title}>Ubicaciones de riesgo</h1>
          <p className={styles.subtitle}>
            Registra cada inmueble a asegurar. Cada ubicación debe tener al menos una cobertura para poder calcular.
          </p>
        </div>
        <Button variant="primary" type="button" onClick={handleOpenNew}>
          <span className="material-symbols-outlined" aria-hidden="true" style={{ fontSize: '1.1rem' }}>
            add
          </span>{' '}
          Agregar ubicación
        </Button>
      </div>

      <div className={styles.gridToolbar}>
        <Button
          variant="ghost"
          type="button"
          onClick={() => setShowLayoutConfig((prev) => !prev)}
          aria-pressed={showLayoutConfig}
          aria-label="Configurar vista de la grilla"
        >
          <span className="material-symbols-outlined" aria-hidden="true" style={{ fontSize: '1.1rem' }}>tune</span>{' '}
          Configurar vista
        </Button>
      </div>

      <LocationsGrid folio={folio} onEdit={handleOpenEdit} onDelete={handleOpenDelete} />

      {showLayoutConfig && (
        <>
          <div
            className={styles.drawerBackdrop}
            onClick={() => setShowLayoutConfig(false)}
            aria-hidden="true"
          />
          <aside className={styles.drawerSide}>
            <LayoutConfigPanel folio={folio} onClose={() => setShowLayoutConfig(false)} />
          </aside>
        </>
      )}

      <footer className={styles.nav}>
        <Button variant="ghost" type="button" onClick={() => navigate(`/quotes/${folio}/general-info`)}>
          ← Anterior
        </Button>
        <Button
          variant="primary"
          type="button"
          onClick={handleContinue}
          disabled={!canContinue}
          title={canContinue ? undefined : 'Agrega al menos 1 ubicación calculable para continuar'}
        >
          Continuar →
        </Button>
      </footer>

      {formState !== null && (
        <LocationForm
          folio={folio}
          locationIndex={formState.locationIndex}
          initialData={
            formState.initialData
              ? {
                  locationName: formState.initialData.locationName,
                  address: formState.initialData.address,
                  zipCode: formState.initialData.zipCode,
                  state: formState.initialData.state,
                  municipality: formState.initialData.municipality,
                  neighborhood: formState.initialData.neighborhood,
                  city: formState.initialData.city,
                  catZone: formState.initialData.catZone,
                  constructionType: formState.initialData.constructionType as
                    | 'Tipo 1 - Macizo'
                    | 'Tipo 2 - Mixto'
                    | 'Tipo 3 - Ligero'
                    | 'Tipo 4 - Metalico'
                    | undefined,
                  level: formState.initialData.level,
                  constructionYear: formState.initialData.constructionYear,
                  businessLine: formState.initialData.locationBusinessLine ?? undefined,
                  guarantees: formState.initialData.guarantees,
                }
              : undefined
          }
          onSuccess={handleFormSuccess}
          onCancel={() => setFormState(null)}
        />
      )}

      {deleteTarget !== null && (
        <DeleteLocationModal
          locationName={deleteTarget.name}
          onConfirm={() => deleteLocation(deleteTarget.index)}
          onCancel={() => setDeleteTarget(null)}
        />
      )}

      {isDeleting && (
        <div className={styles.deletingOverlay} aria-live="polite">
          <span className="material-symbols-outlined" aria-hidden="true" style={{ animation: 'spin 1s linear infinite', fontSize: '1.5rem' }}>
            progress_activity
          </span>{' '}
          Eliminando...
        </div>
      )}

      <ToastContainer toasts={toasts} onClose={removeToast} />
    </div>
  );
}
