export const endpoints = {
  subscribers: '/v1/subscribers',
  riskClassification: '/v1/risk-classification',
  layout: {
    get: (folio: string) => `/v1/quotes/${folio}/locations/layout`,
    update: (folio: string) => `/v1/quotes/${folio}/locations/layout`,
  },
  locations: {
    list: (folio: string) => `/v1/quotes/${folio}/locations`,
    summary: (folio: string) => `/v1/quotes/${folio}/locations/summary`,
    update: (folio: string) => `/v1/quotes/${folio}/locations`,
    patch: (folio: string, index: number) => `/v1/quotes/${folio}/locations/${index}`,
  },
  zipCode: {
    get: (cp: string) => `/v1/zip-codes/${cp}`,
  },
  businessLines: '/v1/business-lines',
  coverageOptions: {
    get: (folio: string) => `/v1/quotes/${folio}/coverage-options`,
    update: (folio: string) => `/v1/quotes/${folio}/coverage-options`,
  },
  catalogs: {
    guarantees: '/v1/catalogs/guarantees',
  },
  quoteState: (folio: string) => `/v1/quotes/${folio}/state`,
  calculate: (folio: string) => `/v1/quotes/${folio}/calculate`,
};
