import { useCalculateQuote } from '../model/useCalculateQuote';
import { CALCULATE_QUOTE_STRINGS } from '../strings';
import type { CalculateResultResponse } from '../model/useCalculateQuote';
import styles from './CalculateButton.module.css';

interface CalculateButtonProps {
  folio: string;
  version: number;
  disabled?: boolean;
  onSuccess?: (result: CalculateResultResponse) => void;
  onError?: (message: string, type?: string) => void;
  buttonLabel?: string;
  variant?: 'primary' | 'secondary';
}

export function CalculateButton({ folio, version, disabled = false, onSuccess, onError, buttonLabel, variant = 'primary' }: CalculateButtonProps) {
  const { mutate, isPending } = useCalculateQuote({ folio, onSuccess, onError });

  const handleClick = () => {
    mutate({ version });
  };

  return (
    <button
      type="button"
      className={`${styles.btn} ${variant === 'secondary' ? styles.btnSecondary : ''}`}
      onClick={handleClick}
      disabled={disabled || isPending}
      aria-busy={isPending}
    >
      {isPending ? CALCULATE_QUOTE_STRINGS.btnCalculating : (buttonLabel ?? CALCULATE_QUOTE_STRINGS.btnCalculate)}
    </button>
  );
}
