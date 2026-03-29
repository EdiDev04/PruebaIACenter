import { useRef } from 'react';
import { useMutation } from '@tanstack/react-query';
import { createFolio } from '../api/folioApi';

export function useCreateFolio() {
  const idempotencyKeyRef = useRef<string>(crypto.randomUUID());

  const mutation = useMutation({
    mutationFn: () => createFolio(idempotencyKeyRef.current),
    onError: () => {
      // Keep same idempotency key for retry
    },
    onSuccess: () => {
      // Reset key after successful creation to allow new folio
      idempotencyKeyRef.current = crypto.randomUUID();
    },
  });

  return mutation;
}
