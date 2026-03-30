import type { CoveragePremiumDto } from '@/entities/quote-state';
import { formatCurrency, formatRate } from '@/shared/lib/formatCurrency';
import styles from './CoverageAccordion.module.css';

const GUARANTEE_LABELS: Record<string, string> = {
  building_fire: 'Incendio — Inmueble',
  contents_fire: 'Incendio — Contenidos',
  coverage_extension: 'Ampliación de Cobertura',
  cat_tev: 'CAT — Terremoto / Erupción Volcánica',
  cat_fhm: 'CAT — Huracán / Maremoto',
  debris_removal: 'Remoción de Escombros',
  extraordinary_expenses: 'Gastos Extraordinarios',
  rent_loss: 'Pérdida de Renta',
  business_interruption: 'Interrupción de Negocio',
  electronic_equipment: 'Equipos Electrónicos',
  theft: 'Robo',
  cash_and_securities: 'Valores y Efectivo',
  glass: 'Rotura de Cristales',
  illuminated_signs: 'Letreros Luminosos',
};

interface CoverageAccordionProps {
  coveragePremiums: CoveragePremiumDto[];
}

export function CoverageAccordion({ coveragePremiums }: CoverageAccordionProps) {
  if (coveragePremiums.length === 0) {
    return null;
  }

  return (
    <div className={styles.container} role="table" aria-label="Desglose de coberturas">
      <div className={styles.header} role="row">
        <span role="columnheader">Cobertura</span>
        <span role="columnheader" className={styles.right}>Suma Asegurada</span>
        <span role="columnheader" className={styles.right}>Tasa</span>
        <span role="columnheader" className={styles.right}>Prima</span>
      </div>
      {coveragePremiums.map((cov) => (
        <div key={cov.guaranteeKey} className={styles.row} role="row">
          <span role="cell">{GUARANTEE_LABELS[cov.guaranteeKey] ?? cov.guaranteeKey}</span>
          <span role="cell" className={styles.right}>
            {cov.insuredAmount > 0 ? formatCurrency(cov.insuredAmount) : '—'}
          </span>
          <span role="cell" className={styles.right}>
            {cov.rate > 0 ? formatRate(cov.rate) : 'Tarifa plana'}
          </span>
          <span role="cell" className={`${styles.right} ${styles.premium}`}>
            {formatCurrency(cov.premium)}
          </span>
        </div>
      ))}
    </div>
  );
}
