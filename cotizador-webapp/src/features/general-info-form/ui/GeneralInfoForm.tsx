import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { useState } from 'react';
import { useGeneralInfoForm } from '../model/useGeneralInfoForm';
import { InsuredDataSection } from './InsuredDataSection';
import { ConductionDataSection } from './ConductionDataSection';
import { BusinessClassSection } from './BusinessClassSection';
import { VersionConflictModal } from './VersionConflictModal';
import { setCurrentStep, setFolioVersion } from '@/entities/folio';
import styles from './GeneralInfoForm.module.css';

interface Props {
  onSaveSuccess: () => void;
  onToastError: (msg: string) => void;
  onSaveStart?: () => void;
  formId?: string;
}

function isAppError(err: unknown): err is { type?: string; message?: string } {
  return typeof err === 'object' && err !== null;
}

export function GeneralInfoForm({ onSaveSuccess, onToastError, onSaveStart, formId }: Props) {
  const { folioNumber } = useParams<{ folioNumber: string }>();
  const dispatch = useDispatch();
  const [showConflictModal, setShowConflictModal] = useState(false);

  const { form, mutation, isLoadingData, reloadData } = useGeneralInfoForm(folioNumber ?? '');
  const { control, handleSubmit, setValue, formState: { errors } } = form;

  const handleSubscriberChange = (_code: string, officeName: string) => {
    setValue('officeName', officeName, { shouldValidate: true });
  };

  const onSubmit = handleSubmit((data) => {
    onSaveStart?.();
    mutation.mutate(data, {
      onSuccess: (res) => {
        dispatch(setFolioVersion(res.data.version ?? 0));
        dispatch(setCurrentStep(2));
        onSaveSuccess();
      },
      onError: (err: unknown) => {
        if (isAppError(err) && err.type === 'versionConflict') {
          setShowConflictModal(true);
        } else if (isAppError(err) && err.type === 'invalidQuoteState') {
          onToastError(err.message ?? 'El agente no está registrado en el catálogo');
        } else {
          onToastError('Error al guardar los datos. Intente nuevamente.');
        }
      },
    });
  });

  const handleReload = () => {
    setShowConflictModal(false);
    reloadData();
  };

  if (isLoadingData) {
    return <div className={styles.loading}>Cargando datos...</div>;
  }

  return (
    <>
      <form id={formId} onSubmit={onSubmit} className={styles.form} noValidate>
        <InsuredDataSection control={control} errors={errors} />
        <ConductionDataSection
          control={control}
          errors={errors}
          onSubscriberChange={handleSubscriberChange}
        />
        <BusinessClassSection control={control} errors={errors} />
      </form>

      <VersionConflictModal
        isOpen={showConflictModal}
        onReload={handleReload}
        onCancel={() => setShowConflictModal(false)}
      />
    </>
  );
}
