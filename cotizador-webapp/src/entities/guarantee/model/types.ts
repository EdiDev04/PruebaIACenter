export interface GuaranteeDto {
  key: string;
  name: string;
  description: string;
  category: 'fire' | 'cat' | 'additional' | 'special';
  requiresInsuredAmount: boolean;
}

export interface GuaranteesResponse {
  data: GuaranteeDto[];
}
