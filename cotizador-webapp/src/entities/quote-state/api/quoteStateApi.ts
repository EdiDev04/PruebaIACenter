import { httpClient, endpoints } from '@/shared/api';
import type { QuoteStateDto } from '../model/types';

interface QuoteStateResponse {
  data: QuoteStateDto;
}

export const getQuoteState = (folio: string): Promise<QuoteStateDto> =>
  httpClient.get<QuoteStateResponse>(endpoints.quoteState(folio)).then((res) => res.data);
