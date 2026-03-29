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
        placeholder="202600001"
        error={error}
        helperText="Ingrese año y número, ej: 20260001"
        autoComplete="off"
        {...rest}
      />
    );
  }
);

FolioSearchInput.displayName = 'FolioSearchInput';
