export interface CoverageOptionsDto {
  enabledGuarantees: string[];
  deductiblePercentage: number;
  coinsurancePercentage: number;
  version: number;
}

export interface CoverageOptionsResponse {
  data: CoverageOptionsDto;
}

export interface UpdateCoverageOptionsRequest {
  enabledGuarantees: string[];
  deductiblePercentage: number;
  coinsurancePercentage: number;
  version: number;
}
