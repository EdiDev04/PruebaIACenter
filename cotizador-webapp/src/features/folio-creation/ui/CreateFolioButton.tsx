import { useNavigate } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { Button } from '@/shared/ui';
import { useCreateFolio } from '../model/useCreateFolio';
import { initWizard, setFolioVersion } from '@/entities/folio';
import styles from './CreateFolioButton.module.css';

interface Props {
  readonly onToastError: (message: string) => void;
}

const STEP_ROUTES: Record<number, string> = {
  0: 'general-info',
  1: 'general-info',
  2: 'locations',
  3: 'coverages',
  4: 'results',
};

export function CreateFolioButton({ onToastError }: Props) {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { mutate, isPending } = useCreateFolio();

  const handleClick = () => {
    mutate(undefined, {
      onSuccess: (res) => {
        const { folioNumber, version, metadata } = res.data;
        dispatch(initWizard({ activeFolio: folioNumber, currentStep: metadata.lastWizardStep || 1 }));
        dispatch(setFolioVersion(version));
        navigate(`/quotes/${folioNumber}/created`, { state: { fromCreation: true, folio: res.data } });
      },
      onError: () => {
        onToastError('No fue posible crear el folio. Intente nuevamente.');
      },
    });
  };

  return (
    <Button
      variant="primary"
      isLoading={isPending}
      loadingText="Creando folio..."
      onClick={handleClick}
      className={styles.btn}
    >
      + Crear folio nuevo
    </Button>
  );
}
