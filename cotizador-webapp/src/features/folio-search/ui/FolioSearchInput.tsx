import { forwardRef, InputHTMLAttributes } from 'react';
import { TextInput } from '@/shared/ui';

interface FolioSearchInputProps extends InputHTMLAttributes<HTMLInputElement> {
  error?: string;
}

export const FolioSearchInput = forwardRef<HTMLInputElement, FolioSearchInputProps>(
  ({ error, ...rest }, ref) => {
    return (
      <TextInput
        ref={ref}
        label="Número de folio"
        placeholder="DAN-YYYY-NNNNN"
        error={error}
        helperText="Ej: DAN-2026-00001"
        autoComplete="off"
        {...rest}
      />
    );
  }
);

FolioSearchInput.displayName = 'FolioSearchInput';
