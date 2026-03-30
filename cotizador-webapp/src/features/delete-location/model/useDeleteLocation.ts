import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateLocations, LOCATION_STRINGS, useLocationsQuery } from '@/entities/location';
import type { UpdateLocationsRequest } from '@/entities/location';

interface UseDeleteLocationOptions {
  folio: string;
  onSuccess?: () => void;
  onError?: (message: string) => void;
}

export function useDeleteLocation({ folio, onSuccess, onError }: UseDeleteLocationOptions) {
  const queryClient = useQueryClient();
  const { data: locationsData } = useLocationsQuery(folio);

  const mutation = useMutation({
    mutationFn: (body: UpdateLocationsRequest) => updateLocations(folio, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['locations', folio] });
      queryClient.invalidateQueries({ queryKey: ['locations-summary', folio] });
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

  function deleteLocation(index: number) {
    if (!locationsData) return;
    const { locations, version } = locationsData;

    const updatedList = locations
      .filter((loc) => loc.index !== index)
      .map((loc, i) => ({ ...loc, index: i }));

    mutation.mutate({ locations: updatedList, version });
  }

  return {
    deleteLocation,
    isPending: mutation.isPending,
  };
}
