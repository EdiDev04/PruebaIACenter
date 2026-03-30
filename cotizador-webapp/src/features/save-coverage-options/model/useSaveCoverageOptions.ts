import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateCoverageOptions } from '@/entities/coverage-options';
import type { UpdateCoverageOptionsRequest } from '@/entities/coverage-options';
import { SAVE_COVERAGE_OPTIONS_STRINGS } from '../strings';

interface UseSaveCoverageOptionsOptions {
  folio: string;
  onSuccess?: () => void;
  onError?: (message: string, type?: 'versionConflict' | 'generic') => void;
  onValidationError?: (field: string, message: string) => void;
}

export function useSaveCoverageOptions({
  folio,
  onSuccess,
  onError,
  onValidationError,
}: UseSaveCoverageOptionsOptions) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (body: UpdateCoverageOptionsRequest) => updateCoverageOptions(folio, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['coverage-options', folio] });
      queryClient.invalidateQueries({ queryKey: ['quote-state', folio] });
      onSuccess?.();
    },
    onError: (err: unknown) => {
      const apiErr = err as { type?: string; message?: string; field?: string };
      if (apiErr?.type === 'versionConflict') {
        onError?.(SAVE_COVERAGE_OPTIONS_STRINGS.errorVersionConflict, 'versionConflict');
      } else if (apiErr?.type === 'validationError' && apiErr?.field) {
        onValidationError?.(
          apiErr.field,
          apiErr.message ?? SAVE_COVERAGE_OPTIONS_STRINGS.errorGeneric,
        );
      } else {
        onError?.(apiErr?.message ?? SAVE_COVERAGE_OPTIONS_STRINGS.errorGeneric, 'generic');
      }
    },
  });
}
