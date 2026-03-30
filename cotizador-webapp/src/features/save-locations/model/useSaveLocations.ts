import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateLocations, LOCATION_STRINGS } from '@/entities/location';
import type { UpdateLocationsRequest } from '@/entities/location';

interface UseSaveLocationsOptions {
  folio: string;
  onSuccess?: () => void;
  onError?: (message: string) => void;
}

export function useSaveLocations({ folio, onSuccess, onError }: UseSaveLocationsOptions) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (body: UpdateLocationsRequest) => updateLocations(folio, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['locations', folio] });
      queryClient.invalidateQueries({ queryKey: ['locations-summary', folio] });
      queryClient.invalidateQueries({ queryKey: ['quote-state', folio] });
      onSuccess?.();
    },
    onError: (err: unknown) => {
      const apiErr = err as { type?: string; message?: string };
      if (apiErr?.type === 'versionConflict') {
        onError?.(LOCATION_STRINGS.errorVersionConflict);
      } else {
        onError?.(apiErr?.message ?? LOCATION_STRINGS.errorGeneric);
      }
    },
  });
}
