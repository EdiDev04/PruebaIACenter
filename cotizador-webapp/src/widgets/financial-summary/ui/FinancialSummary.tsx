import { formatCurrency } from '@/shared/lib/formatCurrency';
import styles from './FinancialSummary.module.css';

interface FinancialSummaryProps {
  netPremium: number;
  commercialPremiumBeforeTax: number;
  commercialPremium: number;
}

export function FinancialSummary({ netPremium, commercialPremiumBeforeTax, commercialPremium }: FinancialSummaryProps) {
  return (
    <div className={styles.grid} role="region" aria-label="Resumen financiero">
      <article
        className={styles.card}
        role="region"
        aria-label={`Prima Neta Total: ${formatCurrency(netPremium)}`}
      >
        <div>
          <p className={styles.label}>Prima Neta Total</p>
          <p className={styles.amount}>{formatCurrency(netPremium)}</p>
        </div>
        <p className={styles.description}>Solo riesgo puro</p>
      </article>

      <article
        className={styles.card}
        role="region"
        aria-label={`Prima Comercial sin IVA: ${formatCurrency(commercialPremiumBeforeTax)}`}
      >
        <div>
          <p className={styles.label}>Prima Comercial (sin IVA)</p>
          <p className={styles.amount}>{formatCurrency(commercialPremiumBeforeTax)}</p>
        </div>
        <p className={styles.description}>Incluye gastos de expedición</p>
      </article>

      <article
        className={styles.cardFeatured}
        role="region"
        aria-label={`Prima Comercial Total: ${formatCurrency(commercialPremium)}`}
      >
        <div>
          <div className={styles.cardHeader}>
            <p className={styles.label}>Prima Comercial Total</p>
            <span className={styles.badge} aria-label="Precio final">Precio final</span>
          </div>
          <p className={styles.amountFeatured}>{formatCurrency(commercialPremium)}</p>
        </div>
        <p className={styles.descriptionFeatured}>IVA incluido</p>
      </article>
    </div>
  );
}
