export interface BusinessLineDto {
  code: string;
  description: string;
  fireKey: string;
  riskLevel: string;
}

export interface BusinessLinesResponse {
  data: BusinessLineDto[];
}
