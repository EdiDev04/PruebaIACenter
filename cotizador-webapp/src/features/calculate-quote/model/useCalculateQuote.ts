import { useMutation, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '@/shared/api';
import { endpoints } from '@/shared/api/endpoints';
import { CALCULATE_QUOTE_STRINGS } from '../strings';

export interface CalculateRequest {
  version: number;
}

export interface CoveragePremium {
  guaranteeKey: string;
  insuredAmount: number;
  rate: number;
  premium: number;
}

export interface LocationPremium {
  locationIndex: number;
  locationName: string;
  netPremium: number;
  validationStatus: 'calculable' | 'incomplete';
  coveragePremiums: CoveragePremium[];
}

export interface CalculateResultResponse {
  netPremium: number;
  commercialPremiumBeforeTax: number;
  commercialPremium: number;
  premiumsByLocation: LocationPremium[];
  quoteStatus: string;
  version: number;
}

interface CalculateApiResponse {
  data: CalculateResultResponse;
}

interface UseCalculateQuoteOptions {
  folio: string;
  onSuccess?: (result: CalculateResultResponse) => void;
  onError?: (message: string, type?: 'versionConflict' | 'noCalculable' | 'serviceUnavailable' | 'generic') => void;
}

export function useCalculateQuote({ folio, onSuccess, onError }: UseCalculateQuoteOptions) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (body: CalculateRequest): Promise<CalculateResultResponse> => {
      const response = await httpClient.post<CalculateApiResponse>(endpoints.calculate(folio), body);
      return response.data;
    },
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['quote-state', folio] });
      queryClient.invalidateQueries({ queryKey: ['locations', folio] });
      queryClient.invalidateQueries({ queryKey: ['locations-summary', folio] });
      onSuccess?.(result);
    },
    onError: (err: unknown) => {
      const apiErr = err as { type?: string; message?: string };
      if (apiErr?.type === 'versionConflict') {
        onError?.(CALCULATE_QUOTE_STRINGS.errorVersionConflict, 'versionConflict');
      } else if (apiErr?.type === 'invalidQuoteState') {
        onError?.(CALCULATE_QUOTE_STRINGS.errorNoCalculable, 'noCalculable');
      } else if (apiErr?.type === 'coreOhsUnavailable') {
        onError?.(CALCULATE_QUOTE_STRINGS.errorServiceUnavailable, 'serviceUnavailable');
      } else {
        onError?.(apiErr?.message ?? CALCULATE_QUOTE_STRINGS.errorGeneric, 'generic');
      }
    },
  });
}
