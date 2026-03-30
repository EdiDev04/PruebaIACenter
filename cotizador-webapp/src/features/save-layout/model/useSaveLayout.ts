import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateLayout } from '@/entities/layout';
import type { UpdateLayoutRequest } from '@/entities/layout';

interface ApiError {
  type: string;
  message: string;
  field: string | null;
}

interface UseSaveLayoutOptions {
  folio: string;
  onSuccess?: () => void;
  onFolioNotFound?: () => void;
}

export function useSaveLayout({ folio, onSuccess, onFolioNotFound }: UseSaveLayoutOptions) {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (request: UpdateLayoutRequest) => updateLayout(folio, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['layout', folio] });
      onSuccess?.();
    },
    onError: (err: unknown) => {
      const apiErr = err as ApiError;
      if (apiErr?.type === 'folioNotFound') {
        onFolioNotFound?.();
      }
    },
  });

  const isConflict =
    mutation.isError &&
    (mutation.error as ApiError)?.type === 'versionConflict';

  const isValidationError =
    mutation.isError &&
    (mutation.error as ApiError)?.type === 'validationError';

  const isFolioNotFound =
    mutation.isError &&
    (mutation.error as ApiError)?.type === 'folioNotFound';

  return {
    mutate: mutation.mutate,
    isPending: mutation.isPending,
    isError: mutation.isError,
    error: mutation.error as ApiError | null,
    isConflict,
    isValidationError,
    isFolioNotFound,
    reset: mutation.reset,
  };
}
