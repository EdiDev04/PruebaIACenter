/**
 * Formatea un número como moneda COP.
 * Ejemplo: 125430.50 → "$125.430,50"
 */
export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

/**
 * Formatea una tasa como porcentaje con hasta 4 decimales.
 * Ejemplo: 0.00125 → "0,1250%"
 */
export function formatRate(rate: number): string {
  return new Intl.NumberFormat('es-CO', {
    style: 'percent',
    minimumFractionDigits: 2,
    maximumFractionDigits: 4,
  }).format(rate);
}
